using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CardStock.Application.Cards;

namespace CardStock.Api.Tests;

public class SalesEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static CardIdentity Identity(
        long cardId = 42, DateTimeOffset? notACardAt = null, DateTimeOffset? delistedAt = null) => new(
        CardId: cardId,
        Title: "Charizard #4/102",
        CollectorNumber: "4",
        SetSize: null,
        SetId: 7,
        SetName: "Base Set",
        Species: [],
        ImageHash: null,
        DelistedAt: delistedAt,
        NotACardAt: notACardAt);

    [Fact]
    public async Task A_known_card_returns_its_sales_newest_first_straight_from_the_reader()
    {
        using var app = new TestApp
        {
            Identity = Identity(),
            Sales =
            [
                new LedgerSale(new DateOnly(2026, 8, 1), "PSA 10", 12000, 12500, "eBay", "Charizard PSA 10"),
                new LedgerSale(new DateOnly(2026, 7, 1), "Ungraded", 4000, null, "eBay", "Charizard raw"),
            ],
        };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<SaleDto>>(JsonOptions);
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos!.Count);
        Assert.Equal(new DateOnly(2026, 8, 1), dtos[0].SoldOn);
        Assert.Equal("Charizard PSA 10", dtos[0].Title);
        Assert.Equal(new DateOnly(2026, 7, 1), dtos[1].SoldOn);
        Assert.Equal("Charizard raw", dtos[1].Title);
    }

    [Fact]
    public async Task An_unknown_id_is_a_404_problem_with_reason_unknown()
    {
        using var app = new TestApp { Identity = null };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/999/sales");

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

        var response = await client.GetAsync("/api/v1/cards/42/sales");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("No such card", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("not_a_card", body.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task A_delisted_card_still_serves_its_sales()
    {
        using var app = new TestApp
        {
            Identity = Identity(delistedAt: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)),
            Sales = [new LedgerSale(new DateOnly(2026, 8, 1), "PSA 10", 12000, null, "eBay", "Charizard PSA 10")],
        };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<SaleDto>>(JsonOptions);
        Assert.Single(dtos!);
    }
}
