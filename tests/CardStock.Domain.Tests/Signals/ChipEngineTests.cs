using CardStock.Domain.Prices;
using CardStock.Domain.Signals;

namespace CardStock.Domain.Tests.Signals;

public class ChipEngineTests
{
    private static readonly DateOnly Current = new(2026, 8, 1);

    private static TierSnapshot Tier(PriceTier tier, params (int YearMonthOffsetFromCurrent, decimal Dollars)[] points)
    {
        var monthly = points
            .Select(p => new MonthlyPrice(
                Current.AddMonths(p.YearMonthOffsetFromCurrent), (int)(p.Dollars * 100),
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)))
            .OrderBy(p => p.Month)
            .ToList();
        var series = new TierSeries(tier, monthly);
        return new TierSnapshot(tier, series, new NoPriceSeries(), new ChangeInsufficient(0, 0));
    }

    private static CardPriceSnapshot Snapshot(params TierSnapshot[] tiers) =>
        new(630417, null, tiers);

    [Fact]
    public void Roc_fires_at_the_threshold_and_excludes_the_current_month()
    {
        // Closed months: May 100 -> ... -> t(=Jul) 115 exactly +15% vs t-3(Apr).
        // Current month (Aug) present but must be ignored.
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-4, 100m), (-3, 105m), (-2, 108m), (-1, 115m), (0, 999m)));

        var chips = ChipEngine.Evaluate(snapshot, Current);

        var roc = Assert.Single(chips, c => c.Text.StartsWith("ROC 3M"));
        Assert.Equal("ROC 3M +15%", roc.Text);
        Assert.Equal(ChipTone.Pos, roc.Tone);
        Assert.Contains("PSA 10", roc.Tooltip);
    }

    [Fact]
    public void Below_the_floor_a_signal_never_chips()
    {
        // Only 3 closed months: ROC 3M needs t and t-3 -> t-3 missing -> silent.
        var snapshot = Snapshot(Tier(PriceTier.Psa10, (-3, 100m), (-2, 110m), (-1, 130m)));

        Assert.DoesNotContain(ChipEngine.Evaluate(snapshot, Current),
            c => c.Text.StartsWith("ROC"));
    }

    [Fact]
    public void Anchor_falls_back_to_the_next_tier_and_the_tooltip_names_it()
    {
        // PSA 10 too thin for ROC; Grade 9 has both endpoints and fires.
        var snapshot = Snapshot(
            Tier(PriceTier.Psa10, (-1, 100m)),
            Tier(PriceTier.Grade9, (-4, 100m), (-1, 130m)));

        var roc = Assert.Single(ChipEngine.Evaluate(snapshot, Current),
            c => c.Text.StartsWith("ROC 3M"));
        Assert.Contains("Grade 9", roc.Tooltip);
    }

    [Fact]
    public void Spread_compression_fires_on_the_authored_threshold()
    {
        // PSA10/Grade9 ratio: t-6 = 1000/100 = 10.0; t = 790/100 = 7.9 -> 0.79x <= 0.8x
        var snapshot = Snapshot(
            Tier(PriceTier.Psa10, (-7, 1000m), (-1, 790m)),
            Tier(PriceTier.Grade9, (-7, 100m), (-1, 100m)));

        var spread = Assert.Single(ChipEngine.Evaluate(snapshot, Current),
            c => c.Text == "spread compressing");
        Assert.Equal(ChipTone.Neg, spread.Tone);
    }

    [Fact]
    public void Drawdown_formats_with_the_true_minus()
    {
        // Peak 120, last 90 -> -25% -> "−25% off peak" (U+2212).
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-3, 100m), (-2, 120m), (-1, 90m)));

        Assert.Contains(ChipEngine.Evaluate(snapshot, Current),
            c => c.Text == "−25% off peak");
    }

    [Fact]
    public void Chips_come_back_in_priority_order()
    {
        // A steep clean 12-month doubling run: ROC, MACD, z(maybe), R2, all closed months present.
        var run = Enumerable.Range(0, 13)
            .Select(i => ((i - 12), 100m * (decimal)Math.Pow(1.09, i)))
            .ToArray();
        var chips = ChipEngine.Evaluate(Snapshot(Tier(PriceTier.Psa10, run)), Current);

        var order = chips.Select(c => c.Text.Split(' ')[0]).ToList();
        // Whatever fired, ROC precedes MACD precedes EMA precedes z precedes R².
        var rank = new List<string> { "ROC", "MACD", "EMA", "z", "spread", "clean" };
        var indices = order.Select(o => rank.FindIndex(r => o.StartsWith(r))).ToList();
        Assert.Equal(indices.OrderBy(i => i).ToList(), indices);
        Assert.Contains(chips, c => c.Text.StartsWith("ROC 3M +"));
    }

    [Fact]
    public void A_quiet_card_returns_no_chips()
    {
        var flat = Enumerable.Range(0, 13).Select(i => ((i - 12), 100m)).ToArray();
        Assert.Empty(ChipEngine.Evaluate(Snapshot(Tier(PriceTier.Psa10, flat)), Current));
    }
}
