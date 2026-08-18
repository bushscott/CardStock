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

public class BrowsePagePokemonTests : BunitContext
{
    // BrowseFilterPopover focuses its root on first render (browse.md contradictions §334,
    // closed in the port: role=dialog + focus + Esc, mirroring Lightbox.razor's FocusAsync
    // idiom). Loose is bUnit's default already (per IdentityHeaderTests/PriceChartTests), set
    // explicitly per that same precedent so a future default change can't regress this silently.
    public BrowsePagePokemonTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static SpeciesTileDto Species(int id, string name, long value, string[] types,
        short gen, string? habitat = "Urban") => new(
        id, name, name.ToLowerInvariant(), 10, value, types, gen,
        "Johto", "Ordinary", 1, "Black", ["Field"], habitat);

    private static readonly SpeciesTileDto[] All =
    [
        Species(6, "Charizard", 28_400_000, ["Fire"], 1),
        Species(197, "Umbreon", 9_640_000, ["Dark"], 2),
        Species(471, "Glaceon", 1_190_000, ["Ice"], 4, habitat: null),
    ];

    [Fact]
    public void Pokemon_mode_renders_the_value_ordered_grid_with_icons_and_deferred_deltas()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");

        var names = cut.FindAll(".species-tile .sp-name").Select(n => n.TextContent).ToList();
        Assert.Equal(["Charizard", "Umbreon", "Glaceon"], names);   // wire order preserved
        Assert.Contains("Ordered by total market value across all printings", cut.Markup);

        // Owner ruling 2026-08-18 (UAT): header is a flex row — name + printings left,
        // sprite trailing right at its native 68×56 — and the gradient circle + initial
        // are gone outright (icon coverage is 1,025/1,025; onerror collapse suffices).
        var tile = cut.FindAll(".species-tile")[0];
        var head = tile.QuerySelector(".sp-head")!;
        Assert.Equal(2, head.Children.Length);
        Assert.Contains("sp-id", head.Children[0].ClassName);
        Assert.Equal("IMG", head.Children[1].TagName);
        Assert.Contains("api/v1/species/6/icon", head.QuerySelector("img.sp-sprite")!.GetAttribute("src"));
        Assert.Empty(cut.FindAll(".sp-avatar"));
        Assert.Empty(cut.FindAll(".sp-initial"));
        Assert.Contains("$284K", tile.TextContent);
        Assert.Contains("10 printings", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        Assert.Equal("character/charizard", tile.GetAttribute("href"));
        Assert.Contains("3 of 3 species", cut.Markup);
    }

    // D-113: with the art index present, each sprite draws cropped to its measured art box
    // at the largest clean factor that fits the 68×56 slot; species missing from the index
    // keep the plain 1×-canvas draw.
    [Fact]
    public void Sprites_draw_cropped_at_their_clean_factor_when_the_art_index_is_present()
    {
        _spriteArtJson = """
            {"generatedOn":"test","method":"test",
             "sprites":{"6":[10,15,44,39,68,56],"197":[22,27,26,27,68,56]}}
            """;
        var cut = RenderBrowse(species: All, mode: "pokemon");
        var tiles = cut.FindAll(".species-tile");

        // Charizard 44×39 → 1×: window is the art box, canvas shifted by the art origin.
        var charWin = tiles[0].QuerySelector(".sp-art-window")!;
        Assert.Contains("width:44px;height:39px", charWin.GetAttribute("style"));
        var charImg = charWin.QuerySelector("img")!;
        Assert.Contains("width:68px;height:56px", charImg.GetAttribute("style"));
        Assert.Contains("margin:-15px 0 0 -10px", charImg.GetAttribute("style"));

        // Umbreon 26×27 → 2×: window, canvas, and origin all double.
        var umbWin = tiles[1].QuerySelector(".sp-art-window")!;
        Assert.Contains("width:52px;height:54px", umbWin.GetAttribute("style"));
        var umbImg = umbWin.QuerySelector("img")!;
        Assert.Contains("width:136px;height:112px", umbImg.GetAttribute("style"));
        Assert.Contains("margin:-54px 0 0 -44px", umbImg.GetAttribute("style"));

        // Glaceon has no index entry → the plain fallback, no crop window.
        Assert.NotNull(tiles[2].QuerySelector("img.sp-sprite"));
        Assert.Null(tiles[2].QuerySelector(".sp-art-window"));
    }

