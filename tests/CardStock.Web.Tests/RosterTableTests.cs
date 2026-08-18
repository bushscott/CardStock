using Bunit;
using CardStock.Web.Components.Catalog;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

    // D-117 (owner UAT): the first column is the flexible track, so its own grip was dead —
    // shrinking a minmax minimum changes nothing while the fr share fills the container.
    // Ruled behavior: the name grip redistributes. Dragging left grows the fixed columns
    // by the dragged amount, split proportional to their drag-start widths, and the
    // stored minimum tracks the drag; the fr share visibly shrinks by the same amount.
    // Other grips stay single-column, now under the spec's 420 ceiling (§4.1).
    [Fact]
    public void The_name_grip_redistributes_and_other_grips_stay_single_column()
    {
        var cut = Render(new SortState("value"));

        // re-find per event: every dispatch re-renders and stales the previous reference
        cut.FindAll(".rt-grip")[0].PointerDown(new PointerEventArgs { ClientX = 500 });
        cut.FindAll(".rt-grip")[0].PointerMove(new PointerEventArgs { ClientX = 408 });   // 92px left
        cut.FindAll(".rt-grip")[0].PointerUp(new PointerEventArgs());
        // others started 100 + 84 = 184 → +50 and +42; the name minimum 230 → 138.
        Assert.Contains("minmax(138px, 1.4fr) 150px 126px",
            cut.Find(".rt-head").GetAttribute("style"));

        cut.FindAll(".rt-grip")[1].PointerDown(new PointerEventArgs { ClientX = 500 });
        cut.FindAll(".rt-grip")[1].PointerMove(new PointerEventArgs { ClientX = 1100 });  // +600 → ceiling
        Assert.Contains("420px", cut.Find(".rt-head").GetAttribute("style"));
    }

    private sealed class CountingRows(IReadOnlyList<Row> inner) : IReadOnlyList<Row>
    {
        public int Enumerations;
        public Row this[int i] => inner[i];
        public int Count => inner.Count;
        public IEnumerator<Row> GetEnumerator() { Enumerations++; return inner.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // D-118 (owner UAT, the scroll snap): Virtualize must see one identity-stable Items
    // instance per Rows instance. A fresh materialization on every render resets its
    // internal anchoring — the snap-to-top at the table's foot.
    [Fact]
    public void Rows_materialize_once_per_instance_not_once_per_render()
    {
        var rows = new CountingRows([new Row("A", 1), new Row("B", 2)]);
        var cut = Render<RosterTable<Row>>(p => p
            .Add(x => x.Columns, Columns())
            .Add(x => x.Rows, rows)
            .Add(x => x.Sort, new SortState("value")));

        cut.FindAll(".rt-grip")[1].PointerDown(new PointerEventArgs { ClientX = 500 });
        cut.FindAll(".rt-grip")[1].PointerMove(new PointerEventArgs { ClientX = 520 });   // re-renders
        cut.FindAll(".rt-grip")[1].PointerMove(new PointerEventArgs { ClientX = 540 });

        Assert.Equal(1, rows.Enumerations);
    }
}
