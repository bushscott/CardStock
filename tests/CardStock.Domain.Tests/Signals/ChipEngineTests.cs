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

    // -- EvaluateRows: the signals panel's three-state engine (card.md §2.3.2) --

    private static readonly List<string> RowPriority =
        ["ROC 3M", "MACD (3,6,4)", "EMA 3/9 cross", "RSI (6)", "z vs 6M", "Tier spread 10/9", "Trend R²", "Drawdown"];

    private static (int, decimal)[] SteepRun() =>
        [.. Enumerable.Range(0, 13).Select(i => (i - 12, 100m * (decimal)Math.Pow(1.09, i)))];

    private static (int, decimal)[] FlatRun() =>
        [.. Enumerable.Range(0, 13).Select(i => (i - 12, 100m))];

    private static SignalRow Row(IReadOnlyList<SignalRow> rows, string name) =>
        Assert.Single(rows, r => r.Name == name);

    private static int StateRank(SignalState state) => state switch
    {
        SignalState.Firing => 0,
        SignalState.Neutral => 1,
        SignalState.Quiet => 2,
        SignalState.BelowFloor => 3,
        _ => 4,
    };

    [Fact]
    public void Rows_report_every_price_signal_exactly_once()
    {
        var rows = ChipEngine.EvaluateRows(
            Snapshot(Tier(PriceTier.Psa10, SteepRun()), Tier(PriceTier.Grade9, FlatRun())), Current);

        Assert.Equal(RowPriority.Order().ToList(), rows.Select(r => r.Name).Order().ToList());
    }

    [Fact]
    public void Rows_group_by_state_and_keep_signal_priority_within_each_group()
    {
        var rows = ChipEngine.EvaluateRows(
            Snapshot(Tier(PriceTier.Psa10, SteepRun()), Tier(PriceTier.Grade9, FlatRun())), Current);

        var ranks = rows.Select(r => StateRank(r.State)).ToList();
        Assert.Equal(ranks.OrderBy(r => r).ToList(), ranks);

        foreach (var group in rows.GroupBy(r => r.State))
        {
            var indices = group.Select(r => RowPriority.IndexOf(r.Name)).ToList();
            Assert.DoesNotContain(-1, indices);
            Assert.Equal(indices.OrderBy(i => i).ToList(), indices);
        }

        // The steep run's momentum signals lead the list.
        Assert.Equal(SignalState.Firing, Row(rows, "ROC 3M").State);
        Assert.Equal(SignalState.Firing, Row(rows, "MACD (3,6,4)").State);
    }

    [Fact]
    public void Roc_row_fires_with_the_evidence_value()
    {
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-4, 100m), (-3, 105m), (-2, 108m), (-1, 115m), (0, 999m)));

        var roc = Row(ChipEngine.EvaluateRows(snapshot, Current), "ROC 3M");

        Assert.Equal(SignalState.Firing, roc.State);
        Assert.Equal("▲", roc.Glyph);
        Assert.Equal("+15%", roc.Value);
        Assert.Equal(ChipTone.Pos, roc.Tone);
        Assert.Contains("PSA 10", roc.Tooltip);
    }

    [Fact]
    public void Roc_row_quiet_inside_the_band_shows_the_live_reading()
    {
        var snapshot = Snapshot(Tier(PriceTier.Psa10, (-4, 100m), (-1, 113m)));

        var roc = Row(ChipEngine.EvaluateRows(snapshot, Current), "ROC 3M");

        Assert.Equal(SignalState.Quiet, roc.State);
        Assert.Equal("–", roc.Glyph);
        Assert.Equal("+13%", roc.Value);
        Assert.Equal(ChipTone.Neutral, roc.Tone);
        Assert.Contains("±15%", roc.Tooltip);
    }

    [Fact]
    public void Roc_row_below_the_floor_names_the_floor_and_progress_never_a_number()
    {
        // t present, t−3 absent, on the only tier there is.
        var snapshot = Snapshot(Tier(PriceTier.Psa10, (-2, 110m), (-1, 130m)));

        var roc = Row(ChipEngine.EvaluateRows(snapshot, Current), "ROC 3M");

        Assert.Equal(SignalState.BelowFloor, roc.State);
        Assert.Equal("–", roc.Glyph);
        Assert.Equal("—", roc.Value);
        Assert.Equal("needs closed months at t and t−3 · 1 of 2 present", roc.Tooltip);
    }

    [Fact]
    public void Macd_row_quiet_on_an_exactly_flat_run_reads_hist_zero()
    {
        var macd = Row(
            ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, FlatRun())), Current), "MACD (3,6,4)");

        Assert.Equal(SignalState.Quiet, macd.State);
        Assert.Equal("hist 0", macd.Value);
    }

    [Fact]
    public void Macd_row_firing_carries_the_histogram_in_the_tooltip()
    {
        var macd = Row(
            ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, SteepRun())), Current), "MACD (3,6,4)");

        Assert.Equal(SignalState.Firing, macd.State);
        Assert.Equal("above signal", macd.Value);
        Assert.Contains("histogram +", macd.Tooltip);
    }

    [Fact]
    public void Ema_row_fires_on_a_cross_within_two_closed_months()
    {
        // Long decline, sharp recovery in the last two closed months: the EMA-3/EMA-9
        // spread's sign at t−2 is negative and at t is positive (hand-checked).
        decimal[] vals = [200m, 190m, 180m, 170m, 160m, 150m, 140m, 130m, 120m, 110m, 150m, 200m];
        var points = vals.Select((v, i) => (i - 12, v)).ToArray();

        var ema = Row(ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, points)), Current), "EMA 3/9 cross");

        Assert.Equal(SignalState.Firing, ema.State);
        Assert.Equal("▲", ema.Glyph);
        Assert.Equal("+ cross 2mo", ema.Value);
    }

    [Fact]
    public void Ema_row_quiet_when_nothing_crossed()
    {
        var ema = Row(
            ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, SteepRun())), Current), "EMA 3/9 cross");

        Assert.Equal(SignalState.Quiet, ema.State);
        Assert.Equal("no cross 2mo", ema.Value);
    }

    [Fact]
    public void Rsi_row_fires_caution_when_overbought()
    {
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-7, 100m), (-6, 110m), (-5, 105m), (-4, 115m), (-3, 120m), (-2, 118m), (-1, 125m)));

        var rsi = Row(ChipEngine.EvaluateRows(snapshot, Current), "RSI (6)");

        Assert.Equal(SignalState.Firing, rsi.State);
        Assert.Equal(ChipTone.Caution, rsi.Tone);
        Assert.Equal("–", rsi.Glyph);
        Assert.Equal("overbought", rsi.Value);
        Assert.Contains("82", rsi.Tooltip);
    }

    [Fact]
    public void Rsi_row_fires_positive_when_oversold()
    {
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-7, 125m), (-6, 118m), (-5, 120m), (-4, 115m), (-3, 105m), (-2, 110m), (-1, 100m)));

        var rsi = Row(ChipEngine.EvaluateRows(snapshot, Current), "RSI (6)");

        Assert.Equal(SignalState.Firing, rsi.State);
        Assert.Equal(ChipTone.Pos, rsi.Tone);
        Assert.Equal("▲", rsi.Glyph);
        Assert.Equal("oversold", rsi.Value);
        Assert.Contains("18", rsi.Tooltip);
    }

    [Fact]
    public void Rsi_row_quiet_between_bands_shows_the_reading()
    {
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-7, 100m), (-6, 102m), (-5, 101m), (-4, 103m), (-3, 102m), (-2, 104m), (-1, 103m)));

        var rsi = Row(ChipEngine.EvaluateRows(snapshot, Current), "RSI (6)");

        Assert.Equal(SignalState.Quiet, rsi.State);
        Assert.Equal("67", rsi.Value);
    }

    [Fact]
    public void Z_row_quiet_shows_the_signed_sigma_reading()
    {
        // Window (trailing 6 incl. newest closed): [10,10,10,10,11,11] -> z ≈ +1.29.
        var snapshot = Snapshot(Tier(PriceTier.Psa10,
            (-7, 10m), (-6, 10m), (-5, 10m), (-4, 10m), (-3, 10m), (-2, 11m), (-1, 11m)));

        var z = Row(ChipEngine.EvaluateRows(snapshot, Current), "z vs 6M");

        Assert.Equal(SignalState.Quiet, z.State);
        Assert.Equal("+1.3σ", z.Value);
    }

    [Fact]
    public void Z_row_below_the_floor_when_sigma_is_zero()
    {
        var z = Row(ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, FlatRun())), Current), "z vs 6M");

        Assert.Equal(SignalState.BelowFloor, z.State);
        Assert.Equal("—", z.Value);
        Assert.Contains("σ is zero", z.Tooltip);
    }

    [Fact]
    public void Spread_row_fires_at_the_ratio_ceiling()
    {
        var snapshot = Snapshot(
            Tier(PriceTier.Psa10, (-1, 400m)),
            Tier(PriceTier.Grade9, (-1, 100m)));

        var spread = Row(ChipEngine.EvaluateRows(snapshot, Current), "Tier spread 10/9");

        Assert.Equal(SignalState.Firing, spread.State);
        Assert.Equal("▼", spread.Glyph);
        Assert.Equal("×4.0", spread.Value);
        Assert.Equal(ChipTone.Neg, spread.Tone);
        Assert.Contains("×4", spread.Tooltip);
        Assert.Contains("20%", spread.Tooltip);
    }

    [Theory]
    [InlineData(370, "×3.7")] // 3.7/3.0 − 1 = +23%
    [InlineData(240, "×2.4")] // 2.4/3.0 − 1 = −20%
    public void Spread_row_fires_on_a_20_percent_move_in_either_direction(int nowTop, string value)
    {
        var snapshot = Snapshot(
            Tier(PriceTier.Psa10, (-7, 300m), (-1, nowTop)),
            Tier(PriceTier.Grade9, (-7, 100m), (-1, 100m)));

        var spread = Row(ChipEngine.EvaluateRows(snapshot, Current), "Tier spread 10/9");

        Assert.Equal(SignalState.Firing, spread.State);
        Assert.Equal(value, spread.Value);
    }

    [Fact]
    public void Spread_row_quiet_shows_the_current_ratio_and_its_basis()
    {
        var snapshot = Snapshot(
            Tier(PriceTier.Psa10, (-7, 300m), (-1, 310m)),
            Tier(PriceTier.Grade9, (-7, 100m), (-1, 100m)));

        var spread = Row(ChipEngine.EvaluateRows(snapshot, Current), "Tier spread 10/9");

        Assert.Equal(SignalState.Quiet, spread.State);
        Assert.Equal("×3.1", spread.Value);
        Assert.Contains("×3.0 six closed months ago", spread.Tooltip);
    }

    [Fact]
    public void Spread_row_below_the_floor_when_grade_9_is_absent()
    {
        var spread = Row(
            ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, (-1, 400m))), Current), "Tier spread 10/9");

        Assert.Equal(SignalState.BelowFloor, spread.State);
        Assert.Equal("—", spread.Value);
        Assert.Equal("needs PSA 10 and Grade 9 at the last closed month · 1 of 2 present", spread.Tooltip);
    }

    [Fact]
    public void Drawdown_row_fires_with_the_signed_value()
    {
        var snapshot = Snapshot(Tier(PriceTier.Psa10, (-3, 100m), (-2, 120m), (-1, 90m)));

        var drawdown = Row(ChipEngine.EvaluateRows(snapshot, Current), "Drawdown");

        Assert.Equal(SignalState.Firing, drawdown.State);
        Assert.Equal("▼", drawdown.Glyph);
        Assert.Equal("−25%", drawdown.Value);
    }

    [Fact]
    public void An_empty_snapshot_reports_every_row_below_its_floor()
    {
        var rows = ChipEngine.EvaluateRows(Snapshot(), Current);

        Assert.Equal(8, rows.Count);
        Assert.All(rows, r => Assert.Equal(SignalState.BelowFloor, r.State));
        Assert.All(rows, r => Assert.Equal("—", r.Value));
    }

    [Fact]
    public void A_flat_card_reads_quiet_not_empty()
    {
        var rows = ChipEngine.EvaluateRows(Snapshot(Tier(PriceTier.Psa10, FlatRun())), Current);

        Assert.Equal(8, rows.Count);
        Assert.Equal("+0%", Row(rows, "ROC 3M").Value);
        Assert.Equal(SignalState.Quiet, Row(rows, "ROC 3M").State);
        Assert.Equal("50", Row(rows, "RSI (6)").Value);
        Assert.Equal("no cross 2mo", Row(rows, "EMA 3/9 cross").Value);
        Assert.Equal("0%", Row(rows, "Drawdown").Value);
        Assert.Equal(SignalState.BelowFloor, Row(rows, "z vs 6M").State);
        Assert.Equal(SignalState.BelowFloor, Row(rows, "Tier spread 10/9").State);
    }
}
