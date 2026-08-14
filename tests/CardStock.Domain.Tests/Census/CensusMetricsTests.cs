using CardStock.Domain.Census;
using CardStock.Domain.Signals;

namespace CardStock.Domain.Tests.Census;

/// <summary>
/// The census sentences (card.md §3.8/§3.9), computed read-time behind data
/// checks (owner ruling 2026-08-13, D-093). Levels flat-fill from the FULL row
/// history (the populations storage contract — pre-floor levels are real);
/// measurement WINDOWS are valid only wholly on/after the 2026-09-01 floor
/// (D-033 gates the interval, not the level). The pace fixture reproduces
/// §3.9's seed derivations exactly — the spec hand-checked 331 / +29% / +58 /
/// rising against the prototype, so the seed doubles as an external referee.
/// </summary>
public class CensusMetricsTests
{
    private static CensusObservation Row(string grader, short grade, int pop, int y, int m, int d) =>
        new(grader, grade, pop, new DateTimeOffset(y, m, d, 12, 0, 0, TimeSpan.Zero));

    private static CensusMetric Gem(IReadOnlyList<CensusObservation> rows, DateOnly today) =>
        Assert.Single(CensusMetrics.Evaluate(rows, today), m => m.Name == "Gem rate");

    private static CensusMetric Pace(IReadOnlyList<CensusObservation> rows, DateOnly today) =>
        Assert.Single(CensusMetrics.Evaluate(rows, today), m => m.Name == "Pace");

    private static string Text(CensusMetric metric) => string.Concat(metric.Segments.Select(s => s.Text));

    // -- gem rate ------------------------------------------------------------

    [Fact]
    public void Gem_rate_low_data_until_the_window_is_fully_post_floor()
    {
        // 2026-11-29: the trailing 90 days reach back to Aug 31, before the floor.
        var gem = Gem([], new DateOnly(2026, 11, 29));

        Assert.Equal(MetricState.LowData, gem.State);
        Assert.Null(gem.Value);
        Assert.Equal(
            "needs 90 days of census deltas; observations count from 09-01-2026 — the window fills 11-30-2026",
            Text(gem));
    }

    [Fact]
    public void Gem_rate_low_data_below_thirty_submissions_names_the_progress()
    {
        // Window valid (2026-11-30) but only 29 new PSA slabs inside it.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 20),
            Row("psa", 10, 1010, 2026, 11, 25),  // Δ10 = 10
            Row("psa", 9, 5000, 2026, 8, 20),
            Row("psa", 9, 5019, 2026, 11, 25),   // Δ9 = 19 → Δall = 29
        ];

        var gem = Gem(rows, new DateOnly(2026, 11, 30));

