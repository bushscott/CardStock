using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Api.Tests;

public class BrowseEndpointTests
{
    [Fact]
    public async Task Sets_and_species_serialize_their_tiles()
    {
        using var app = new TestApp
        {
            BrowseSets = [new SetTile(7, "Evolving Skies", 237, 1, "matched", "SWSH",
                new DateOnly(2021, 8, 27))],
            BrowseSpecies = [new SpeciesTile(197, "Umbreon", "umbreon", "#2B2D42", "#5C6B9E",
                34, 9_640_000, ["Dark"], 2, "Johto", "Ordinary", 1, "Black", ["Field"], "Urban")],
        };
        using var client = app.CreateClient();

        var sets = await client.GetFromJsonAsync<BrowseSetsDto>("/api/v1/browse/sets");
        Assert.Equal("Evolving Skies", sets!.Sets[0].Name);
        Assert.Equal(new DateOnly(2021, 8, 27), sets.Sets[0].ReleasedOn);

        var species = await client.GetFromJsonAsync<BrowseSpeciesDto>("/api/v1/browse/species");
        Assert.Equal("Umbreon", species!.Species[0].Name);
        Assert.Equal("Urban", species.Species[0].Habitat);
    }
}
