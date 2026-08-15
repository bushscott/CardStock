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
        CensusMetricDto? gemRate = null, CensusMetricDto? pace = null,
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
            gemRate ?? GemSkeleton(),
            pace ?? PaceSkeleton(qualifying),
            deltaBars ?? SevenGhosts());

    private static MetricSegmentDto Seg(string text, string tone = "neutral", bool mono = false) =>
        new(text, tone, mono);

    // D-102: the sentences are always printed — the skeleton with – in the value
    // runs and the gate note riding the ◌ tooltip, exactly what the wire computes
    // for every card today.
    private const string GemGate =
        "needs 90 days of census deltas; observations count from 09-01-2026 — the window fills 11-30-2026";

    private static CensusMetricDto GemSkeleton() =>
        new("lowdata",
        [
            Seg("–", mono: true),
            Seg(" — of the last 90 days of PSA submissions, the share that came back 10."),
        ], GemGate);

    private static CensusMetricDto PaceSkeleton(int qualifying) =>
        new("lowdata",
        [
            Seg("– / mo", mono: true),
            Seg(" — "),
            Seg("–", mono: true),
            Seg(" new 10s since Sep ’26."),
        ], $"needs census deltas; observations count from 09-01-2026, {qualifying} so far — deltas need two");

    private static IReadOnlyList<CensusDeltaBarDto> SevenGhosts()
    {
        var labels = new[] { "Sep ’26", "Oct ’26", "Nov ’26", "Dec ’26", "Jan ’27", "Feb ’27", "Mar ’27" };
        return
        [
            .. labels.Select((label, i) => new CensusDeltaBarDto(
                new DateOnly(2026, 9, 1).AddMonths(i).ToString("yyyy-MM"), label, "pending", null,
                $"new PSA 10 slabs for {label} — closes {CardStock.Domain.Dates.Full(new DateOnly(2026, 10, 1).AddMonths(i))}")),
        ];
    }

    private static string SentenceText<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent =>
        string.Concat(cut.FindAll(".cs-seg").Select(e => e.TextContent));

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
    public void All_zero_census_renders_six_4px_stubs()
    {
        var census = SixBars(0, 0, 0, 0, 0, 0);

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        var bars = cut.FindAll(".census-bar");
        Assert.Equal(6, bars.Count);
        Assert.All(bars, bar => Assert.Equal("height: 4px;", bar.GetAttribute("style")));
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
    public void Header_sub_carries_the_full_grader_totals_and_the_all_grades_tooltip()
    {
        // D-102 relocated D-084.4's totals from the summary line into the header
        // sub, keeping the honesty framing on a tooltip: the six bars are a
        // slice, the totals are every grade.
        var census = SixBars(
            1, 1, 1, 1, 1, 1, psaTotal: 15931, cgcTotal: 618,
            observedAt: new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero));

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        var sub = cut.Find(".census-sub");
        Assert.Equal("15,931 PSA · 618 CGC · as of 07-30-2026", sub.TextContent);
        Assert.Equal("Totals across every grade, not only the six bars shown", sub.GetAttribute("title"));
    }

    [Fact]
    public void Header_sub_reads_never_observed_when_observed_at_is_null()
    {
        var census = SixBars(0, 0, 0, 0, 0, 0, observedAt: null);

        var cut = Render<CensusBars>(p => p.Add(x => x.Census, census));

        Assert.Equal("PSA + CGC · never observed", cut.Find(".census-sub").TextContent);
    }

    [Fact]
    public void Population_panel_prints_the_gem_sentence_with_the_gate_glyph_below_data()
    {
        // D-102: the mockup's :232 sentence is permanent copy at the panel foot —
        // the value run is the – glyph and the ◌ carries the unlock tooltip.
        var cut = Render<CensusBars>(p => p.Add(x => x.Census, SixBars(0, 0, 0, 0, 0, 0)));

        Assert.Equal("Gem rate", cut.Find(".cs-name").TextContent);
        var gate = cut.Find(".cs-gate");
        Assert.Equal("◌", gate.TextContent);
        Assert.Equal(GemGate, gate.GetAttribute("title"));
        Assert.Equal(
            "– — of the last 90 days of PSA submissions, the share that came back 10.",
            SentenceText(cut));
        var dash = cut.FindAll(".cs-seg")[0];
        Assert.Contains("cs-mono", dash.ClassList);
    }

    [Fact]
    public void Population_panel_prints_the_computed_gem_sentence_without_the_glyph()
    {
        var computed = new CensusMetricDto("ok",
        [
            Seg("27.0%", mono: true),
            Seg(" — of the last 90 days of PSA submissions, the share that came back 10."),
            Seg(" Drifting "),
            Seg("−0.4pp / 90d", tone: "pos", mono: true),
            Seg(" (harder to gem = supply of fresh 10s slowing)."),
        ], GateNote: null);
        var cut = Render<CensusBars>(p => p.Add(x => x.Census,
            SixBars(0, 0, 1479, 0, 0, 0, gemRate: computed)));

        Assert.Empty(cut.FindAll(".cs-gate"));
        Assert.StartsWith("27.0% — of the last 90 days", SentenceText(cut));
        var drift = cut.FindAll(".cs-seg").Single(e => e.TextContent == "−0.4pp / 90d");
        Assert.Contains("cs-tone-pos", drift.ClassList);
        Assert.Contains("cs-mono", drift.ClassList);
    }

    [Theory]
    [InlineData(0, "0 OBS", "Census observations counted from 09-01-2026 — 0 so far; deltas need two.")]
    [InlineData(5, "5 OBS", "Census observations counted from 09-01-2026 — 5 so far; deltas need two.")]
    public void Grading_activity_badge_counts_qualifying_observations(
        int qualifying, string expectedBadge, string expectedTooltip)
    {
        var census = SixBars(0, 0, 0, 0, 0, 0, qualifying: qualifying);

        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census, census));

        Assert.Contains("Grading activity · PSA 10 slabs added", cut.Find("h2").TextContent);
        var badge = cut.Find(".obs-badge");
        Assert.Equal(expectedBadge, badge.TextContent);
        Assert.Equal(expectedTooltip, badge.GetAttribute("title"));
    }

    [Fact]
    public void Grading_activity_prints_the_pace_sentence_and_no_slot_rows()
    {
        // D-102: the metric-slot stack is gone — the panel's foot is the mockup's
        // :248 sentence, dashed until the wire computes it.
        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census,
            SixBars(0, 0, 0, 0, 0, 0, qualifying: 1)));

        Assert.Empty(cut.FindAll(".ga-metric"));
        Assert.Empty(cut.FindAll(".ga-state"));
        Assert.Equal("Pace", cut.Find(".cs-name").TextContent);
        Assert.Equal(
            "needs census deltas; observations count from 09-01-2026, 1 so far — deltas need two",
            cut.Find(".cs-gate").GetAttribute("title"));
        Assert.Equal("– / mo — – new 10s since Sep ’26.", SentenceText(cut));
    }

    [Fact]
    public void Grading_activity_prints_the_computed_pace_sentence_with_market_tones()
    {
        var computed = new CensusMetricDto("ok",
        [
            Seg("+58 / mo", mono: true),
            Seg(" and rising — "),
            Seg("331", mono: true),
            Seg(" new 10s since Sep ’26"),
            Seg(", growing the census "),
            Seg("+29%", tone: "neg", mono: true),
            Seg(" in 7 months "),
            Seg("(fresh supply working against the price)."),
        ], GateNote: null);
        var cut = Render<GradingActivityPanel>(p => p.Add(x => x.Census,
            SixBars(0, 0, 1479, 0, 0, 0, qualifying: 7, pace: computed)));

        Assert.Empty(cut.FindAll(".cs-gate"));
        Assert.Equal(
            "+58 / mo and rising — 331 new 10s since Sep ’26, growing the census +29% in 7 months " +
            "(fresh supply working against the price).",
            SentenceText(cut));
        var growth = cut.FindAll(".cs-seg").Single(e => e.TextContent == "+29%");
        Assert.Contains("cs-tone-neg", growth.ClassList);
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
            "new PSA 10 slabs for Sep ’26 — closes 10-01-2026", ghosts[0].GetAttribute("title"));
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
            new("2026-11", "Nov ’26", "pending", null, "new PSA 10 slabs for Nov ’26 — closes 12-01-2026"),
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
}
