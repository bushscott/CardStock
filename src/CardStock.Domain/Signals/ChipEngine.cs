using CardStock.Domain.Prices;

namespace CardStock.Domain.Signals;

/// <summary>
/// Spec §12 made executable: firing rules, floors, the anchor-tier rule,
/// priority order, and exact chip text. Windows use CLOSED months only —
/// everything strictly before <c>currentMonth</c>. Returns every firing chip
/// in priority order; the CLIENT caps display at four (+N more).
/// </summary>
public static class ChipEngine
{
    private const decimal RocBand = 0.15m;
    private const decimal ZBand = 1.5m;
    private const decimal R2Floor = 0.8m;
    private const decimal DrawdownBand = -0.15m;
    private const decimal CompressionFactor = 0.8m;

    // Anchor preference = strip order, best first (spec §12).
    private static readonly PriceTier[] AnchorOrder =
        [PriceTier.Psa10, PriceTier.Grade9Half, PriceTier.Grade9,
         PriceTier.Grade8, PriceTier.Grade7, PriceTier.Ungraded];

    public static IReadOnlyList<SignalChip> Evaluate(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var chips = new List<SignalChip>();
        AddRoc(chips, prices, currentMonth);
        AddMacd(chips, prices, currentMonth);
        AddEmaCross(chips, prices, currentMonth);
        AddZScore(chips, prices, currentMonth);
        AddSpreadCompression(chips, prices, currentMonth);
        AddTrendR2(chips, prices, currentMonth);
        AddDrawdown(chips, prices, currentMonth);
        return chips;
    }

    // -- window plumbing ---------------------------------------------------

    /// <summary>Closed-month value at offset back from the newest closed month
    /// (offset 0 = last closed month), or null where the source published nothing.</summary>
    private static decimal? At(TierSnapshot tier, DateOnly currentMonth, int offsetBack)
    {
        var month = currentMonth.AddMonths(-1 - offsetBack);
        var point = tier.Series.Points.FirstOrDefault(p => p.Month == month);
        return point is null ? null : point.PriceCents / 100m;
    }

    /// <summary>The trailing N closed months, oldest→newest, null when any is absent.</summary>
    private static IReadOnlyList<decimal>? ConsecutiveRun(TierSnapshot tier, DateOnly currentMonth, int months)
    {
        var run = new decimal[months];
        for (var back = 0; back < months; back++)
        {
            var value = At(tier, currentMonth, months - 1 - back);
            if (value is null)
            {
                return null;
            }

            run[back] = value.Value;
        }

        return run;
    }

    /// <summary>Present closed-month values in the trailing window, month order, holes skipped.</summary>
    private static IReadOnlyList<decimal> PresentInWindow(TierSnapshot tier, DateOnly currentMonth, int months) =>
        [.. Enumerable.Range(0, months)
            .Select(back => At(tier, currentMonth, months - 1 - back))
            .Where(v => v is not null)
            .Select(v => v!.Value)];

    /// <summary>The anchor rule: PSA 10 when it satisfies the requirement, else the
    /// first strip-order tier that does. Returns the tier and its label for the tooltip.</summary>
    private static (TierSnapshot Tier, string Label)? Anchor(
        CardPriceSnapshot prices, Func<TierSnapshot, bool> satisfies)
    {
        foreach (var wanted in AnchorOrder)
        {
            var tier = prices.Tiers.FirstOrDefault(t => t.Tier == wanted);
            if (tier is not null && satisfies(tier))
            {
                return (tier, Label(wanted));
            }
        }

        return null;
    }

    private static string Label(PriceTier tier) => TierLabels.For(tier);

    private static string Pct(decimal fraction)
    {
        var rounded = Math.Round(fraction * 100, MidpointRounding.AwayFromZero);
        return rounded < 0 ? $"−{Math.Abs(rounded):0}%" : $"+{rounded:0}%";
    }

    // -- the seven signals -------------------------------------------------

    private static void AddRoc(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => At(t, currentMonth, 0) is not null && At(t, currentMonth, 3) is not null);
        if (anchor is null)
        {
            return;
        }

        var roc = Indicators.Roc(
            At(anchor.Value.Tier, currentMonth, 0)!.Value, At(anchor.Value.Tier, currentMonth, 3)!.Value);
        // I2: Roc is null when the t-3 anchor price is zero (CardPriceReader filters those at
        // the source; this is the defensive path). Treated exactly like any other
        // insufficient-data case: no chip, not a crash.
        if (roc is null || Math.Abs(roc.Value) < RocBand)
        {
            return;
        }

