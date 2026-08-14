using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

public class CensusPairTests : BunitContext
{
    // card.md §3.8 fixed six: PSA 8, PSA 9, PSA 10, CGC 8, CGC 9, CGC 10 (D-084.4 --
    // CGC 9.5 is structurally impossible, populations.grade is a short 1-10 column).
    private static CensusDto SixBars(
        int psa8, int psa9, int psa10, int cgc8, int cgc9, int cgc10,
        int psaTotal = 0, int cgcTotal = 0, DateTimeOffset? observedAt = null, int qualifying = 0,
        IReadOnlyList<CensusMetricDto>? metrics = null,
        IReadOnlyList<CensusDeltaBarDto>? deltaBars = null) =>
        new(
            [
                new CensusBarDto("psa", 8, psa8),
                new CensusBarDto("psa", 9, psa9),
                new CensusBarDto("psa", 10, psa10),
                new CensusBarDto("cgc", 8, cgc8),
                new CensusBarDto("cgc", 9, cgc9),
                new CensusBarDto("cgc", 10, cgc10),
            ],
            psaTotal, cgcTotal, observedAt, qualifying,
            metrics ??
            [
                LowData("Gem rate",
                    "needs 90 days of census deltas; observations count from 2026-09-01 — the window fills 2026-11-30"),
                LowData("Pace",
                    $"needs census deltas; observations count from 2026-09-01, {qualifying} so far — deltas need two"),
            ],
            deltaBars ?? SevenGhosts());

    private static CensusMetricDto LowData(string name, string note) =>
        new(name, "lowdata", null, [new MetricSegmentDto(note, "neutral")]);

    private static IReadOnlyList<CensusDeltaBarDto> SevenGhosts()
    {
        var labels = new[] { "Sep ’26", "Oct ’26", "Nov ’26", "Dec ’26", "Jan ’27", "Feb ’27", "Mar ’27" };
        return
        [
            .. labels.Select((label, i) => new CensusDeltaBarDto(
                new DateOnly(2026, 9, 1).AddMonths(i).ToString("yyyy-MM"), label, "pending", null,
                $"new PSA 10 slabs for {label} — closes {new DateOnly(2026, 10, 1).AddMonths(i):yyyy-MM-dd}")),
        ];
    }

    public static IEnumerable<object[]> HeightCases =>
    [
        [0, 108], // PSA 8  = 15,931 (this card's own max, D-084.8)
        [1, 59],  // PSA 9  =  8,455
        [2, 7],   // PSA 10 =    486
        [3, 4],   // CGC 8  =      1
        [4, 4],   // CGC 9  =      2
        [5, 4],   // CGC 10 =      4
    ];

    [Theory]
    [MemberData(nameof(HeightCases))]
    public void Bar_height_scales_to_the_cards_own_max(int index, int expectedPx)
    {
        var census = SixBars(15931, 8455, 486, 1, 2, 4);

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        var bars = cut.FindAll(".census-bar");
        Assert.Equal($"height: {expectedPx}px;", bars[index].GetAttribute("style"));
    }

    [Fact]
    public void All_zero_census_renders_six_4px_stubs_and_the_zero_totals_line()
    {
        var census = SixBars(0, 0, 0, 0, 0, 0, psaTotal: 0, cgcTotal: 0);

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        var bars = cut.FindAll(".census-bar");
        Assert.Equal(6, bars.Count);
        Assert.All(bars, bar => Assert.Equal("height: 4px;", bar.GetAttribute("style")));
        Assert.Equal("0 PSA · 0 CGC slabs across all grades", cut.Find(".census-summary").TextContent);
    }

    [Fact]
    public void Value_and_grade_labels_and_bar_tooltip_are_formatted_per_spec()
    {
        var census = SixBars(15931, 8455, 486, 1, 2, 4);

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        var cols = cut.FindAll(".census-bar-col");
        Assert.Equal("15,931", cols[0].QuerySelector(".census-bar-value")!.TextContent);
        Assert.Equal("PSA 8", cols[0].QuerySelector(".census-bar-label")!.TextContent);
        Assert.Equal(
            "PSA 8: 15,931 slabs in current census (PSA)",
            cols[0].QuerySelector(".census-bar")!.GetAttribute("title"));

        Assert.Equal("CGC 10", cols[5].QuerySelector(".census-bar-label")!.TextContent);
    }

