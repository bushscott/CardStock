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
    public void A_chip_click_filters_the_rows_and_updates_the_count()
    {
        var sales = new[]
        {
            Sale("2026-08-01", "PSA 10", 100),
            Sale("2026-08-02", "PSA 10", 200),
            Sale("2026-08-03", "Grade 9", 300),
        };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));
        Assert.Equal("3 sales shown", cut.Find(".lg-count").TextContent);

        cut.Find("button[data-bucket='PSA 10']").Click();

        Assert.Equal("2 sales shown", cut.Find(".lg-count").TextContent);
    }

    [Fact]
    public void The_all_chip_resets_the_selection_and_closes_both_group_popovers()
    {
        var sales = new[] { Sale("2026-08-01", "PSA 10", 100), Sale("2026-08-02", "Grade 9", 200) };
        var cut = Render<SalesLedger>(p => p.Add(x => x.Sales, sales));

        cut.Find("button[data-bucket='PSA 10']").Click();
        cut.FindAll("button.lg-chip").First(b => b.TextContent.StartsWith("other 10s")).Click();
        Assert.NotEmpty(cut.FindAll(".lg-popover"));
        Assert.Equal("1 sales shown", cut.Find(".lg-count").TextContent);

        cut.FindAll("button.lg-chip").First(b => b.TextContent == "All").Click();

        Assert.Equal("2 sales shown", cut.Find(".lg-count").TextContent);
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
        var listedCell = cut.Find(".lg-listed");
        Assert.Equal("listed $2 → sold $1", listedCell.GetAttribute("title"));
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
