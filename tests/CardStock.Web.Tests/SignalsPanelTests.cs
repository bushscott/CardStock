using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

public class SignalsPanelTests : BunitContext
{
    private static SignalRowDto Row(
        string name, string state = "quiet", string tone = "neutral",
        string glyph = "–", string value = "+1%", string? tooltip = null) =>
        new(glyph, name, value, tooltip ?? $"{name} tooltip", state, tone);

    /// <summary>Twelve rows — more than the mockup's eight, because the eight are a
    /// sample, not a cap (owner ruling 1, D-092).</summary>
    private static SignalsDto TwelveRows() => new(12, 2, 2, 0, 0,
    [
        Row("ROC 3M", state: "firing", tone: "pos", glyph: "▲", value: "+18%"),
        Row("Trend R²", state: "firing", tone: "pos", glyph: "▲", value: ".99"),
        Row("Sales volume", state: "neutral", glyph: "●", value: "3 / 30d"),
        Row("Drawdown", value: "0%"),
        Row("MACD (3,6,4)", state: "belowfloor", value: "—"),
        Row("EMA 3/9 cross", state: "belowfloor", value: "—"),
        Row("RSI (6)", state: "belowfloor", value: "—"),
        Row("z vs 6M", state: "belowfloor", value: "—"),
        Row("Tier spread 10/9", state: "belowfloor", value: "—"),
        Row("RS vs index 3M", state: "locked", glyph: "◌", value: "locked",
            tooltip: "Relative strength needs the market index — it arrives with the worker phase"),
        Row("Pop Δ 60d", state: "locked", glyph: "◌", value: "locked"),
        Row("Churn 30d", state: "locked", glyph: "◌", value: "unlocks 10-31-2026",
            tooltip: "Needs 60+ post-seam days · 0 recorded"),
    ]);

    // D-128: the count line breaks the firing total down by tone — glyphs carry the
    // meaning, zero segments are omitted, and no breakdown renders when nothing fires.
    [Fact]
    public void The_count_line_breaks_firing_down_by_tone_and_omits_zeros()
    {
        var mixed = new SignalsDto(12, 5, 3, 1, 1,
            [Row("ROC 3M", state: "firing", tone: "pos", glyph: "▲")]);
        var cut = Render<SignalsPanel>(p => p.Add(x => x.Signals, mixed));
        var count = cut.Find(".sig-count");
        Assert.Equal("▲3", count.QuerySelector(".sig-bd-chip-pos")!.TextContent);
        Assert.Equal("▼1", count.QuerySelector(".sig-bd-chip-neg")!.TextContent);
        Assert.Equal("–1", count.QuerySelector(".sig-bd-chip-warn")!.TextContent);
        Assert.Contains("3 bullish, 1 bearish, 1 caution", count.GetAttribute("title"));

        var bullOnly = new SignalsDto(12, 2, 2, 0, 0, []);
        var cut2 = Render<SignalsPanel>(p => p.Add(x => x.Signals, bullOnly));
        Assert.NotNull(cut2.Find(".sig-count .sig-bd-chip-pos"));
        Assert.Empty(cut2.FindAll(".sig-bd-chip-neg"));
        Assert.Empty(cut2.FindAll(".sig-bd-chip-warn"));

        var none = new SignalsDto(12, 0, 0, 0, 0, []);
        var cut3 = Render<SignalsPanel>(p => p.Add(x => x.Signals, none));
        Assert.Empty(cut3.FindAll(".sig-bd-chip"));
    }

    [Fact]
    public void Every_row_renders_unbounded_no_fold_no_cap()
    {
        var cut = Render<SignalsPanel>(p => p.Add(x => x.Signals, TwelveRows()));

        // Ruling 1: 10+ rows all render; nothing folds in Phase 2, so the
        // "+N quiet" element does not exist at all.
        Assert.Equal(12, cut.FindAll(".sig-row").Count);
        Assert.Empty(cut.FindAll(".sig-quiet-more"));
    }

    [Fact]
    public void The_count_line_is_the_wire_truth_with_the_authored_tooltip()
    {
        var cut = Render<SignalsPanel>(p => p.Add(x => x.Signals, TwelveRows()));

        var count = cut.Find(".sig-count");
        Assert.Equal("12 evaluated · 2 firing ▲2", count.TextContent);
        // D-128: the tooltip leads with the breakdown in words, then the authored copy.
        Assert.StartsWith("Firing now: 2 bullish, 0 bearish, 0 caution. Every chip-eligible signal is evaluated",
            count.GetAttribute("title"));
        Assert.Equal(count.GetAttribute("title"), count.GetAttribute("aria-label"));
        Assert.Equal("0", count.GetAttribute("tabindex"));
    }

    [Fact]
    public void Rows_carry_state_classes_glyph_text_and_tooltips()
    {
        var cut = Render<SignalsPanel>(p => p.Add(x => x.Signals, TwelveRows()));
        var rows = cut.FindAll(".sig-row");

        Assert.Contains("sig-firing", rows[0].ClassList);
        Assert.Contains("sig-tone-pos", rows[0].ClassList);
        Assert.Equal("▲", rows[0].QuerySelector(".sig-glyph")!.TextContent);
        Assert.Equal("ROC 3M", rows[0].QuerySelector(".sig-name")!.TextContent);
        Assert.Equal("+18%", rows[0].QuerySelector(".sig-value")!.TextContent);

        Assert.Contains("sig-locked", rows[9].ClassList);
        Assert.Equal("◌", rows[9].QuerySelector(".sig-glyph")!.TextContent);
        Assert.Equal(
            "Relative strength needs the market index — it arrives with the worker phase",
            rows[9].GetAttribute("title"));

        Assert.Equal("unlocks 10-31-2026", rows[11].QuerySelector(".sig-value")!.TextContent);
        Assert.Equal("Needs 60+ post-seam days · 0 recorded", rows[11].GetAttribute("title"));
    }

    [Fact]
    public void The_charts_link_is_deferred_disabled_and_nothing_is_an_anchor()
    {
        var cut = Render<SignalsPanel>(p => p.Add(x => x.Signals, TwelveRows()));

        Assert.Empty(cut.FindAll("a"));
        var link = cut.Find(".sig-charts-link");
        Assert.True(link.HasAttribute("disabled"));
        Assert.Equal("all signals in Charts →", link.TextContent);
        Assert.Equal("Charts arrives in a later phase", link.GetAttribute("title"));
    }

    [Fact]
    public void The_panel_carries_a_scoped_stylesheet()
    {
        var cut = Render<SignalsPanel>(p => p.Add(x => x.Signals, TwelveRows()));

        var panel = cut.Find(".sig-panel");
        Assert.Contains(panel.Attributes, a => a.Name.StartsWith("b-"));
    }
}
