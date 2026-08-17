using CardStock.Application.Catalog;
using Xunit;

namespace CardStock.Application.Tests;

public class BrowseMapperTests
{
    [Fact]
    public void Set_tile_fields_survive_the_map()
    {
        var tile = new SetTile(
            SetId: 7, Name: "Evolving Skies", Cards: 237, TopCardId: 1,
            MetadataStatus: "matched", Era: "SWSH", ReleasedOn: new DateOnly(2021, 8, 27));

        var dto = CatalogMappers.ToDto([tile]).Sets[0];

        Assert.Equal(7, dto.SetId);
        Assert.Equal("Evolving Skies", dto.Name);
        Assert.Equal(237, dto.Cards);
        Assert.Equal(1, dto.TopCardId);
        Assert.Equal("matched", dto.MetadataStatus);
        Assert.Equal("SWSH", dto.Era);
        Assert.Equal(new DateOnly(2021, 8, 27), dto.ReleasedOn);
    }

    [Fact]
    public void Species_tile_fields_survive_the_map()
    {
        var tile = new SpeciesTile(
            SpeciesId: 197, Name: "Umbreon", Slug: "umbreon",
            GradientStart: "#2B2D42", GradientEnd: "#5C6B9E",
            Printings: 34, TotalValueCents: 9_640_000, Types: ["Dark"],
            Generation: 2, Region: "Johto", Status: "Ordinary", Stage: 1,
            Color: "Black", EggGroups: ["Field"], Habitat: "Urban");

        var dto = CatalogMappers.ToDto([tile]).Species[0];

        Assert.Equal(197, dto.SpeciesId);
        Assert.Equal("Umbreon", dto.Name);
        Assert.Equal("umbreon", dto.Slug);
        Assert.Equal("#2B2D42", dto.GradientStart);
        Assert.Equal("#5C6B9E", dto.GradientEnd);
        Assert.Equal(34, dto.Printings);
        Assert.Equal(9_640_000, dto.TotalValueCents);
        Assert.Equal(["Dark"], dto.Types);
        Assert.Equal((short)2, dto.Generation);
        Assert.Equal("Johto", dto.Region);
        Assert.Equal("Ordinary", dto.Status);
        Assert.Equal((short)1, dto.Stage);
        Assert.Equal("Black", dto.Color);
        Assert.Equal(["Field"], dto.EggGroups);
        Assert.Equal("Urban", dto.Habitat);
    }
}
