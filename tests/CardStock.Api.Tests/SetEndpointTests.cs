using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;
using CardStock.Domain.Census;

namespace CardStock.Api.Tests;

public class SetEndpointTests
{
    private static SetPageSnapshot Snapshot() => new(
        7, "Evolving Skies", "matched", "swsh7", "SWSH", 237, new DateOnly(2021, 12, 15),
        [new RosterCard(1, "Umbreon VMAX", true, 45_000, 0.25m,
            new PopulationDelta.Result(PopulationDeltaState.Pending, null,
                new DateOnly(2026, 7, 30), new DateOnly(2026, 9, 28)), 2)]);

    [Fact]
    public async Task A_known_set_serializes_the_dto()
    {
        using var app = new TestApp { SetSnapshot = Snapshot() };
        using var client = app.CreateClient();

        var dto = await client.GetFromJsonAsync<SetPageDto>("/api/v1/sets/7");

        Assert.NotNull(dto);
        Assert.Equal("Evolving Skies", dto!.Name);
        Assert.Equal("2021-12", dto.FirstSaleMonth);
        Assert.Equal("pending", dto.Roster[0].Pop.State);
        Assert.Equal("2026-09-28", dto.Roster[0].Pop.DeltasBeginOn);
    }

    [Fact]
    public async Task An_unknown_set_is_a_404_problem()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/sets/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("unknown", problem!["reason"].ToString());
    }
}
