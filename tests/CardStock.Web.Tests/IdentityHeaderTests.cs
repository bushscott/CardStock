using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CardStock.Web.Tests;

public class IdentityHeaderTests : BunitContext
{
    private static readonly PricesDto EmptyPrices = new("2026-08", []);

    public IdentityHeaderTests()
    {
        // Close() returns focus to the thumbnail through JS interop -- nothing under
        // test asserts on the call itself, so Loose keeps unrelated JS calls from
        // throwing (bUnit's default, set explicitly so it can't silently regress).
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static IdentityDto Identity(
        string? collectorNumber = "215", int? setSize = null, DateTimeOffset? delistedAt = null,
        IReadOnlyList<SpeciesRefDto>? species = null) =>
        new("Umbreon VMAX (Alt Art)", collectorNumber, setSize, SetId: 7, "Evolving Skies",
            species ?? [new SpeciesRefDto("Umbreon", "umbreon")], HasImage: true, delistedAt);

    private IRenderedComponent<IdentityHeader> RenderHeader(IdentityDto identity) =>
        Render<IdentityHeader>(p => p
            .Add(x => x.Identity, identity)
            .Add(x => x.CardId, 630417L)
            .Add(x => x.Prices, EmptyPrices)
            .Add(x => x.Signals, new SignalsDto(0, 0, [])));

    // D-122: the set segment is a live link now that /set/{id} exists — the deferred
    // button era ended with the wire gaining SetId.
    [Fact]
    public void Subline_set_segment_links_to_the_set_page()
    {
        var cut = RenderHeader(Identity());

        var setLink = cut.Find("a.subline-set");
        Assert.Equal("Evolving Skies", setLink.TextContent);
        Assert.Equal("set/7", setLink.GetAttribute("href"));
        Assert.Empty(cut.FindAll(".subline button"));
    }

    [Fact]
    public void Subline_number_segment_renders_the_hash_form_and_omits_when_null()
    {
        var withNumber = RenderHeader(Identity(collectorNumber: "215"));
        Assert.Contains("#215", withNumber.Find(".subline").TextContent);

        var withoutNumber = RenderHeader(Identity(collectorNumber: null));
        Assert.DoesNotContain("#", withoutNumber.Find(".subline").TextContent);
    }

    [Fact]
    public void Subline_number_segment_prefers_the_set_size_form_when_present()
    {
        var cut = RenderHeader(Identity(collectorNumber: "215", setSize: 203));

        Assert.Contains("215/203", cut.Find(".subline").TextContent);
    }

    // D-122 closes the D-087 placeholder era: the Pokédex tag table shipped, so the
    // character segment renders real species links — and a card with no tagged species
    // (Trainers/Energy, D-108) omits the segment honestly instead of showing a label.
    [Fact]
    public void Subline_species_render_as_character_links()
    {
        var cut = RenderHeader(Identity());
        var species = cut.Find("a.subline-character");

        Assert.Equal("Umbreon", species.TextContent);
        Assert.Equal("character/umbreon", species.GetAttribute("href"));
    }

    [Fact]
    public void Subline_species_links_render_even_without_a_collector_number()
    {
        var cut = RenderHeader(Identity(collectorNumber: null));

        Assert.Single(cut.FindAll("a.subline-character"));
    }

    [Fact]
    public void A_card_without_species_omits_the_character_segment_and_dot()
    {
        var cut = RenderHeader(Identity(species: []));

        Assert.Empty(cut.FindAll(".subline-character"));
        Assert.Empty(cut.FindAll(".subline-dot"));
    }

    [Fact]
    public void Delisted_chip_renders_only_when_delisted_at_is_set()
    {
        var delisted = RenderHeader(Identity(delistedAt: new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero)));
        var chip = delisted.Find(".chip-delisted");
        Assert.Equal("delisted 07-30-2026", chip.TextContent);
        Assert.Equal(
            "The source no longer lists this card; its history stands.", chip.GetAttribute("title"));

        var active = RenderHeader(Identity(delistedAt: null));
        Assert.Empty(active.FindAll(".chip-delisted"));
    }

    [Fact]
    public void Row_a_actions_render_deferred_disabled_with_task_15s_canonical_copy()
    {
        var cut = RenderHeader(Identity());

        var openInCharts = cut.Find(".btn-open-charts");
        Assert.True(openInCharts.HasAttribute("disabled"));
        Assert.Equal("Charts arrives in a later phase", openInCharts.GetAttribute("title"));

        var watchlist = cut.Find(".btn-watchlist");
        Assert.Equal("Watchlists arrive with accounts, in a later phase", watchlist.GetAttribute("title"));

        var binder = cut.Find(".btn-binder");
        Assert.Equal("The Binder arrives in Phase 4", binder.GetAttribute("title"));
    }

    [Fact]
    public void Imageless_identity_renders_the_placeholder_and_the_lightbox_cannot_open()
    {
        // I3: HasImage was carried on the wire but never read. False means the source never had
        // art for this card -- the placeholder must render immediately (no <img>, no 404
        // round-trip) and the art button must not open a lightbox onto nothing.
        var cut = RenderHeader(Identity() with { HasImage = false });

        var artButton = cut.Find(".art-col");
        Assert.True(artButton.HasAttribute("disabled"));
        Assert.Single(cut.FindAll(".art-placeholder"));
        Assert.Empty(cut.FindAll("#card-art-thumb"));

        artButton.Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public void Escape_closes_the_lightbox_opened_from_the_thumbnail()
    {
        var cut = RenderHeader(Identity());

        cut.Find(".art-col").Click();
        var dialog = cut.Find("[role='dialog']");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));

        // I4: this test previously passed vacuously -- bUnit's KeyDown() dispatches straight to
        // the element's registered handler regardless of real DOM focus, so Escape "worked"
        // here even when nothing moved focus onto the backdrop. A real browser only ever
        // delivers a keydown event to whatever element currently holds focus, so without this,
        // Escape would never reach OnKeyDownAsync outside a test. Confirming the backdrop
        // actually received FocusAsync() is what makes aria-modal (and Escape) real.
        JSInterop.VerifyFocusAsyncInvoke();

        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll("[role='dialog']"));
    }
}
