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
        id, name, name.ToLowerInvariant(), "#2B2D42", "#5C6B9E", 10, value, types, gen,
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

        var tile = cut.FindAll(".species-tile")[0];
        Assert.Contains("api/v1/species/6/icon", tile.QuerySelector(".sp-avatar img")!.GetAttribute("src"));
        Assert.Contains("$284K", tile.TextContent);
        Assert.Contains("10 printings", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        Assert.Equal("character/charizard", tile.GetAttribute("href"));
        Assert.Contains("3 of 3 species", cut.Markup);
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

    // Mirrors BrowsePageSetsTests' RenderBrowse (one CatalogApiClient stub per test-class
    // instance, mutable fixture read through closure via RegisterClient/RespondingWith) but
    // cannot reuse that private helper -- it lives on a different test class -- and cannot
    // reuse its wait condition either: it polls for ".set-grid", which pokémon mode never
    // renders (BrowsePage.razor gates the whole set wall behind "@if (!IsPokemon)"). This
    // file only ever exercises pokémon mode, so the helper here is narrowed to species+mode
    // and waits on ".species-grid" instead, which renders once loaded regardless of match
    // count (the empty-panel test needs it present even at 0 matches).
    private IReadOnlyList<SpeciesTileDto> _species = [];
    private bool _registered;

    private IRenderedComponent<BrowsePage> RenderBrowse(
        IReadOnlyList<SpeciesTileDto> species, string mode)
    {
        _species = species;
        if (!_registered)
        {
            _registered = true;
            RegisterClient(RespondingWith(_ => new HttpResponseMessage(HttpStatusCode.OK)
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