    [Fact]
    public void Summary_and_header_sub_use_the_full_grader_totals_not_just_the_six_bars()
    {
        var census = SixBars(
            1, 1, 1, 1, 1, 1, psaTotal: 15931, cgcTotal: 618,
            observedAt: new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero));

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        Assert.Equal("15,931 PSA · 618 CGC slabs across all grades", cut.Find(".census-summary").TextContent);
        Assert.Equal("PSA + CGC · as of 2026-07-30", cut.Find(".census-sub").TextContent);
    }

    [Fact]
    public void Header_sub_reads_never_observed_when_observed_at_is_null()
    {
        var census = SixBars(0, 0, 0, 0, 0, 0, observedAt: null);

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        Assert.Equal("PSA + CGC · never observed", cut.Find(".census-sub").TextContent);
    }

    [Theory]
    [InlineData(0, "0 OBS", "Census observations counted from 2026-09-01 — 0 so far; deltas need two.")]
    [InlineData(5, "5 OBS", "Census observations counted from 2026-09-01 — 5 so far; deltas need two.")]
    public void Grading_activity_panel_renders_both_metric_slots_as_low_data_states(
        int qualifying, string expectedBadge, string expectedTooltip)
    {
        // D-087 applied to the census metrics (owner, 2026-08-13): the mockup's two
        // metric rows — gem rate and pace — render as slots holding LOW DATA states
        // with their unlock condition, never a generic one-liner and never a number.
        // D-093: the notes arrive computed on the wire, per metric.
        var census = SixBars(0, 0, 0, 0, 0, 0, qualifying: qualifying);

        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census, census));

        Assert.Contains("Grading activity · PSA 10 slabs added", cut.Find("h2").TextContent);
        var badge = cut.Find(".obs-badge");
        Assert.Equal(expectedBadge, badge.TextContent);
        Assert.Equal(expectedTooltip, badge.GetAttribute("title"));

        var names = cut.FindAll(".ga-metric-name").Select(e => e.TextContent).ToList();
        Assert.Equal(["Gem rate", "Pace"], names);
        Assert.All(cut.FindAll(".ga-state"), s => Assert.Equal("LOW DATA", s.TextContent));
        Assert.Equal(2, cut.FindAll(".ga-state").Count);
        var notes = cut.FindAll(".ga-metric-note").Select(n => n.TextContent).ToList();
        Assert.Contains("the window fills 2026-11-30", notes[0]);
        Assert.Equal(
            $"needs census deltas; observations count from 2026-09-01, {qualifying} so far — deltas need two",
            notes[1]);
    }

    [Fact]
    public void The_ghost_chart_renders_seven_dashed_slots_with_no_numbers()
    {
        // D-094: before any month closes, all seven slots ghost — dashed, no
        // value, month labels below, tooltips naming each month's unlock.
        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census, SixBars(0, 0, 0, 0, 0, 0)));

        var ghosts = cut.FindAll(".ga-bar-pending");
        Assert.Equal(7, ghosts.Count);
        Assert.Empty(cut.FindAll(".ga-bar-observed"));
        Assert.Empty(cut.FindAll(".ga-bar-value"));
        Assert.Equal(
            "new PSA 10 slabs for Sep ’26 — closes 2026-10-01", ghosts[0].GetAttribute("title"));
        Assert.Equal(
            ["Sep ’26", "Oct ’26", "Nov ’26", "Dec ’26", "Jan ’27", "Feb ’27", "Mar ’27"],
            cut.FindAll(".ga-bar-label").Select(l => l.TextContent));
    }

    [Fact]
    public void Observed_months_materialize_scaled_beside_the_remaining_ghosts()
    {
        // Sep +10 and Oct +42 observed, the rest ghost: the tallest observed bar
        // reaches 108px, +10 scales to round(10/42·104)+4 = 29, and a ghost
        // never carries a number.
        var bars = new List<CensusDeltaBarDto>
        {
            new("2026-09", "Sep ’26", "observed", 10, "+10 new PSA 10 slabs in Sep ’26"),
            new("2026-10", "Oct ’26", "observed", 42, "+42 new PSA 10 slabs in Oct ’26"),
            new("2026-11", "Nov ’26", "pending", null, "new PSA 10 slabs for Nov ’26 — closes 2026-12-01"),
        };
        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census,
            SixBars(0, 0, 0, 0, 0, 0, qualifying: 2, deltaBars: bars)));

        var observed = cut.FindAll(".ga-bar-observed");
        Assert.Equal("height: 29px;", observed[0].GetAttribute("style"));
        Assert.Equal("height: 108px;", observed[1].GetAttribute("style"));
        Assert.Equal(["+10", "+42"], cut.FindAll(".ga-bar-value").Select(v => v.TextContent));
        Assert.Single(cut.FindAll(".ga-bar-pending"));
    }

    [Fact]
    public void A_restated_down_month_shows_its_minus_on_a_stub_bar()
    {
        var bars = new List<CensusDeltaBarDto>
        {
            new("2026-09", "Sep ’26", "observed", 10, "+10 new PSA 10 slabs in Sep ’26"),
            new("2026-10", "Oct ’26", "observed", -4, "−4 new PSA 10 slabs in Oct ’26"),
        };
        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census,
            SixBars(0, 0, 0, 0, 0, 0, qualifying: 2, deltaBars: bars)));

        var observed = cut.FindAll(".ga-bar-observed");
        Assert.Equal("height: 4px;", observed[1].GetAttribute("style"));
        Assert.Equal(["+10", "−4"], cut.FindAll(".ga-bar-value").Select(v => v.TextContent));
    }

    [Fact]
    public void A_computed_metric_renders_its_value_and_toned_segments_with_no_state_chip()
    {
        // D-093's unlocked form, driven entirely by the wire: headline value,
        // sentence segments with the market-meaning tone on the number token.
        var census = SixBars(0, 0, 1479, 0, 0, 0, qualifying: 7, metrics:
        [
            LowData("Gem rate",
                "needs 90 days of census deltas; observations count from 2026-09-01 — the window fills 2026-11-30"),
            new CensusMetricDto("Pace", "ok", "+58 / mo",
            [
                new MetricSegmentDto("and rising — ", "neutral"),
                new MetricSegmentDto("331 new 10s since Sep ’26", "neutral"),
                new MetricSegmentDto(", growing the census ", "neutral"),
                new MetricSegmentDto("+29%", "neg"),
                new MetricSegmentDto(" in 7 months ", "neutral"),
                new MetricSegmentDto("(fresh supply working against the price)", "neutral"),
            ]),
        ]);

        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census, census));

        Assert.Single(cut.FindAll(".ga-state"));  // only the still-locked gem rate
        Assert.Equal("+58 / mo", cut.Find(".ga-metric-value").TextContent);
        var growth = cut.FindAll(".ga-seg-neg").Single();
        Assert.Equal("+29%", growth.TextContent);
        Assert.Equal(
            "and rising — 331 new 10s since Sep ’26, growing the census +29% in 7 months " +
            "(fresh supply working against the price)",
            cut.FindAll(".ga-metric-note")[1].TextContent);
    }
}
