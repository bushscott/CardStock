using Bunit;
using CardStock.Web.Components.Catalog;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CardStock.Web.Tests;

public class BinderGridTests : BunitContext
{
    private sealed record Tile(long Id, string Name, bool HasImage);

    private static RenderFragment<Tile> Body() =>
        tile => builder => builder.AddContent(0, tile.Name);

    private IRenderedComponent<BinderGrid<Tile>> Render(params Tile[] tiles) =>
        Render<BinderGrid<Tile>>(p => p
            .Add(x => x.Rows, tiles)
            .Add(x => x.Href, t => $"card/{t.Id}")
            .Add(x => x.ArtUrl, t => t.HasImage ? $"api/v1/cards/{t.Id}/image" : null)
            .Add(x => x.GradientStart, _ => "#2B2D42")
            .Add(x => x.GradientEnd, _ => "#5C6B9E")
            .Add(x => x.TileBody, Body()));

    [Fact]
    public void The_whole_tile_is_the_link_and_art_lazy_loads()
    {
        var cut = Render(new Tile(630001, "Umbreon VMAX", true));
        var link = cut.Find("a.bg-tile");
        Assert.Equal("card/630001", link.GetAttribute("href"));
        var img = cut.Find(".bg-art img");
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Contains("api/v1/cards/630001/image", img.GetAttribute("src"));
    }

    [Fact]
    public void A_card_without_art_renders_the_gradient_alone()
    {
        var cut = Render(new Tile(2, "Glaceon V", false));
        Assert.Empty(cut.FindAll(".bg-art img"));
        Assert.Contains("linear-gradient(160deg, #2B2D42, #5C6B9E)",
            cut.Find(".bg-art").GetAttribute("style"));
    }
}
