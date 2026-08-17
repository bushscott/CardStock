using Bunit;
using CardStock.Domain.Signals;
using CardStock.Web.Components.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class CatalogKitTests : BunitContext
{
    [Fact]
    public void PendingGlyph_is_keyboard_reachable_and_carries_its_note()
    {
        var cut = Render<PendingGlyph>(p => p.Add(x => x.Note, "Arrives with the analytics worker"));
        var span = cut.Find("span.gate-glyph");
        Assert.Equal("◌", span.TextContent);
        Assert.Equal("0", span.GetAttribute("tabindex"));
        Assert.Equal("Arrives with the analytics worker", span.GetAttribute("title"));
        Assert.Equal("Arrives with the analytics worker", span.GetAttribute("aria-label"));
    }

    [Fact]
    public void SortState_flips_on_repeat_and_resets_on_key_change()
    {
        var sort = new SortState("value");
        Assert.True(sort.Descending);
        sort.Apply("value");
        Assert.False(sort.Descending);
        sort.Apply("roc");
        Assert.Equal("roc", sort.Key);
        Assert.True(sort.Descending);
    }

    [Fact]
    public void DensityToggle_marks_the_active_side_with_aria_pressed()
    {
        string value = "terminal";
        var cut = Render<DensityToggle>(p => p
            .Add(x => x.LeftKey, "terminal").Add(x => x.LeftLabel, "terminal")
            .Add(x => x.LeftTooltip, "Terminal density — more rows, tighter type, every metric column")
            .Add(x => x.RightKey, "binder").Add(x => x.RightLabel, "binder")
            .Add(x => x.RightTooltip, "Binder density — fewer rows with card art")
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        var buttons = cut.FindAll("button");
        Assert.Equal("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", buttons[1].GetAttribute("aria-pressed"));

        buttons[1].Click();
        Assert.Equal("binder", value);
    }

    [Fact]
    public void A_deferred_pill_is_disabled_with_the_honest_tooltip_and_never_sorts()
    {
        var sort = new SortState("value");
        var pills = new[]
        {
            new SortPill("value", "value", "Sort by value", false, null),
            new SortPill("rs", "RS", "Sort by RS", true, CatalogCopy.WorkerGate),
        };
        var cut = Render<SortPills>(p => p
            .Add(x => x.Pills, pills).Add(x => x.Sort, sort));

        var rs = cut.FindAll("button")[1];
        Assert.True(rs.HasAttribute("disabled"));
        Assert.Equal(CatalogCopy.WorkerGate, rs.GetAttribute("title"));
        Assert.Equal("value", sort.Key);
    }

    [Fact]
    public void The_deferred_index_block_prints_labels_dashes_and_one_glyph_no_fake_line()
    {
        var cut = Render<DeferredIndexBlock>(p => p.Add(x => x.Caption, "set index · 12M"));
        Assert.Contains("set index · 12M", cut.Markup);
        Assert.Single(cut.FindAll("span.gate-glyph"));
        Assert.Empty(cut.FindAll("svg polyline"));
        var deltas = cut.FindAll(".dib-delta-value");
        Assert.Equal(2, deltas.Count);
        Assert.All(deltas, d => Assert.Equal(ChipEngine.GlyphDash, d.TextContent));
    }
}
