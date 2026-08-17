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

public class CharacterPageTests : BunitContext
{
    public CharacterPageTests()
    {
        // CharacterPage defaults to binder density, but several tests switch to terminal,
        // which mounts RosterTable -- its OnAfterRenderAsync imports ./js/catalog.js and
        // installs grip/key-guard capture on every mount (mirrors SetPageTests: loose mode
        // auto-satisfies those calls so this file's assertions, which don't care about
        // drag/keyboard JS, don't need to configure every call by hand).
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static CharacterPageDto Dto(params CharacterRosterRowDto[] roster) => new(
        197, "Umbreon", "#2B2D42", "#5C6B9E",
        [new ChipDto("Dark", "Pokédex type"), new ChipDto("Gen 2", "First appeared in Generation 2 (Johto)")],
        roster.Length, 6, 9_640_000, 7, roster);

    private static CharacterRosterRowDto Row(
        long id = 1, string name = "Umbreon VMAX", short? year = 2021, int? price = 45_000) => new(
        id, name, true, 7, "Evolving Skies", year, price, 0.25m, 2);

    [Fact]
    public void The_header_carries_the_icon_over_the_gradient_with_initial_fallback()
    {
        var cut = RenderCharacterPage(Dto(Row()));
        var avatar = cut.Find(".char-avatar");
        Assert.Contains("linear-gradient(160deg, #2B2D42, #5C6B9E)", avatar.GetAttribute("style"));
        var img = avatar.QuerySelector("img")!;
        Assert.Contains("api/v1/species/197/icon", img.GetAttribute("src"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Contains("U", avatar.QuerySelector(".char-initial")!.TextContent);
    }

    [Fact]
    public void The_90d_tile_is_deferred_with_dash_and_glyph_and_totals_abbreviate()
    {
        var cut = RenderCharacterPage(Dto(Row()));
        Assert.Contains("$96.4K", cut.Markup);
        var tile = cut.FindAll(".stat-tile").Single(t => t.TextContent.Contains("90D"));
        Assert.Contains("–", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        var total = cut.FindAll(".stat-tile").Single(t => t.TextContent.Contains("TOTAL VALUE"));
        Assert.Contains("over 7 of 1 printings with a PSA 10 price",
            total.GetAttribute("title") ?? total.QuerySelector("[title]")!.GetAttribute("title"));
    }

    [Fact]
    public void Binder_is_the_default_with_four_pills_reachable()
    {
        var cut = RenderCharacterPage(Dto(Row()));
        Assert.NotEmpty(cut.FindAll(".binder-grid"));
        Assert.Empty(cut.FindAll(".roster-table"));
        var labels = cut.FindAll(".sort-pills .pill").Select(p => p.TextContent).ToList();
        Assert.Equal(["value", "year", "ROC 3M", "sales/mo"], labels);
    }

    [Fact]
    public void The_set_cell_links_and_a_pending_year_renders_the_dash_with_its_tooltip()
    {
        var cut = RenderCharacterPage(Dto(Row(), Row(id: 2, year: null)));
        cut.FindAll(".density-toggle button")[1].Click();   // → terminal

        var setLinks = cut.FindAll(".rt-cell-set a");
        Assert.All(setLinks, a => Assert.Equal("set/7", a.GetAttribute("href")));

        var yearCells = cut.FindAll(".rt-cell-year");
        Assert.Contains("2021", yearCells[0].TextContent);
        Assert.Equal("–", yearCells[1].TextContent.Trim());
        Assert.Equal("Release date pending curation",
            yearCells[1].QuerySelector("[title]")!.GetAttribute("title"));
    }

    [Fact]
    public void The_binder_tile_drops_a_pending_year_without_a_dangling_separator()
    {
        var cut = RenderCharacterPage(Dto(Row(id: 2, year: null)));
        var line = cut.Find(".tile-setline");
        Assert.Equal("Evolving Skies", line.TextContent.Trim());
        Assert.DoesNotContain("·", line.TextContent);
    }

    [Fact]
    public void The_footer_states_the_named_species_rule()
    {
        Assert.Contains("a card naming multiple Pokémon in its title appears under every species it names",
            RenderCharacterPage(Dto(Row())).Markup);
    }

    // Controller ruling (standing R15, mirrored from SetPageTests): ascending sort must
    // never float a row with no value for the active nullable key to the top merely because
    // null loses every comparison ascending -- it must append last regardless of direction.
    // value/year/roc are nullable here; sales is not. Exercised in terminal density (where
    // both the name and set columns render .row-link, so the assertion scopes to the name
    // cell specifically rather than relying on document order across two link classes).
    [Fact]
    public void Ascending_value_sort_still_pushes_the_null_priced_row_last()
    {
        var high = Row(id: 1, name: "High", price: 100);
        var unpriced = Row(id: 2, name: "Unpriced", price: null);
        var low = Row(id: 3, name: "Low", price: 50);
        var cut = RenderCharacterPage(Dto(high, unpriced, low));
        cut.FindAll(".density-toggle button")[1].Click();   // → terminal

        // "value" is already the default active key at descending (SortState's ctor
        // default); one click on it flips to ascending -- a second click would flip
        // straight back to descending, so this deliberately clicks only once.
        cut.FindAll(".sort-pills .pill").Single(p => p.TextContent == "value").Click();

        var names = cut.FindAll(".rt-cell-name").Select(c => c.TextContent).ToList();
        Assert.Equal("Unpriced", names[^1]);
    }

    // Follows SetPageTests' established registration idiom for CatalogApiClient/HttpClient
    // stubs -- this file registers CatalogApiClient over a stub handler the same way, then
    // renders CharacterPage with Slug="umbreon" and waits for the header (present on every
    // successful load, binder/terminal and empty/populated roster alike) as the "loaded"
    // signal.
    //
    // Registration happens once per test (guarded by _registered): bUnit 2.9's
    // BunitServiceProvider throws if a service is (re-)registered after the first render has
    // already resolved one (CheckInitializedAndThrow). No test in this file re-renders with a
    // second fixture, but the stub still reads the current dto through a mutable field
    // captured by closure, matching RenderSetPage's shape exactly.
    private CharacterPageDto? _dto;
    private bool _registered;

    private IRenderedComponent<CharacterPage> RenderCharacterPage(CharacterPageDto dto)
    {
        _dto = dto;
        if (!_registered)
        {
            _registered = true;
            RegisterClient(RespondingWith(_ =>
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(_dto) }));
        }

        var cut = Render<CharacterPage>(p => p.Add(x => x.Slug, "umbreon"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".char-header")));
        return cut;
    }

    private static HttpClient RespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri("http://localhost/") };

    private void RegisterClient(HttpClient http) =>
        Services.AddScoped(_ => new CatalogApiClient(http));
}
