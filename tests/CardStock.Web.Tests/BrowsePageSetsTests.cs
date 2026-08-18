using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Bunit;
using CardStock.Application.Catalog;
using CardStock.Web.Pages;
using CardStock.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CardStock.Web.Tests;

public class BrowsePageSetsTests : BunitContext
{
    private static SetTileDto Tile(long id, string name, int cards = 100, long? top = null,
        string status = "matched", string? era = "SWSH", string? released = "2021-08-27") => new(
        id, name, cards, top, status, era,
        released is null ? null : DateOnly.Parse(released, CultureInfo.InvariantCulture));

    [Fact]
    public void Sets_mode_is_the_default_with_the_alphabetical_wall()
    {
        var cut = RenderBrowse(sets: [Tile(2, "Evolving Skies"), Tile(1, "Base Set", era: "WOTC")]);
        var names = cut.FindAll(".fan-tile .fan-name").Select(n => n.TextContent).ToList();
        Assert.Equal(["Base Set", "Evolving Skies"], names);
    }

    [Fact]
    public void A_tile_carries_count_deferred_delta_and_the_top_cards_art()
    {
        var cut = RenderBrowse(sets: [Tile(2, "Evolving Skies", cards: 237, top: 630001)]);
        var tile = cut.Find(".fan-tile");
        Assert.Contains("237 cards", tile.TextContent);
        Assert.Contains("30d", tile.TextContent);
        Assert.Contains("–", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        Assert.Contains("api/v1/cards/630001/image",
            tile.QuerySelector(".fan-front img")!.GetAttribute("src"));

        // Controller ruling R8: the planned markup makes the whole .fan-tile the <a> itself
        // (class="browse-set-link fan-tile" on the anchor), so it has no descendant anchors --
        // tile.QuerySelectorAll("a").First() would throw on an empty sequence. Assert on the
        // tile element directly instead.
        Assert.Contains("browse-set-link", tile.ClassList);
        Assert.Equal("set/2", tile.GetAttribute("href"));
    }

    [Fact]
    public void The_era_order_renders_shelf_headings_with_the_tails()
    {
        var cut = RenderBrowse(sets:
        [
            Tile(1, "Base Set", era: "WOTC", released: "1999-01-09"),
            Tile(4, "POP Series 5", era: null, released: "2006-03-01"),
            Tile(5, "Japanese Promo", status: "pending", era: null, released: null),
        ]);

        cut.FindAll(".order-pills .pill").Single(p => p.TextContent == "era").Click();

        var headings = cut.FindAll(".shelf-title").Select(h => h.TextContent).ToList();
        Assert.Equal(["WOTC", "no era", "metadata pending"], headings);
    }

    [Fact]
    public void A_pending_tile_shows_the_metadata_chip_in_era_view()
    {
        var cut = RenderBrowse(sets: [Tile(5, "Japanese Promo", status: "pending", era: null, released: null)]);
        cut.FindAll(".order-pills .pill").Single(p => p.TextContent == "era").Click();
        Assert.Contains("◌ metadata pending", cut.Find(".shelf-title + .set-grid .fan-tile").TextContent);
    }

    // Controller-ruled regression test: CatalogApiClient.GetAsync<T> returns a non-null
    // CatalogResult even on failure (Failed: true), so a naive `_sets ??= await ...` in
    // LoadAsync would never re-fetch once a failed result was stored -- Retry would no-op
    // forever. Bypasses RenderBrowse (whose closure always succeeds) and registers its own
    // stub directly through the same RegisterClient/RespondingWith building blocks, mirroring
    // the mutable-field-captured-by-closure shape RenderBrowse and the landed SetPageTests/
    // CharacterPageTests both use -- here the mutated variable is a bool the closure reads to
    // decide whether to throw (first load) or succeed (after Retry), rather than a dto.
    [Fact]
    public void Retry_refetches_after_a_failure_instead_of_replaying_it()
    {
        var fail = true;
        var sets = new List<SetTileDto> { Tile(1, "Base Set") };
        RegisterClient(RespondingWith(_ => fail
            ? throw new HttpRequestException("down")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new BrowseSetsDto(sets)) }));

        var cut = Render<BrowsePage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-error")));
        Assert.Empty(cut.FindAll(".set-grid"));

        fail = false;
        cut.Find(".card-error button").Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".set-grid")));
        Assert.Equal("Base Set", cut.Find(".fan-name").TextContent);
    }

    // Registration idiom mirrors SetPageTests/CharacterPageTests: one CatalogApiClient stub
    // per test-class instance, registered once (bUnit 2.9's BunitServiceProvider throws if a
    // service is (re-)registered after the first render already resolved one), reading the
    // current fixtures through mutable fields captured by closure. Unlike Set/CharacterPage,
    // BrowsePage.LoadAsync() calls exactly one of the two browse endpoints depending on mode,
    // so the stub branches on the request path rather than always returning one fixed body.
    //
    // Controller ruling (bunit 2.9 idiom): [SupplyParameterFromQuery] resolves from whatever
    // URI the registered NavigationManager holds at render time. BunitContext registers a
    // NavigationManager (Bunit.TestDoubles.BunitNavigationManager) by default and wires
    // Microsoft's SupplyParameterFromQueryValueProvider into its default services -- so
    // navigating that NavigationManager to a URI carrying "?mode=..." *before* Render() is
    // enough; no extra DI registration is needed. Confirmed empirically against this exact
    // bunit 2.9.0 / net10.0 pairing before wiring this helper into the suite.
    private IReadOnlyList<SetTileDto> _sets = [];
    private IReadOnlyList<SpeciesTileDto> _species = [];
    private bool _registered;

    private IRenderedComponent<BrowsePage> RenderBrowse(
        IReadOnlyList<SetTileDto>? sets = null, IReadOnlyList<SpeciesTileDto>? species = null,
        string? mode = null)
    {
        _sets = sets ?? [];
        _species = species ?? [];
        if (!_registered)
        {
            _registered = true;
            RegisterClient(RespondingWith(req =>
            {
                HttpContent content = req.RequestUri!.AbsolutePath.EndsWith("species", StringComparison.Ordinal)
                    ? JsonContent.Create(new BrowseSpeciesDto(_species))
                    : JsonContent.Create(new BrowseSetsDto(_sets));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }));
        }

        if (mode is not null)
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo($"browse?mode={mode}");
        }

        var cut = Render<BrowsePage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".set-grid")));
        return cut;
    }

    private static HttpClient RespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri("http://localhost/") };

    private void RegisterClient(HttpClient http) =>
        Services.AddScoped(_ => new CatalogApiClient(http));
}
