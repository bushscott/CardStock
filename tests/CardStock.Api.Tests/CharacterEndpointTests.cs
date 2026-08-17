using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Api.Tests;

public class CharacterEndpointTests
{
    private static CharacterPageSnapshot Umbreon() => new(
        197, "Umbreon", "umbreon", "#2B2D42", "#5C6B9E", 2, "Johto", "Black", "Urban",
        0, 1, "Eevee", ["Dark"], ["Field"], 6, 9_640_000, 7, []);

    [Fact]
    public async Task A_known_slug_serializes_the_dto_with_chips()
    {
        using var app = new TestApp { CharacterSnapshot = Umbreon() };
        using var client = app.CreateClient();

        var dto = await client.GetFromJsonAsync<CharacterPageDto>("/api/v1/characters/umbreon");

        Assert.Equal("Umbreon", dto!.Name);
        Assert.Equal("Gen 2", dto.Chips[1].Label);
        Assert.Equal(0, dto.Printings);
    }

    [Fact]
    public async Task An_unknown_slug_is_a_404_problem()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var response = await client.GetAsync("/api/v1/characters/missingno");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
