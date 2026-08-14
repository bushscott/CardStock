using CardStock.Domain.Prices;

namespace CardStock.Domain.Signals;

/// <summary>
/// Spec §12 made executable: firing rules, floors, the anchor-tier rule,
/// priority order, and exact chip text. Windows use CLOSED months only —
/// everything strictly before <c>currentMonth</c>. <see cref="Evaluate"/>
/// returns every firing chip in priority order; <see cref="EvaluateRows"/>
/// is the signals panel's three-state view of the same rules (card.md §2.3.2)
/// — every price-computable signal as a row, quiet and below-floor included.
/// </summary>
public static class ChipEngine
{
    private const decimal RocBand = 0.15m;
    private const decimal ZBand = 1.5m;
    private const decimal R2Floor = 0.8m;
    private const decimal DrawdownBand = -0.15m;
    private const decimal CompressionFactor = 0.8m;
    private const decimal RsiCautionBand = 70m;
    private const decimal RsiOversoldBand = 30m;
    private const decimal SpreadRatioCeiling = 4m;
    private const decimal SpreadMoveBand = 0.20m;

    private const string GlyphUp = "▲";
    private const string GlyphDown = "▼";
    private const string GlyphDash = "–"; // U+2013: caution firing, quiet, below-floor
    private const string NoValue = "—";

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

    // -- the signals panel's rows (card.md §2.3.2) ---------------------------

    /// <summary>
    /// Every price-computable signal as a row in exactly one of three states:
    /// firing (the §12 rules), quiet (computed, inside its bands; value = the
    /// live reading), or below-floor (value `—`; the tooltip names the floor
    /// and the best progress toward it across anchor-order tiers — never a
    /// number). Ordered firing → quiet → below-floor, §12 priority within each
    /// state (RSI sits in the momentum family, after the EMA cross). The
    /// composition layer appends the Neutral and Locked rows — sales and
    /// clocks stay out of Domain purity here.
    /// </summary>
    public static IReadOnlyList<SignalRow> EvaluateRows(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var rows = new List<SignalRow>
        {
            RocRow(prices, currentMonth),
            MacdRow(prices, currentMonth),
            EmaCrossRow(prices, currentMonth),
            RsiRow(prices, currentMonth),
            ZScoreRow(prices, currentMonth),
            SpreadRow(prices, currentMonth),
            TrendR2Row(prices, currentMonth),
            DrawdownRow(prices, currentMonth),
        };

        // OrderBy is stable, so within a state the list order above IS priority.
        return [.. rows.OrderBy(r => r.State switch
        {
            SignalState.Firing => 0,
            SignalState.Neutral => 1,
            SignalState.Quiet => 2,
            SignalState.BelowFloor => 3,
            _ => 4,
        })];
    }

