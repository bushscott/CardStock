using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

// card.md §2.5/§3.7/§4.4-§4.6/§5.5, task-18 brief. bUnit.JSInterop runs in loose mode (as
// PriceChartTests does, task-17): watchOutsideMousedown/startColumnDrag calls the component
// makes during its render lifecycle are auto-satisfied, so these tests assert on the C# side --
// DOM structure, filter/count state, and encoding -- without configuring every JS call.
public class SalesLedgerTests : BunitContext
{
    public SalesLedgerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static SaleDto Sale(string date, string bucket, int priceCents, string src = "ebay",
        string title = "listing", int? listedCents = null) =>
        new(DateOnly.Parse(date), bucket, priceCents, listedCents, src, title);

    [Fact]
    public void Headers_render_their_labels_and_the_active_sort_arrow_not_razor_source()
    {
        // 'Date@Arrow("date")' hit Razor's email-address heuristic — an @ between word
        // characters never transitions to C#, so every header printed its own source
        // text verbatim. The explicit @(...) form is the fix; this pins the rendered text.
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, new[] { Sale("2026-08-01", "PSA 10", 100) }));

        var headers = cut.FindAll(".lg-h-label").Select(h => h.TextContent).ToList();
        Assert.Equal(["Date ▾", "Grade bucket", "Realized", "Source", "Listing title"], headers);

        // Flip the active column: ascending arrow replaces the descending one.
        cut.FindAll(".lg-h")[0].Click();
        Assert.Equal("Date ▴", cut.FindAll(".lg-h-label")[0].TextContent);
    }

    [Fact]
    public void Header_row_has_exactly_five_grid_children_with_grips_nested_inside_them()
    {
        // The header row shares the body's five-track grid-template-columns, so it must
        // have exactly five direct children — grips rendered as SIBLINGS ate grid tracks,
        // shoving every later header over the wrong column and wrapping the overflow onto
        // a second row (found live 2026-08-13). Grips live INSIDE their header cells.
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, new[] { Sale("2026-08-01", "PSA 10", 100) }));

        var header = cut.Find(".lg-header");
        Assert.Equal(5, header.Children.Length);
        Assert.All(cut.FindAll(".lg-grip"), grip => Assert.Contains("lg-h", grip.ParentElement!.ClassList));
        Assert.Equal(4, cut.FindAll(".lg-grip").Count);   // columns 1–4 only; the title has none

        // §5.5 row 31: interacting with a grip must never trigger the header's sort. The
        // grip stops click propagation and handles no click itself, so bUnit reports the
        // click as reaching no handler — remove the stopPropagation and this click would
        // bubble into SortBy and the assertion below would fail instead of throwing.
        Assert.Throws<MissingEventHandlerException>(() => cut.FindAll(".lg-grip")[0].Click());
        Assert.Equal("Date ▾", cut.FindAll(".lg-h-label")[0].TextContent);
    }

    [Fact]
    public void A_chip_click_filters_the_rows_and_updates_the_count()
    {
        var sales = new[]
        {
            Sale("2026-08-01", "PSA 10", 100),
            Sale("2026-08-02", "PSA 10", 200),
            Sale("2026-08-03", "Grade 9", 300),
        };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));
        Assert.Equal("3 sales · newest 300 per grade", cut.Find(".lg-count").TextContent);

        cut.Find("button[data-bucket='PSA 10']").Click();

        Assert.Equal("2 sales · newest 300 per grade", cut.Find(".lg-count").TextContent);
    }

    [Fact]
    public void The_all_chip_resets_the_selection_and_closes_both_group_popovers()
    {
        var sales = new[] { Sale("2026-08-01", "PSA 10", 100), Sale("2026-08-02", "Grade 9", 200) };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        cut.Find("button[data-bucket='PSA 10']").Click();
        cut.FindAll("button.lg-chip").First(b => b.TextContent.StartsWith("other 10s")).Click();
        Assert.NotEmpty(cut.FindAll(".lg-popover"));
        Assert.Equal("1 sales · newest 300 per grade", cut.Find(".lg-count").TextContent);

        cut.FindAll("button.lg-chip").First(b => b.TextContent == "All").Click();

        Assert.Equal("2 sales · newest 300 per grade", cut.Find(".lg-count").TextContent);
        Assert.Empty(cut.FindAll(".lg-popover"));
    }

    [Fact]
    public void Empty_state_names_the_single_selected_grade()
    {
        var sales = new[] { Sale("2026-08-01", "Grade 9", 100) }; // no PSA 10 rows at all
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        cut.Find("button[data-bucket='PSA 10']").Click();

        Assert.Equal(
            "No sales observed in this grade — that's a true zero: our scrapers visited and found none.",
            cut.Find(".lg-empty").TextContent);
    }

    [Fact]
    public void Empty_state_names_several_selected_grades()
    {
        var sales = new[] { Sale("2026-08-01", "Grade 9", 100) };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        cut.Find("button[data-bucket='PSA 10']").Click();
        cut.Find("button[data-bucket='Grade 7']").Click();

        Assert.Equal(
            "No sales observed in these grades — that's a true zero: our scrapers visited and found none.",
            cut.Find(".lg-empty").TextContent);
    }

    [Fact]
    public void Empty_state_scopes_to_the_whole_card_when_no_filter_is_selected()
    {
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, Array.Empty<SaleDto>()));

        Assert.Equal(
            "No sales observed for this card — that's a true zero: our scrapers visited and found none.",
            cut.Find(".lg-empty").TextContent);
    }

    [Fact]
    public void The_listed_price_underline_appears_only_on_rows_that_have_a_listed_price()
    {
        var sales = new[]
        {
            Sale("2026-08-01", "PSA 10", 100, listedCents: 150),
            Sale("2026-08-02", "PSA 10", 200), // no listed price
        };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".lg-price").Count));
        Assert.Single(cut.FindAll(".lg-listed"));
        var listed = cut.Find(".lg-listed");
        Assert.Equal("listed $2 → sold $1", listed.GetAttribute("title"));

        // The underline element wraps ONLY the price text, nested inside the cell —
        // a border on the cell itself would stretch across the whole grid track.
        Assert.Contains("lg-price", listed.ParentElement!.ClassList);
        Assert.Equal("$1", listed.TextContent);
    }

    [Fact]
    public void A_hostile_listing_title_renders_html_encoded_never_as_live_markup()
    {
        var sales = new[] { Sale("2026-08-01", "PSA 10", 100, title: "Charizard <script>alert(1)</script>") };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        cut.WaitForAssertion(() => Assert.Contains("&lt;script&gt;", cut.Markup));
        Assert.Empty(cut.FindAll("script"));
    }

    [Fact]
    public void Disposing_removes_its_outside_mousedown_listener_through_the_js_teardown()
    {
        var sales = new[] { Sale("2026-08-01", "PSA 10", 100) };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        // Every mount registers a permanent `document` listener (OnAfterRenderAsync ->
        // watchOutsideMousedown); ordinary card-to-card navigation tears down and rebuilds this
        // component on every Id change, so it must actually be removed on disposal, not just
        // orphaned. Locks the convention mirrored from PriceChart.Dispose (task-17).
        Assert.IsAssignableFrom<IDisposable>(cut.Instance);

        Renderer.DisposeComponents();

        JSInterop.VerifyInvoke("unwatchOutsideMousedown");
    }

    [Fact]
    public void Twenty_five_row_pages_with_a_pager_when_the_set_overflows()
    {
        // D-090: display pages at 50 rows; filters/sorts still act on the whole set.
        var sales = Enumerable.Range(0, 120)
            .Select(i => Sale("2026-08-01", "PSA 10", (i + 1) * 100))
            .ToArray();
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        Assert.Equal(25, cut.FindAll(".lg-body").Count);
        Assert.Equal("Rows 1–25 of 120", cut.Find(".lg-page-label").TextContent);
        Assert.True(cut.FindAll(".lg-page-btn")[0].HasAttribute("disabled"));   // Prev at start

        cut.FindAll(".lg-page-btn")[1].Click();                                 // Next
        Assert.Equal("Rows 26–50 of 120", cut.Find(".lg-page-label").TextContent);

        cut.FindAll(".lg-page-btn")[1].Click();
        cut.FindAll(".lg-page-btn")[1].Click();
        cut.FindAll(".lg-page-btn")[1].Click();
        Assert.Equal("Rows 101–120 of 120", cut.Find(".lg-page-label").TextContent);
        Assert.Equal(20, cut.FindAll(".lg-body").Count);                        // the tail page
        Assert.True(cut.FindAll(".lg-page-btn")[1].HasAttribute("disabled"));   // Next at end

        // A filter change lands back on page one of the new set.
        cut.Find("button[data-bucket='PSA 10']").Click();
        Assert.Equal("Rows 1–25 of 120", cut.Find(".lg-page-label").TextContent);
    }

    [Fact]
    public void No_pager_renders_when_the_set_fits_one_page()
    {
        var sales = new[] { Sale("2026-08-01", "PSA 10", 100) };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        Assert.Empty(cut.FindAll(".lg-pager"));
    }

    [Fact]
    public void A_null_sales_list_renders_the_panel_error_state_and_retry_invokes_the_callback()
    {
        var retried = false;
        var cut = Render<SalesLedger>(p => p
            .Add(x => x.Sales, null)
            .Add(x => x.Retry, () => retried = true));

        Assert.Contains("Couldn't load the sales ledger.", cut.Markup);
        cut.Find(".lg-retry").Click();

        Assert.True(retried);
    }
}
