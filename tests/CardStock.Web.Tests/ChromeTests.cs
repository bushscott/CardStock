using Bunit;
using CardStock.Web.Components;
using CardStock.Web.Components.Card;
using CardStock.Web.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CardStock.Web.Tests;

public class ChromeTests : BunitContext
{
    public static IEnumerable<object[]> DeferredTooltips =>
        new List<object[]>
        {
            new object[] { "Home", "Home arrives in a later phase" },
            new object[] { "Screener", "The Screener arrives in a later phase" },
            new object[] { "Charts", "Charts arrives in a later phase" },
            new object[] { "Binder", "The Binder arrives in a later phase (Phase 4)" },
            new object[] { "Search", "Search arrives in a later phase" },
            new object[] { "Avatar", "Accounts arrive with the Binder phase" },
            new object[] { "Open in Charts →", "Charts arrives in a later phase" },
            new object[] { "+ Watchlist ▾", "Watchlists arrive with accounts, in a later phase" },
            new object[] { "+ Binder", "The Binder arrives in Phase 4" },
        };

    [Theory]
    [MemberData(nameof(DeferredTooltips))]
    public void DeferredControl_renders_disabled_with_its_exact_tooltip(string label, string tooltip)
    {
        var cut = Render<DeferredControl>(p => p
            .Add(x => x.Label, label)
            .Add(x => x.Tooltip, tooltip));

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        Assert.Equal(tooltip, button.GetAttribute("title"));
        Assert.Equal(label, button.TextContent);
    }

    [Fact]
    public void AppChrome_renders_four_deferred_tabs_and_the_live_browse_anchor()
    {
        var cut = Render<AppChrome>();

        Assert.Equal(4, cut.FindAll("button.tab").Count);
        Assert.Single(cut.FindAll("a.tab"));
    }

    [Fact]
    public void AppChrome_renders_an_honestly_disabled_avatar_and_search_input()
    {
        var cut = Render<AppChrome>();

        var avatar = cut.Find("button.avatar");
        Assert.True(avatar.HasAttribute("disabled"));
        Assert.Equal("Accounts arrive with the Binder phase", avatar.GetAttribute("title"));

        var search = cut.Find("input.search-input");
        Assert.True(search.HasAttribute("disabled"));
        Assert.Equal("Search arrives in a later phase", search.GetAttribute("title"));
    }

    [Fact]
    public void The_browse_tab_is_a_live_link_active_across_catalog_routes()
    {
        var cut = Render<AppChrome>();
        var browse = cut.FindAll(".tabs a").Single(a => a.TextContent == "Browse");
        Assert.Equal("browse", browse.GetAttribute("href"));

        // No forced cut.Render() here -- WaitForAssertion polls until the assertion holds,
        // so only AppChrome's own LocationChanged subscription (not a test-driven re-render)
        // can make these pass. Delete the subscription and this test times out and fails.
        var nav = Services.GetRequiredService<NavigationManager>();
        foreach (var route in new[] { "browse", "set/7", "character/umbreon" })
        {
            nav.NavigateTo(route);
            cut.WaitForAssertion(() => Assert.Contains("active",
                cut.FindAll(".tabs a").Single(a => a.TextContent == "Browse").ClassList));
        }

        // The negative transition: leaving the catalog un-lights the tab, proving
        // IsCatalogRoute() is re-evaluated on every redraw rather than latched true.
        nav.NavigateTo("card/1");
        cut.WaitForAssertion(() => Assert.DoesNotContain("active",
            cut.FindAll(".tabs a").Single(a => a.TextContent == "Browse").ClassList));
    }

    [Fact]
    public void The_search_tooltip_no_longer_promises_the_browse_phase()
    {
        var cut = Render<AppChrome>();
        Assert.Equal("Search arrives in a later phase",
            cut.Find("input[type=search]").GetAttribute("title"));
    }

    [Fact]
    public void Breadcrumb_renders_browse_set_and_leaf_with_the_reusable_tooltips()
    {
        var cut = Render<Breadcrumb>(p => p
            .Add(x => x.SetName, "Evolving Skies")
            .Add(x => x.CardTitle, "Umbreon VMAX (Alt Art)"));

        // The separator is the mockup's › (U+203A, Card.dc.html:56) — never a slash.
        Assert.All(cut.FindAll(".crumb-sep"), sep => Assert.Equal("›", sep.TextContent));
        Assert.Equal(2, cut.FindAll(".crumb-sep").Count);

        var crumbs = cut.FindAll(".crumb-link");
        Assert.Equal(2, crumbs.Count);
        Assert.Equal("Browse", crumbs[0].TextContent);
        Assert.Equal(Breadcrumb.BrowseTooltip, crumbs[0].GetAttribute("title"));
        Assert.Equal("Evolving Skies", crumbs[1].TextContent);
        Assert.Equal(Breadcrumb.SetTooltip, crumbs[1].GetAttribute("title"));

        var leaf = cut.Find(".crumb-leaf");
        Assert.Equal("Umbreon VMAX (Alt Art)", leaf.TextContent);
        Assert.False(leaf.HasAttribute("title"));
    }

    [Fact]
    public void AppLogo_renders_a_square_mark_at_the_requested_size()
    {
        var cut = Render<AppLogo>(p => p.Add(x => x.Size, 32));

        var svg = cut.Find("svg");
        Assert.Equal("0 0 32 32", svg.GetAttribute("viewBox"));
        Assert.Equal("32", svg.GetAttribute("width"));
        Assert.Equal("32", svg.GetAttribute("height"));
    }

    [Fact]
    public void AppLogo_defaults_to_24px()
    {
        var cut = Render<AppLogo>();

        var svg = cut.Find("svg");
        Assert.Equal("24", svg.GetAttribute("width"));
    }
}