        Assert.Equal(MetricState.LowData, gem.State);
        Assert.Equal(
            "fewer than 30 PSA slabs graded in the last 90 days · 29 of 30 — rate withheld",
            Text(gem));
    }

    [Fact]
    public void Gem_rate_treats_a_restated_down_census_as_zero_progress()
    {
        // A restatement shrinks the window delta below zero: 0 of 30, never negative.
        List<CensusObservation> rows =
        [
            Row("psa", 9, 5000, 2026, 8, 20),
            Row("psa", 9, 4995, 2026, 11, 25),
        ];

        var gem = Gem(rows, new DateOnly(2026, 12, 15));

        Assert.Equal(MetricState.LowData, gem.State);
        Assert.Contains("0 of 30", Text(gem));
    }

    [Fact]
    public void Gem_rate_computes_without_drift_while_the_prior_window_is_pre_floor()
    {
        // today 2026-12-15: current window starts Sep 16 (valid); the prior 90
        // days start Jun 18 (pre-floor) → the drift clause is omitted entirely.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 9, 10),
            Row("psa", 10, 1011, 2026, 12, 1),   // Δ10 = 11
            Row("psa", 9, 5000, 2026, 8, 20),
            Row("psa", 9, 5029, 2026, 11, 20),   // Δ9 = 29 → Δall = 40
        ];

        var gem = Gem(rows, new DateOnly(2026, 12, 15));

        Assert.Equal(MetricState.Ok, gem.State);
        Assert.Equal("27.5%", gem.Value);  // 11/40
        Assert.Equal("of the last 90 days of PSA submissions, the share that came back 10", Text(gem));
    }

    [Fact]
    public void Gem_rate_falling_drift_is_green_with_the_authored_parenthetical()
    {
        // Prior window (Sep 11–Dec 10): Δ10 40 of Δall 100 → 40.0%.
        // Current window (Dec 10–Mar 10): Δ10 27 of Δall 100 → 27.0%. Drift −13.0pp.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 9, 5),
            Row("psa", 10, 1040, 2026, 12, 1),
            Row("psa", 10, 1067, 2027, 3, 1),
            Row("psa", 9, 5000, 2026, 9, 5),
            Row("psa", 9, 5060, 2026, 12, 1),
            Row("psa", 9, 5133, 2027, 3, 1),
        ];

        var gem = Gem(rows, new DateOnly(2027, 3, 10));

        Assert.Equal(MetricState.Ok, gem.State);
        Assert.Equal("27.0%", gem.Value);
        Assert.Equal(
            "of the last 90 days of PSA submissions, the share that came back 10 · drifting " +
            "−13.0pp / 90d (harder to gem = supply of fresh 10s slowing)",
            Text(gem));
        var drift = gem.Segments.Single(s => s.Text.Contains("pp / 90d"));
        Assert.Equal(ChipTone.Pos, drift.Tone);  // falling gem rate is bullish for holders
    }

    [Fact]
    public void Gem_rate_rising_drift_is_red_with_the_authored_parenthetical()
    {
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 9, 5),
            Row("psa", 10, 1027, 2026, 12, 1),
            Row("psa", 10, 1067, 2027, 3, 1),
            Row("psa", 9, 5000, 2026, 9, 5),
            Row("psa", 9, 5073, 2026, 12, 1),
            Row("psa", 9, 5133, 2027, 3, 1),
        ];

        var gem = Gem(rows, new DateOnly(2027, 3, 10));

        Assert.Contains("(easier to gem = fresh 10s arriving faster)", Text(gem));
        var drift = gem.Segments.Single(s => s.Text.Contains("pp / 90d"));
        Assert.Equal("+13.0pp / 90d", drift.Text);
        Assert.Equal(ChipTone.Neg, drift.Tone);
    }

    [Fact]
    public void Gem_rate_inside_the_flat_band_reads_steady_untoned()
    {
        // Prior 271/1000 = 27.1%; current 270/1000 = 27.0% → drift −0.1pp: steady.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 9, 5),
            Row("psa", 10, 1271, 2026, 12, 1),
            Row("psa", 10, 1541, 2027, 3, 1),
            Row("psa", 9, 5000, 2026, 9, 5),
            Row("psa", 9, 5729, 2026, 12, 1),
            Row("psa", 9, 6459, 2027, 3, 1),
        ];

        var gem = Gem(rows, new DateOnly(2027, 3, 10));

        Assert.EndsWith("−0.1pp / 90d steady", Text(gem));
        Assert.All(gem.Segments, s => Assert.Equal(ChipTone.Neutral, s.Tone));
    }

    // -- pace ----------------------------------------------------------------

    [Fact]
    public void Pace_low_data_below_two_qualifying_observations()
    {
        // One pre-floor row and one post-floor row: 1 qualifying (D-033).
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 7, 28),
            Row("psa", 10, 1010, 2026, 9, 10),
        ];

        var pace = Pace(rows, new DateOnly(2026, 9, 20));

        Assert.Equal(MetricState.LowData, pace.State);
        Assert.Equal(
            "needs census deltas; observations count from 09-01-2026, 1 so far — deltas need two",
            Text(pace));
    }

    [Fact]
    public void Pace_low_data_until_a_post_floor_month_has_closed()
    {
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 9, 5),
            Row("psa", 10, 1010, 2026, 9, 20),
        ];

        var pace = Pace(rows, new DateOnly(2026, 9, 25));

        Assert.Equal(MetricState.LowData, pace.State);
        Assert.Equal("first monthly delta closes 10-01-2026 — 2 observations so far", Text(pace));
    }

    [Fact]
    public void Pace_reproduces_the_spec_seed_arithmetic_exactly()
    {
        // §3.9's checked seed: deltas [34,41,38,52,47,61,58] over 7 closed
        // months from a census of 1148 → +58 / mo, rising, 331 new 10s, +29%
        // (>2%/mo → red supply parenthetical). Rebased onto Sep ’26–Mar ’27.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1148, 2026, 8, 15),   // level at the floor
            Row("psa", 10, 1182, 2026, 9, 20),   // Sep +34
            Row("psa", 10, 1223, 2026, 10, 15),  // Oct +41
            Row("psa", 10, 1261, 2026, 11, 20),  // Nov +38
            Row("psa", 10, 1313, 2026, 12, 18),  // Dec +52
            Row("psa", 10, 1360, 2027, 1, 15),   // Jan +47
            Row("psa", 10, 1421, 2027, 2, 14),   // Feb +61
            Row("psa", 10, 1479, 2027, 3, 25),   // Mar +58
        ];

        var pace = Pace(rows, new DateOnly(2027, 4, 5));

        Assert.Equal(MetricState.Ok, pace.State);
        Assert.Equal("+58 / mo", pace.Value);
        Assert.Equal(
            "and rising — 331 new 10s since Sep ’26, growing the census +29% in 7 months " +
            "(fresh supply working against the price)",
            Text(pace));
        var growth = pace.Segments.Single(s => s.Text == "+29%");
        Assert.Equal(ChipTone.Neg, growth.Tone);  // supply growth is bearish
    }

    [Fact]
    public void Pace_slow_growth_reads_scarcity_intact_in_green()
    {
        // 7 months, +2 slabs/month on a census of 1000: +1% over 7 months → ≤2%/mo.
        var rows = new List<CensusObservation> { Row("psa", 10, 1000, 2026, 8, 15) };
        for (var m = 0; m < 7; m++)
        {
            var month = new DateOnly(2026, 9, 1).AddMonths(m);
            rows.Add(Row("psa", 10, 1002 + 2 * m, month.Year, month.Month, 15));
        }

        var pace = Pace(rows, new DateOnly(2027, 4, 5));

        Assert.Equal(MetricState.Ok, pace.State);
        Assert.Equal("+2 / mo", pace.Value);
        Assert.Contains("(supply nearly frozen — scarcity intact)", Text(pace));
        var growth = pace.Segments.Single(s => s.Text == "+1%");
        Assert.Equal(ChipTone.Pos, growth.Tone);
    }

    [Fact]
    public void Pace_omits_the_trend_word_below_six_closed_months()
    {
        // Two closed months (Sep, Oct): the 3-vs-3 comparison is undefined.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 15),
            Row("psa", 10, 1010, 2026, 9, 20),
            Row("psa", 10, 1022, 2026, 10, 15),
        ];

        var pace = Pace(rows, new DateOnly(2026, 11, 5));

        Assert.Equal(MetricState.Ok, pace.State);
        Assert.Equal("+12 / mo", pace.Value);
        Assert.StartsWith("— 22 new 10s since Sep ’26", Text(pace));
        Assert.DoesNotContain("rising", Text(pace));
    }

    [Fact]
    public void Pace_omits_the_growth_clause_when_the_starting_census_is_zero()
    {
        // No PSA 10 slabs existed at the floor: percentage growth is undefined.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 8, 2026, 9, 20),
            Row("psa", 10, 12, 2026, 10, 15),
        ];

        var pace = Pace(rows, new DateOnly(2026, 11, 5));

        Assert.Equal(MetricState.Ok, pace.State);
        Assert.Equal("— 12 new 10s since Sep ’26", Text(pace));
    }

    [Fact]
    public void Pace_renders_a_restated_down_month_with_the_true_minus()
    {
        // Sep +10, Oct −4 (restatement): the latest month shows what happened.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 15),
            Row("psa", 10, 1010, 2026, 9, 20),
            Row("psa", 10, 1006, 2026, 10, 15),
        ];

        var pace = Pace(rows, new DateOnly(2026, 11, 5));

        Assert.Equal("−4 / mo", pace.Value);
    }

    // -- window plumbing -----------------------------------------------------

    [Fact]
    public void A_row_on_the_month_boundary_closes_the_earlier_month()
    {
        // CountAt(D) includes rows dated D, so a row observed ON Oct 1 is
        // September's closing level — its movement lands in September. If it
        // were attributed to October instead, Oct would read +30, not +25.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 15),
            Row("psa", 10, 1010, 2026, 9, 20),
            Row("psa", 10, 1015, 2026, 10, 1),
            Row("psa", 10, 1040, 2026, 10, 20),
        ];

        var pace = Pace(rows, new DateOnly(2026, 11, 5));

        // Sep = 1015 − 1000 = +15; Oct = count(Nov 1) − count(Oct 1) = 1040 − 1015.
        Assert.Equal("+25 / mo", pace.Value);
        Assert.Contains("40 new 10s", Text(pace));
    }

    [Fact]
    public void The_qualifying_floor_is_a_utc_date()
    {
        // 2026-09-01 02:00 +05:00 is still 2026-08-31 in UTC — pre-floor, so it
        // does not qualify (local-date semantics would count 2 and unlock pace).
        List<CensusObservation> rows =
        [
            new("psa", 10, 1000, new DateTimeOffset(2026, 9, 1, 2, 0, 0, TimeSpan.FromHours(5))),
            new("psa", 10, 1010, new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero)),
        ];

        var pace = Pace(rows, new DateOnly(2026, 9, 20));

        Assert.Equal(MetricState.LowData, pace.State);
        Assert.Equal(
            "needs census deltas; observations count from 09-01-2026, 1 so far — deltas need two",
            Text(pace));
    }

    [Fact]
    public void Cgc_rows_never_reach_the_psa_only_metrics()
    {
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 15),
            Row("psa", 10, 1010, 2026, 9, 20),
            Row("psa", 10, 1022, 2026, 10, 15),
            Row("cgc", 10, 4, 2026, 9, 10),
            Row("cgc", 10, 900, 2026, 10, 10),
        ];

        var pace = Pace(rows, new DateOnly(2026, 11, 5));

        Assert.Equal("+12 / mo", pace.Value);
        Assert.Contains("22 new 10s", Text(pace));
    }

    [Fact]
    public void Both_metrics_evaluate_in_slot_order()
    {
        var metrics = CensusMetrics.Evaluate([], new DateOnly(2026, 8, 13));

        Assert.Equal(new[] { "Gem rate", "Pace" }, metrics.Select(m => m.Name));
        Assert.All(metrics, m => Assert.Equal(MetricState.LowData, m.State));
    }

    // -- the ghost delta chart (D-094) ---------------------------------------

    [Fact]
    public void Before_the_floor_the_chart_is_seven_ghosts_naming_their_close_dates()
    {
        var bars = CensusMetrics.DeltaBars([], new DateOnly(2026, 8, 14));

        Assert.Equal(7, bars.Count);
        Assert.All(bars, b => Assert.False(b.Observed));
        Assert.All(bars, b => Assert.Null(b.Delta));
        Assert.Equal(
            ["Sep ’26", "Oct ’26", "Nov ’26", "Dec ’26", "Jan ’27", "Feb ’27", "Mar ’27"],
            bars.Select(b => b.Label));
        Assert.Equal("new PSA 10 slabs for Sep ’26 — closes 10-01-2026", bars[0].Tooltip);
        Assert.Equal("new PSA 10 slabs for Mar ’27 — closes 04-01-2027", bars[6].Tooltip);
    }

    [Fact]
    public void Closed_months_materialize_and_the_current_month_stays_a_ghost()
    {
        // Sep +10 and Oct +42 observed; November is the in-progress ghost, the
        // outlined partial month the prototype's border plumbing anticipated (OQ-10).
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 15),
            Row("psa", 10, 1010, 2026, 9, 20),
            Row("psa", 10, 1052, 2026, 10, 15),
        ];

        var bars = CensusMetrics.DeltaBars(rows, new DateOnly(2026, 11, 5));

        Assert.Equal("Sep ’26", bars[0].Label);
        Assert.True(bars[0].Observed);
        Assert.Equal(10, bars[0].Delta);
        Assert.Equal("+10 new PSA 10 slabs in Sep ’26", bars[0].Tooltip);
        Assert.True(bars[1].Observed);
        Assert.Equal(42, bars[1].Delta);
        Assert.False(bars[2].Observed);
        Assert.Equal("new PSA 10 slabs for Nov ’26 — closes 12-01-2026", bars[2].Tooltip);
        Assert.All(bars.Skip(3), b => Assert.False(b.Observed));
    }

    [Fact]
    public void A_closed_month_stays_a_ghost_while_the_pace_gate_is_unmet()
    {
        // One qualifying observation: closed months exist but no delta can be
        // claimed, so they ghost with the gate's own copy, never a fabricated 0.
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 7, 28),
            Row("psa", 10, 1010, 2026, 9, 10),
        ];

        var bars = CensusMetrics.DeltaBars(rows, new DateOnly(2026, 11, 5));

        Assert.False(bars[0].Observed);
        Assert.Equal(
            "new PSA 10 slabs for Sep ’26 — needs census deltas; observations count from 09-01-2026, 1 so far",
            bars[0].Tooltip);
        Assert.False(bars[1].Observed);
    }

    [Fact]
    public void The_window_slides_once_seven_months_have_passed()
    {
        var bars = CensusMetrics.DeltaBars([], new DateOnly(2027, 6, 10));

        Assert.Equal(7, bars.Count);
        Assert.Equal("Dec ’26", bars[0].Label);
        Assert.Equal("Jun ’27", bars[6].Label);
        Assert.Equal(new DateOnly(2026, 12, 1), bars[0].Month);
    }

    [Fact]
    public void A_restated_down_month_charts_with_the_true_minus()
    {
        List<CensusObservation> rows =
        [
            Row("psa", 10, 1000, 2026, 8, 15),
            Row("psa", 10, 1010, 2026, 9, 20),
            Row("psa", 10, 1006, 2026, 10, 15),
        ];

        var bars = CensusMetrics.DeltaBars(rows, new DateOnly(2026, 11, 5));

        Assert.Equal(-4, bars[1].Delta);
        Assert.Equal("−4 new PSA 10 slabs in Oct ’26", bars[1].Tooltip);
    }
}
