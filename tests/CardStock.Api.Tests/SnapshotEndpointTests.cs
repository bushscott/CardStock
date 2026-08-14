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
            Census = CardCensus.From([]),
        };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CardPageSnapshotDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("Charizard #4/102", dto!.Identity.Title);
        Assert.Equal(6, dto.Prices.Tiers.Count);
        Assert.Equal(6, dto.Census.Bars.Count);

        // An empty card still evaluates everything: 8 price rows below their
        // floors, the volume row at zero, and the three locked rows. The counts
        // are computed from the rows, and every state string is lowercase.
        Assert.Equal(12, dto.Signals.Evaluated);
        Assert.Equal(0, dto.Signals.Firing);
        Assert.Equal(12, dto.Signals.Rows.Count);
        Assert.Equal("0 / 30d", Assert.Single(dto.Signals.Rows, r => r.Name == "Sales volume").Value);
        var rs = Assert.Single(dto.Signals.Rows, r => r.Name == "RS vs index 3M");
        Assert.Equal("locked", rs.State);
        Assert.Equal(
            "Relative strength needs the market index — it arrives with the worker phase",
            rs.Tooltip);
        Assert.All(dto.Signals.Rows, r => Assert.Equal(r.State, r.State.ToLowerInvariant()));
    }

    [Fact]
    public async Task Snapshot_sales_volume_counts_from_the_endpoint_clock()
    {
        // Clock fixed at 2026-08-13: a sale 29 days back counts, the sale on the
        // 30-day boundary does not.
        using var app = new TestApp
        {
            Identity = Identity(),
            Prices = Prices(),
            Census = CardCensus.From([]),
            UtcNow = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero),
            Sales =
            [
                new LedgerSale(new DateOnly(2026, 7, 15), "PSA 10", 100_00, null, "ebay", "counts"),
                new LedgerSale(new DateOnly(2026, 7, 14), "PSA 10", 100_00, null, "ebay", "boundary, does not"),
            ],
        };
        using var client = app.CreateClient();

        var dto = await client.GetFromJsonAsync<CardPageSnapshotDto>("/api/v1/cards/42", JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal("1 / 30d", Assert.Single(dto!.Signals.Rows, r => r.Name == "Sales volume").Value);
        Assert.Equal(
            "Needs 60+ post-seam days · 0 recorded",
            Assert.Single(dto.Signals.Rows, r => r.Name == "Churn 30d").Tooltip);
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
