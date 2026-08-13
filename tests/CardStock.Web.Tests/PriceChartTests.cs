using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

// card.md §2.4/§2.4.1/§5.4, task-17-brief's PriceChart contract. bUnit.JSInterop runs in loose
// mode (BunitContext's default): lwcInterop.init/setData/dotY calls the component makes during
// its render lifecycle are auto-satisfied with default return values, so these tests only need
// to assert on the rendered C# side -- the DOM structure, legend state, and the [JSInvokable]
// crosshair callback invoked directly, per the brief.
public class PriceChartTests : BunitContext
{
    public PriceChartTests()
    {
        // The component drives lwcInterop.init/setData/dotY on every render; loose mode
        // auto-satisfies those calls with default return values so tests can assert on the
        // C# side (DOM structure, legend/toggle state, the [JSInvokable] callback) without
        // configuring a return value for every call, per the task-17 brief.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly string[] Months =
    [
        "2025-09", "2025-10", "2025-11", "2025-12", "2026-01", "2026-02",
        "2026-03", "2026-04", "2026-05", "2026-06", "2026-07", "2026-08",
    ];

    private static readonly int?[] AllNull = new int?[12];

    private static TierDto Tier(string tier, string label, params int?[] cents)
    {
        var points = Enumerable.Range(0, 12).Select(i => new PointDto(Months[i], cents[i])).ToList();
        return new TierDto(tier, label, points, new TierPriceDto("none", null, null, null),
            new TierChangeDto("insufficient", null, 0, 0));
    }

    // All six tiers present: PSA 10 and Grade 9 carry data (including a current-month value on
    // PSA 10, so the dot hotspot has something to track); Grade 9.5 carries a closed-month value
    // only; Grade 8, Grade 7 and Raw carry no data at all (the muted no-data legend case).
    private static PricesDto SixTiers()
    {
        var psa = (int?[])AllNull.Clone();
        psa[5] = 140000;
        psa[11] = 148600;
        var grade9 = (int?[])AllNull.Clone();
        grade9[5] = 50000;
        var grade95 = (int?[])AllNull.Clone();
        grade95[3] = 90000;

        return new PricesDto("2026-08",
        [
            Tier("Ungraded", "Raw", AllNull),
            Tier("Grade7", "Grade 7", AllNull),
            Tier("Grade8", "Grade 8", AllNull),
            Tier("Grade9", "Grade 9", grade9),
            Tier("Grade9Half", "Grade 9.5", grade95),
            Tier("Psa10", "PSA 10", psa),
        ]);
    }

    [Fact]
    public void Legend_renders_six_chips()
    {
        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, SixTiers()));

        Assert.Equal(6, cut.FindAll(".pc-legend-chip").Count);
    }

    [Fact]
    public void Toggling_below_one_visible_tier_is_a_silent_no_op()
    {
        // Defaults: PSA 10 + Grade 9 + Raw visible (card.md; task-17 brief). Hide Grade 9 and
        // Raw, leaving PSA 10 the sole visible series, then try to hide PSA 10 too.
        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, SixTiers()));

        cut.Find("button.pc-legend-chip[data-tier='Grade9']").Click();
        cut.Find("button.pc-legend-chip[data-tier='Ungraded']").Click();
        var psaChip = cut.Find("button.pc-legend-chip[data-tier='Psa10']");
        Assert.Equal("true", psaChip.GetAttribute("aria-pressed"));

        psaChip.Click(); // attempted hide of the last visible series -- must no-op

        psaChip = cut.Find("button.pc-legend-chip[data-tier='Psa10']");
        Assert.Equal("true", psaChip.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Muted_no_data_chip_carries_its_tooltip_and_is_disabled()
    {
        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, SixTiers()));

        var grade7Chip = cut.Find("button.pc-legend-chip[data-tier='Grade7']");
        Assert.Equal("no Grade 7 prices observed", grade7Chip.GetAttribute("title"));
        Assert.True(grade7Chip.HasAttribute("disabled"));
        Assert.Contains("pc-legend-chip--muted", grade7Chip.ClassList);
    }

    [Fact]
    public async Task Crosshair_tooltip_renders_one_row_per_visible_series_with_a_value_that_month()
    {
        // Index 5 = 2026-02, where both PSA 10 and Grade 9 (the two data-bearing default-visible
        // series in SixTiers()) carry a value; Raw (also default-visible) has none anywhere.
        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, SixTiers()));

        // The [JSInvokable] target calls StateHasChanged(), which requires the renderer's own
        // dispatcher -- cut.InvokeAsync marshals onto it, matching how the real JS callback
        // would arrive. This is still a direct call to OnCrosshairMonth, per the brief.
        await cut.InvokeAsync(() => cut.Instance.OnCrosshairMonth(5));

        var rows = cut.FindAll(".pc-tooltip-row");
        Assert.Equal(2, rows.Count); // PSA 10 + Grade 9 are the default-visible series with a value at index 5
        Assert.Contains("PSA 10 $1,400", rows[0].TextContent);
        Assert.Contains("pc-tooltip-row--bold", rows[0].ClassList);
        Assert.Contains("Grade 9 $500", rows[1].TextContent);
        Assert.DoesNotContain("pc-tooltip-row--bold", rows[1].ClassList);
    }

    [Fact]
    public async Task Crosshair_tooltip_absent_until_a_month_is_hovered()
    {
        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, SixTiers()));

        Assert.Empty(cut.FindAll(".pc-tooltip"));

        await cut.InvokeAsync(() => cut.Instance.OnCrosshairMonth(5));
        Assert.Single(cut.FindAll(".pc-tooltip"));

        await cut.InvokeAsync(() => cut.Instance.OnCrosshairMonth(null));
        Assert.Empty(cut.FindAll(".pc-tooltip"));
    }

    [Fact]
    public void Dot_hotspot_present_when_a_default_visible_tier_has_a_current_month_value()
    {
        // PSA 10 (default-visible) has a current-month (index 11) value in this fixture.
        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, SixTiers()));

        Assert.Single(cut.FindAll(".pc-dot"));
    }

    [Fact]
    public void Dot_hotspot_absent_when_no_visible_tier_has_a_current_month_value()
    {
        var noCurrentMonth = new PricesDto("2026-08",
        [
            Tier("Ungraded", "Raw", AllNull),
            Tier("Grade7", "Grade 7", AllNull),
            Tier("Grade8", "Grade 8", AllNull),
            Tier("Grade9", "Grade 9", AllNull),
            Tier("Grade9Half", "Grade 9.5", AllNull),
            Tier("Psa10", "PSA 10", AllNull),
        ]);

        var cut = Render<PriceChart>(p => p.Add(x => x.Prices, noCurrentMonth));

        Assert.Empty(cut.FindAll(".pc-dot"));
    }
}
