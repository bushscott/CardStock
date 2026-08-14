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
        int psaTotal = 0, int cgcTotal = 0, DateTimeOffset? observedAt = null, int qualifying = 0) =>
        new(
            [
                new CensusBarDto("psa", 8, psa8),
                new CensusBarDto("psa", 9, psa9),
                new CensusBarDto("psa", 10, psa10),
                new CensusBarDto("cgc", 8, cgc8),
                new CensusBarDto("cgc", 9, cgc9),
                new CensusBarDto("cgc", 10, cgc10),
            ],
            psaTotal, cgcTotal, observedAt, qualifying);

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
        Assert.All(cut.FindAll(".ga-metric-note"), n => Assert.Equal(
            $"needs census deltas; observations count from 2026-09-01, {qualifying} so far — deltas need two",
            n.TextContent));
    }
}