    private static SignalRow RocRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t =>
            At(t, currentMonth, 0) is not null && PresentNonZero(At(t, currentMonth, 3)) == 1);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t =>
                Present(At(t, currentMonth, 0)) + PresentNonZero(At(t, currentMonth, 3)));
            return BelowFloorRow("ROC 3M", $"needs closed months at t and t−3 · {progress} of 2 present");
        }

        var roc = Indicators.Roc(
            At(anchor.Value.Tier, currentMonth, 0)!.Value, At(anchor.Value.Tier, currentMonth, 3)!.Value)!.Value;
        var text = Pct(roc);
        if (Math.Abs(roc) < RocBand)
        {
            return QuietRow("ROC 3M", text,
                $"{anchor.Value.Label} · 3-month return {text} · inside the ±15% band · closed months only");
        }

        return new SignalRow(
            roc > 0 ? GlyphUp : GlyphDown, "ROC 3M", text,
            $"{anchor.Value.Label} · 3-month return {text} · fires at ±15% · closed months only",
            SignalState.Firing, roc > 0 ? ChipTone.Pos : ChipTone.Neg);
    }

    private static SignalRow MacdRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 10) is not null);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t => PresentInWindow(t, currentMonth, 10).Count);
            return BelowFloorRow("MACD (3,6,4)", $"needs the last 10 closed months · {progress} of 10 present");
        }

        var run = ConsecutiveRun(anchor.Value.Tier, currentMonth, 10)!;
        var macd = Indicators.Ema(run, 3).Zip(Indicators.Ema(run, 6), (f, s) => f - s).ToList();
        var signal = Indicators.Ema(macd, 4);
        var histogram = macd[^1] - signal[^1];
        if (histogram == 0)
        {
            return QuietRow("MACD (3,6,4)", "hist 0",
                $"{anchor.Value.Label} · MACD(3,6,4) histogram at zero · closed months only");
        }

        var side = histogram > 0 ? "above" : "below";
        return new SignalRow(
            histogram > 0 ? GlyphUp : GlyphDown, "MACD (3,6,4)", $"{side} signal",
            $"{anchor.Value.Label} · MACD(3,6,4) {side} its signal line · histogram {SignedDollars(histogram)} · closed months only",
            SignalState.Firing, histogram > 0 ? ChipTone.Pos : ChipTone.Neg);
    }

    private static SignalRow EmaCrossRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 12) is not null);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t => PresentInWindow(t, currentMonth, 12).Count);
            return BelowFloorRow("EMA 3/9 cross", $"needs the last 12 closed months · {progress} of 12 present");
        }

        var run = ConsecutiveRun(anchor.Value.Tier, currentMonth, 12)!;
        var spread = Indicators.Ema(run, 3).Zip(Indicators.Ema(run, 9), (f, s) => f - s).ToList();
        var now = Math.Sign(spread[^1]);
        var before = Math.Sign(spread[^3]);
        if (now == 0 || now == before)
        {
            return QuietRow("EMA 3/9 cross", "no cross 2mo",
                $"{anchor.Value.Label} · EMA 3/9 · no crossover within the last 2 closed months");
        }

        return new SignalRow(
            now > 0 ? GlyphUp : GlyphDown, "EMA 3/9 cross",
            now > 0 ? "+ cross 2mo" : "− cross 2mo",
            $"{anchor.Value.Label} · EMA 3 crossed {(now > 0 ? "above" : "below")} EMA 9 within the last 2 closed months",
            SignalState.Firing, now > 0 ? ChipTone.Pos : ChipTone.Neg);
    }

    private static SignalRow RsiRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 7) is not null);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t => PresentInWindow(t, currentMonth, 7).Count);
            return BelowFloorRow("RSI (6)", $"needs the last 7 closed months · {progress} of 7 present");
        }

        var rsi = Indicators.Rsi(ConsecutiveRun(anchor.Value.Tier, currentMonth, 7)!, 6);
        if (rsi is null)
        {
            // Defensive: a non-positive price slipped past the source filter.
            return BelowFloorRow("RSI (6)", "a non-positive price in the window — RSI is undefined");
        }

        var reading = $"{Math.Round(rsi.Value, MidpointRounding.AwayFromZero):0}";
        if (rsi.Value >= RsiCautionBand)
        {
            return new SignalRow(GlyphDash, "RSI (6)", "overbought",
                $"{anchor.Value.Label} · RSI(6) {reading} · caution at ≥ 70 · closed months only",
                SignalState.Firing, ChipTone.Caution);
        }

        if (rsi.Value <= RsiOversoldBand)
        {
            return new SignalRow(GlyphUp, "RSI (6)", "oversold",
                $"{anchor.Value.Label} · RSI(6) {reading} · oversold at ≤ 30 · closed months only",
                SignalState.Firing, ChipTone.Pos);
        }

        return QuietRow("RSI (6)", reading,
            $"{anchor.Value.Label} · RSI(6) {reading} · between bands — caution at ≥ 70, oversold at ≤ 30 · closed months only");
    }

    private static SignalRow ZScoreRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => ConsecutiveRun(t, currentMonth, 7) is not null);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t => PresentInWindow(t, currentMonth, 7).Count);
            return BelowFloorRow("z vs 6M", $"needs the last 7 closed months · {progress} of 7 present");
        }

        var run = ConsecutiveRun(anchor.Value.Tier, currentMonth, 7)!;
        var z = Indicators.ZScore(run.Skip(1).ToList());
        if (z is null)
        {
            return BelowFloorRow("z vs 6M",
                $"{anchor.Value.Label} · 6-month σ is zero — z is undefined on a flat window");
        }

        var text = z.Value > 0 ? $"+{z.Value:0.0}σ"
            : z.Value < 0 ? $"−{Math.Abs(z.Value):0.0}σ"
            : "0.0σ";
        var tooltip = $"{anchor.Value.Label} · {Math.Abs(z.Value):0.0}σ from its 6-month mean · fires beyond 1.5σ";
        if (Math.Abs(z.Value) <= ZBand)
        {
            return QuietRow("z vs 6M", text, tooltip);
        }

        return new SignalRow(
            z.Value > 0 ? GlyphUp : GlyphDown, "z vs 6M", text, tooltip,
            SignalState.Firing, z.Value > 0 ? ChipTone.Pos : ChipTone.Neg);
    }

    private static SignalRow SpreadRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var psa10 = prices.Tiers.FirstOrDefault(t => t.Tier == PriceTier.Psa10);
        var grade9 = prices.Tiers.FirstOrDefault(t => t.Tier == PriceTier.Grade9);
        var nowTop = psa10 is null ? null : At(psa10, currentMonth, 0);
        var nowBase = grade9 is null ? null : At(grade9, currentMonth, 0);
        var endpoints = PresentNonZero(nowTop) + PresentNonZero(nowBase);
        if (endpoints < 2)
        {
            return BelowFloorRow("Tier spread 10/9",
                $"needs PSA 10 and Grade 9 at the last closed month · {endpoints} of 2 present");
        }

        var ratioNow = nowTop!.Value / nowBase!.Value;
        var thenTop = At(psa10!, currentMonth, 6);
        var thenBase = At(grade9!, currentMonth, 6);
        decimal? ratioThen = null;
        if (thenTop is not null && thenTop.Value != 0 && thenBase is not null && thenBase.Value != 0)
        {
            ratioThen = thenTop.Value / thenBase.Value;
        }

        var value = $"×{ratioNow:0.0}";
        var basis = ratioThen is null
            ? "no ratio 6 closed months back to compare"
            : $"×{ratioThen:0.0} six closed months ago";
        var tooltip =
            $"PSA 10 / Grade 9 ratio {value} · fires at ×4 or a ≥20% move vs 6 closed months earlier · {basis}";

        var moved = ratioThen is not null && Math.Abs(ratioNow / ratioThen.Value - 1) >= SpreadMoveBand;
        if (ratioNow >= SpreadRatioCeiling || moved)
        {
            return new SignalRow(GlyphDown, "Tier spread 10/9", value, tooltip, SignalState.Firing, ChipTone.Neg);
        }

        return QuietRow("Tier spread 10/9", value, tooltip);
    }

    private static SignalRow TrendR2Row(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => PresentInWindow(t, currentMonth, 12).Count >= 6);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t => PresentInWindow(t, currentMonth, 12).Count);
            return BelowFloorRow("Trend R²", $"needs 6 of the last 12 closed months · {progress} present");
        }

        var (slope, r2) = Indicators.LogTrend(PresentInWindow(anchor.Value.Tier, currentMonth, 12));
        var value = $"{r2:.00}";
        var tooltip =
            $"{anchor.Value.Label} · log-price regression over the trailing 12 closed months · fires at R² ≥ .80";
        if (r2 < R2Floor)
        {
            return QuietRow("Trend R²", value, tooltip);
        }

        return new SignalRow(
            slope > 0 ? GlyphUp : GlyphDown, "Trend R²", value, tooltip,
            SignalState.Firing, slope > 0 ? ChipTone.Pos : ChipTone.Neg);
    }

    private static SignalRow DrawdownRow(CardPriceSnapshot prices, DateOnly currentMonth)
    {
        var anchor = Anchor(prices, t => PresentInWindow(t, currentMonth, 12).Count >= 3);
        if (anchor is null)
        {
            var progress = BestProgress(prices, t => PresentInWindow(t, currentMonth, 12).Count);
            return BelowFloorRow("Drawdown", $"needs 3 of the last 12 closed months · {progress} present");
        }

        var drawdown = Indicators.Drawdown(PresentInWindow(anchor.Value.Tier, currentMonth, 12));
        var value = Pct(drawdown).Replace("+", "");
        var tooltip = $"{anchor.Value.Label} · {value} below its trailing 12-month peak · fires at −15%";
        if (drawdown > DrawdownBand)
        {
            return QuietRow("Drawdown", value, tooltip);
        }

        return new SignalRow(GlyphDown, "Drawdown", value, tooltip, SignalState.Firing, ChipTone.Neg);
    }

    // -- row plumbing --------------------------------------------------------

    private static SignalRow BelowFloorRow(string name, string tooltip) =>
        new(GlyphDash, name, NoValue, tooltip, SignalState.BelowFloor, ChipTone.Neutral);

    private static SignalRow QuietRow(string name, string value, string tooltip) =>
        new(GlyphDash, name, value, tooltip, SignalState.Quiet, ChipTone.Neutral);

    private static int Present(decimal? value) => value is null ? 0 : 1;

    private static int PresentNonZero(decimal? value) => value is null || value.Value == 0 ? 0 : 1;

    /// <summary>Best progress toward a floor across anchor-order tiers — the
    /// numerator is computed while the floor's denominator is authored (D-061).</summary>
    private static int BestProgress(CardPriceSnapshot prices, Func<TierSnapshot, int> progress)
    {
        var best = 0;
        foreach (var wanted in AnchorOrder)
        {
            var tier = prices.Tiers.FirstOrDefault(t => t.Tier == wanted);
            if (tier is not null)
            {
                best = Math.Max(best, progress(tier));
            }
        }

        return best;
    }

    /// <summary>Whole-dollar signed magnitude for the MACD histogram:
    /// `+94` / `−94` (U+2212) / `0`.</summary>
    private static string SignedDollars(decimal value)
    {
        var rounded = Math.Round(Math.Abs(value), MidpointRounding.AwayFromZero);
        return rounded == 0 ? "0" : value > 0 ? $"+{rounded:0}" : $"−{rounded:0}";
    }
}