    [Fact]
    public void Committing_a_filter_narrows_the_grid_chips_it_and_the_counter_follows()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");

        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Type")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Dark")).Click();
        cut.Find(".pf-add").Click();

        Assert.Single(cut.FindAll(".species-tile"));
        Assert.Contains("type = Dark", cut.Find(".filter-chip").TextContent);
        Assert.Contains("1 of 3 species", cut.Markup);

        cut.Find(".filter-chip .chip-remove").Click();
        Assert.Equal(3, cut.FindAll(".species-tile").Count);
    }

    [Fact]
    public void The_add_button_disables_until_a_value_is_picked()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Type")).Click();
        Assert.True(cut.Find(".pf-add").HasAttribute("disabled"));
        Assert.Contains("pick at least one", cut.Markup);
    }

    [Fact]
    public void Zero_matches_render_the_empty_panel_copy()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Generation")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Gen 4")).Click();
        cut.Find(".pf-add").Click();
        cut.Find(".filter-chip .chip-remove").Click();   // reset

        // Now a filter that excludes everything: type Dark + gen 1.
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Type")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Dark")).Click();
        cut.Find(".pf-add").Click();
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Generation")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Gen 1")).Click();
        cut.Find(".pf-add").Click();

        Assert.Contains("No species match these filters — remove one to widen the net.", cut.Markup);
        Assert.Contains("0 of 3 species", cut.Markup);
    }

    [Fact]
    public void The_habitat_editor_carries_the_gen_explainer()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Habitat")).Click();
        Assert.Contains("Habitat exists for Gen 1–3 species only", cut.Markup);
    }

    // D-110 spec §5 (focus trap fix): bUnit's JSInterop runs loose, and there is no real
    // browser tab order to assert Tab-wrapping against here -- the real behavior lands in
    // Task 24's browser pass. What this test guards cheaply: the structural contract
    // installFocusTrap's JS depends on (role="dialog" + tabindex="-1" on the exact element
    // passed as _root), and that OnAfterRenderAsync's module-import/installFocusTrap call
    // doesn't throw when the interop is stubbed (loose mode returns null for the unconfigured
    // "import" call, so the guarded `if (_module is not null)` skips the install -- same shape
    // RosterTable's installGripCapture/installHeaderKeyGuard already rely on).
    [Fact]
    public void The_popover_root_is_a_focus_trappable_dialog()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();

        var pop = cut.Find(".pf-pop");
        Assert.Equal("dialog", pop.GetAttribute("role"));
        Assert.Equal("-1", pop.GetAttribute("tabindex"));
    }

    // D-114: while the fetch is in flight, the page shows the shared LoadingRing — the same
    // ring index.html's boot indicator draws, so the handoff reads as one animation. The
    // gated handler holds the species response open long enough to observe the state.
    [Fact]
    public void The_fetch_in_flight_renders_the_shared_loading_ring()
    {
        var gate = new TaskCompletionSource<HttpResponseMessage>();
        RegisterClient(new HttpClient(new GatedHandler(gate)) { BaseAddress = new Uri("http://localhost/") });
        Services.GetRequiredService<NavigationManager>().NavigateTo("browse?mode=pokemon");

        var cut = Render<BrowsePage>();
        Assert.NotNull(cut.Find(".loading-ring"));
        Assert.Empty(cut.FindAll(".loading-strip"));

        gate.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = JsonContent.Create(new BrowseSpeciesDto([])) });
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".species-grid")));
    }

    private sealed class GatedHandler(TaskCompletionSource<HttpResponseMessage> gate) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            request.RequestUri!.AbsolutePath.EndsWith("sprite-art.json")
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
                : gate.Task;
    }

    // Mirrors BrowsePageSetsTests' RenderBrowse (one CatalogApiClient stub per test-class
    // instance, mutable fixture read through closure via RegisterClient/RespondingWith) but
    // cannot reuse that private helper -- it lives on a different test class -- and cannot
    // reuse its wait condition either: it polls for ".set-grid", which pokémon mode never
    // renders (BrowsePage.razor gates the whole set wall behind "@if (!IsPokemon)"). This
    // file only ever exercises pokémon mode, so the helper here is narrowed to species+mode
    // and waits on ".species-grid" instead, which renders once loaded regardless of match
    // count (the empty-panel test needs it present even at 0 matches).
    private IReadOnlyList<SpeciesTileDto> _species = [];
    private string? _spriteArtJson;   // null → the static asset 404s → the 1×-canvas fallback
    private bool _registered;

    private IRenderedComponent<BrowsePage> RenderBrowse(
        IReadOnlyList<SpeciesTileDto> species, string mode)
    {
        _species = species;
        if (!_registered)
        {
            _registered = true;
            RegisterClient(RespondingWith(req =>
                req.RequestUri!.AbsolutePath.EndsWith("sprite-art.json")
                    ? _spriteArtJson is null
                        ? new HttpResponseMessage(HttpStatusCode.NotFound)
                        : new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(_spriteArtJson, System.Text.Encoding.UTF8, "application/json") }
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = JsonContent.Create(new BrowseSpeciesDto(_species)) }));
        }

        Services.GetRequiredService<NavigationManager>().NavigateTo($"browse?mode={mode}");

        var cut = Render<BrowsePage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".species-grid")));
        return cut;
    }

    private static HttpClient RespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri("http://localhost/") };

    private void RegisterClient(HttpClient http) =>
        Services.AddScoped(_ => new CatalogApiClient(http));
}
