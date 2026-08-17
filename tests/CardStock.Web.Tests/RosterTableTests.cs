using Bunit;
using CardStock.Web.Components.Catalog;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Xunit;

namespace CardStock.Web.Tests;

// task-9 brief: the virtualized terminal table both rosters share. bUnit.JSInterop runs in
// loose mode (set below, mirroring PriceChartTests/CardPageTests) since OnAfterRenderAsync
// imports ./js/catalog.js and calls installGripCapture on every mount -- these tests assert
// on the rendered C# side (headers, sort state, grid template, row count), not the grip drag.
public class RosterTableTests : BunitContext
{
    public RosterTableTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private sealed record Row(string Name, int Value);

    private static RenderFragment<Row> Text(Func<Row, string> f) =>
        row => builder => builder.AddContent(0, f(row));

    private static IReadOnlyList<RosterColumn<Row>> Columns() =>
    [
        new("name", "Card", 230, "Card name", Sortable: false, Deferred: false, null, Text(r => r.Name)),
        new("value", "PSA 10", 100, "Latest monthly PSA 10 price — click to sort",
            Sortable: true, Deferred: false, null, Text(r => r.Value.ToString())),
        new("rs", "RS pct", 84, "Relative strength", Sortable: false, Deferred: true,
            "Arrives with the analytics worker", Text(_ => "–")),
    ];

    private IRenderedComponent<RosterTable<Row>> Render(SortState sort) =>
        Render<RosterTable<Row>>(p => p
            .Add(x => x.Columns, Columns())
            .Add(x => x.Rows, new[] { new Row("A", 1), new Row("B", 2) })
            .Add(x => x.Sort, sort));

    [Fact]
    public void A_sortable_header_sorts_and_shows_the_arrow()
    {
        var sort = new SortState("value");
        var cut = Render(sort);
        var header = cut.FindAll(".rt-head-cell")[1];
        Assert.Contains("▾", header.TextContent);

        header.Click();
        Assert.False(sort.Descending);
        Assert.Contains("▴", Render(sort).FindAll(".rt-head-cell")[1].TextContent);
    }

    [Fact]
    public void An_unsortable_header_has_no_pointer_affordance()
    {
        var sort = new SortState("value");
        var cut = Render(sort);
        var name = cut.FindAll(".rt-head-cell")[0];
        Assert.DoesNotContain("sortable", name.ClassList);
        name.Click();                                   // wired to nothing
        Assert.Equal("value", sort.Key);
        Assert.True(sort.Descending);
    }

    [Fact]
    public void A_deferred_header_carries_the_glyph_and_never_sorts()
    {
        var sort = new SortState("value");
        var cut = Render(sort);
        var rs = cut.FindAll(".rt-head-cell")[2];
        Assert.Single(rs.QuerySelectorAll("span.gate-glyph"));

        rs.Click();
        Assert.Equal("value", sort.Key);
    }

    [Fact]
    public void The_grid_template_follows_the_column_widths_with_the_name_track_elastic()
    {
        var cut = Render(new SortState("value"));
        var head = cut.Find(".rt-head");
        Assert.Contains("minmax(230px, 1.4fr) 100px 84px", head.GetAttribute("style"));
    }

    [Fact]
    public void Rows_render_through_the_virtualizer()
    {
        var cut = Render(new SortState("value"));
        Assert.Equal(2, cut.FindAll(".rt-row").Count);
        Assert.Contains("A", cut.Markup);
    }
}
