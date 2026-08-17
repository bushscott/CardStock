using System.Net;
using System.Net.Http.Json;
using Bunit;
using CardStock.Application.Catalog;
using CardStock.Web.Pages;
using CardStock.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace CardStock.Web.Tests;

public class SetPageTests : BunitContext
{
    public SetPageTests()
    {
        // SetPage defaults to terminal density, which mounts RosterTable -- its
        // OnAfterRenderAsync imports ./js/catalog.js and installs grip/key-guard capture on
        // every mount (mirrors RosterTableTests/CardPageTests: loose mode auto-satisfies those
        // calls so this file's assertions, which don't care about drag/keyboard JS, don't need
        // to configure every call by hand).
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static SetPageDto Dto(params SetRosterRowDto[] roster) => new(
        7, "Evolving Skies", "matched", "swsh7", "SWSH", 3, "2021-12", roster);

    private static SetRosterRowDto Row(
        long id = 1, string name = "Umbreon VMAX", int? price = 45_000, decimal? roc = 0.25m,
        string popState = "available", decimal? popFraction = 0.10m,
        string? firstObserved = "2026-06-01", string? deltasBegin = null, int sales = 2) =>
        new(id, name, true, price, roc,
            new PopDto(popState, popFraction, firstObserved, deltasBegin), sales);

    [Fact]
    public void The_header_prints_code_uppercase_era_chip_and_first_sale()
    {
        var cut = RenderSetPage(Dto(Row()));
        Assert.Equal("SWSH7", cut.Find(".set-code").TextContent);
        Assert.Equal("SWSH", cut.Find(".set-era").TextContent);
        Assert.Contains("3 cards tracked", cut.Markup);
        Assert.Contains("first sale observed Dec 2021", cut.Markup);
    }

    [Fact]
    public void A_pending_set_renders_one_metadata_chip_with_the_glyph()
    {
        var dto = Dto(Row()) with { MetadataStatus = "pending", Code = null, Era = null };
        var cut = RenderSetPage(dto);
        Assert.Empty(cut.FindAll(".set-code"));
        Assert.Empty(cut.FindAll(".set-era"));
        var chip = cut.Find(".set-meta-pending");
        Assert.Contains("◌", chip.TextContent);
        Assert.Contains("metadata pending", chip.TextContent);
    }

    [Fact]
    public void The_index_block_is_mocked_and_rs_renders_dashes_with_a_header_glyph()
    {
        var cut = RenderSetPage(Dto(Row()));
        Assert.Contains("set index · 12M", cut.Markup);
        var rsHeader = cut.FindAll(".rt-head-cell").Single(h => h.TextContent.Contains("RS pct"));
        Assert.Single(rsHeader.QuerySelectorAll("span.gate-glyph"));
    }

    [Fact]
    public void Pop_states_render_dash_cells_with_computed_tooltips()
    {
        var pending = Row(id: 2, name: "Glaceon V", popState: "pending", popFraction: null,
            firstObserved: "2026-07-30", deltasBegin: "2026-09-28");
        var none = Row(id: 3, name: "Leafeon V", price: null, roc: null,
            popState: "none", popFraction: null, firstObserved: null, sales: 0);
        var cut = RenderSetPage(Dto(Row(), pending, none));

        var pendingCell = cut.FindAll(".rt-cell-pop")[1];
        Assert.Equal("–", pendingCell.TextContent.Trim());
        Assert.Contains("first observation", pendingCell.QuerySelector("[title]")!.GetAttribute("title"));
        var noneCell = cut.FindAll(".rt-cell-pop")[2];
        Assert.Contains("No PSA 10 population observed",
            noneCell.QuerySelector("[title]")!.GetAttribute("title"));
    }

    [Fact]
    public void The_pop_sort_excludes_pending_rows_and_raises_the_banner()
    {
        var pending = Row(id: 2, name: "Glaceon V", popState: "pending", popFraction: null,
            firstObserved: "2026-07-30", deltasBegin: "2026-09-28");
        var cut = RenderSetPage(Dto(Row(), pending));

        cut.FindAll(".sort-pills .pill").Single(p => p.TextContent == "pop Δ").Click();

        Assert.Single(cut.FindAll(".rt-row"));
        var banner = cut.Find(".exclusion-banner");
        Assert.Contains("1 cards excluded", banner.TextContent);
        Assert.Contains("deltas begin arriving", banner.TextContent);
        Assert.Contains("1 of 3 cards", cut.Markup);
    }

    [Fact]
    public void A_negative_pop_delta_renders_a_true_minus_never_a_plus()
    {
        var falling = Row(popState: "available", popFraction: -0.25m);
        var cut = RenderSetPage(Dto(falling));
        Assert.Contains("−25.0%", cut.Find(".rt-cell-pop").TextContent);
    }

    [Fact]
    public void The_footer_owns_the_full_roster_and_the_empty_state_guards()
    {
        var cut = RenderSetPage(Dto(Row()));
        Assert.Contains("Showing all 3 tracked cards", cut.Markup);

        var empty = RenderSetPage(Dto() with { CardsTracked = 0 });
        Assert.Contains("No tracked cards in this set", empty.Markup);
    }

    // Follows CardPageTests' established registration idiom for CardApiClient/HttpClient
    // stubs -- this file registers CatalogApiClient over a stub handler the same way, then
    // renders SetPage with Id=7 and waits for the header (present on every successful load,
    // matched/pending metadata and empty/populated roster alike) as the "loaded" signal.
    //
    // Registration happens once per test (guarded by _registered): bUnit 2.9's
    // BunitServiceProvider throws if a service is (re-)registered after the first render has
    // already resolved one (CheckInitializedAndThrow). The footer/empty-state test renders
    // twice with different fixtures, so the stub reads the current dto through a mutable
    // field captured by closure instead of re-registering the client per call.
    private SetPageDto? _dto;
    private bool _registered;

    private IRenderedComponent<SetPage> RenderSetPage(SetPageDto dto)
    {
        _dto = dto;
        if (!_registered)
        {
            _registered = true;
            RegisterClient(RespondingWith(_ =>
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(_dto) }));
        }

        var cut = Render<SetPage>(p => p.Add(x => x.Id, 7));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".set-header")));
        return cut;
    }

    private static HttpClient RespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri("http://localhost/") };

    private void RegisterClient(HttpClient http) =>
        Services.AddScoped(_ => new CatalogApiClient(http));
}
