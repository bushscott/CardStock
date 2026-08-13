using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CardStock.Application.Cards;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;

namespace CardStock.Api.Tests;

public class SnapshotEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static CardIdentity Identity(long cardId = 42, DateTimeOffset? notACardAt = null) => new(
        CardId: cardId,
        Title: "Charizard #4/102",
        CollectorNumber: "4",
        SetSize: null,
        SetName: "Base Set",
        ImageHash: null,
        DelistedAt: null,
        NotACardAt: notACardAt);

    private static CardPriceSnapshot Prices(long cardId = 42) =>
        CardPriceSnapshotBuilder.Build(cardId, lastVisitedAt: null, prices: [], sales: [], today: new DateOnly(2026, 8, 13));

    [Fact]
    public async Task A_known_card_returns_the_composed_snapshot()
    {
        using var app = new TestApp
        {
            Identity = Identity(),
            Prices = Prices(),
            Census = CardCensus.From([], []),
        };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CardPageSnapshotDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("Charizard #4/102", dto!.Identity.Title);
        Assert.Equal(6, dto.Prices.Tiers.Count);
        Assert.Equal(6, dto.Census.Bars.Count);
        Assert.NotNull(dto.Signals);
    }

    [Fact]
    public async Task An_unknown_id_is_a_404_problem_with_reason_unknown()
    {
        using var app = new TestApp { Identity = null };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("No such card", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("unknown", body.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task A_not_a_card_id_is_a_404_problem_with_reason_not_a_card()
    {
        using var app = new TestApp
        {
            Identity = Identity(notACardAt: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)),
        };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("No such card", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("not_a_card", body.RootElement.GetProperty("reason").GetString());
    }
}