        chips.Add(new SignalChip(
            roc.Value > 0 ? "▲" : "▼",
            $"ROC 3M {Pct(roc.Value)}",
            $"{anchor.Value.Label} · 3-month return {Pct(roc.Value)} · fires at ±15% · closed months only",
            roc.Value > 0 ? ChipTone.Pos : ChipTone.Neg));
    }

    private static void AddMacd(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 10) is not null);
        if (anchor is null)
        {
            return;
        }

        var run = ConsecutiveRun(anchor.Value.Tier, currentMonth, 10)!;
        var macd = Indicators.Ema(run, 3).Zip(Indicators.Ema(run, 6), (f, s) => f - s).ToList();
        var signal = Indicators.Ema(macd, 4);
        var histogram = macd[^1] - signal[^1];
        if (histogram == 0)
        {
            return;
        }

        chips.Add(new SignalChip(
            histogram > 0 ? "▲" : "▼",
            histogram > 0 ? "MACD +" : "MACD −",
            $"{anchor.Value.Label} · MACD(3,6,4) {(histogram > 0 ? "above" : "below")} its signal line · closed months only",
            histogram > 0 ? ChipTone.Pos : ChipTone.Neg));
    }

    private static void AddEmaCross(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 12) is not null);
        if (anchor is null)
        {
            return;
        }

        var run = ConsecutiveRun(anchor.Value.Tier, currentMonth, 12)!;
        var spread = Indicators.Ema(run, 3).Zip(Indicators.Ema(run, 9), (f, s) => f - s).ToList();

        // A cross "within the last 2 closed months": the sign at the end differs
        // from the sign 2 months earlier, and the end is non-zero.
        var now = Math.Sign(spread[^1]);
        var before = Math.Sign(spread[^3]);
        if (now == 0 || now == before)
        {
            return;
        }

        chips.Add(new SignalChip(
            now > 0 ? "▲" : "▼",
            now > 0 ? "EMA cross +" : "EMA cross −",
            $"{anchor.Value.Label} · EMA 3 crossed {(now > 0 ? "above" : "below")} EMA 9 within the last 2 closed months",
            now > 0 ? ChipTone.Pos : ChipTone.Neg));
    }

    private static void AddZScore(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 7) is not null);
        if (anchor is null)
        {
            return;
        }

        // Window: trailing 6 months INCLUSIVE of the newest closed month (spec §12).
        var run = ConsecutiveRun(anchor.Value.Tier, currentMonth, 7)!;
        var z = Indicators.ZScore(run.Skip(1).ToList());
        if (z is null || Math.Abs(z.Value) <= ZBand)
        {
            return;
        }

        var text = z.Value > 0 ? $"z +{z.Value:0.0}" : $"z −{Math.Abs(z.Value):0.0}";
        chips.Add(new SignalChip(
            z.Value > 0 ? "▲" : "▼",
            text,
            $"{anchor.Value.Label} · {Math.Abs(z.Value):0.0}σ from its 6-month mean · fires beyond 1.5σ",
            z.Value > 0 ? ChipTone.Pos : ChipTone.Neg));
    }

    private static void AddSpreadCompression(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var psa10 = prices.Tiers.FirstOrDefault(t => t.Tier == PriceTier.Psa10);
        var grade9 = prices.Tiers.FirstOrDefault(t => t.Tier == PriceTier.Grade9);
        if (psa10 is null || grade9 is null)
        {
            return;
        }

        var nowTop = At(psa10, currentMonth, 0);
        var nowBase = At(grade9, currentMonth, 0);
        var thenTop = At(psa10, currentMonth, 6);
        var thenBase = At(grade9, currentMonth, 6);
        if (nowTop is null || nowBase is null or 0 || thenTop is null || thenBase is null or 0)
        {
            return;
        }

        var ratioNow = nowTop.Value / nowBase.Value;
        var ratioThen = thenTop.Value / thenBase.Value;
        if (ratioThen == 0 || ratioNow > CompressionFactor * ratioThen)
        {
            return;
        }

        chips.Add(new SignalChip(
            "▼",
            "spread compressing",
            $"PSA 10 / Grade 9 ratio {ratioNow:0.0}x, down from {ratioThen:0.0}x six closed months ago · fires at ≤20% of the earlier ratio lost",
            ChipTone.Neg));
    }

    private static void AddTrendR2(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => PresentInWindow(t, currentMonth, 12).Count >= 6);
        if (anchor is null)
        {
            return;
        }

        var values = PresentInWindow(anchor.Value.Tier, currentMonth, 12);
        var (slope, r2) = Indicators.LogTrend(values);
        if (r2 < R2Floor)
        {
            return;
        }

        chips.Add(new SignalChip(
            slope > 0 ? "▲" : "▼",
            $"clean trend R² {r2:.00}",
            $"{anchor.Value.Label} · log-price regression over the trailing 12 closed months · fires at R² ≥ .80",
            slope > 0 ? ChipTone.Pos : ChipTone.Neg));
    }

    private static void AddDrawdown(List<SignalChip> chips, CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => PresentInWindow(t, currentMonth, 12).Count >= 3);
        if (anchor is null)
        {
            return;
        }

        var drawdown = Indicators.Drawdown(PresentInWindow(anchor.Value.Tier, currentMonth, 12));
        if (drawdown > DrawdownBand)
        {
            return;
        }

        chips.Add(new SignalChip(
            "▼",
            $"{Pct(drawdown).Replace("+", "")} off peak",
            $"{anchor.Value.Label} · {Pct(drawdown)} below its trailing 12-month peak · fires at −15%",
            ChipTone.Neg));
    }
}
