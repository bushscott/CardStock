using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Cards;
using CardStock.Web.Services;

namespace CardStock.Web.Tests;

public class CardApiClientTests
{
    [Fact]
    public async Task GetSnapshotAsync_returns_the_dto_on_200()
    {
        var expected = Fixtures.Snapshot(cardId: 630417, title: "Umbreon VMAX (Alt Art)");
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expected) });
        var client = new CardApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await client.GetSnapshotAsync(630417);

        Assert.False(result.Failed);
        Assert.Null(result.NotFoundReason);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(630417, result.Snapshot!.CardId);
        Assert.Equal("Umbreon VMAX (Alt Art)", result.Snapshot!.Identity.Title);
    }

    [Fact]
    public async Task GetSnapshotAsync_returns_the_reason_on_404()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new Dictionary<string, object> { ["reason"] = "not_a_card" }),
            });
        var client = new CardApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await client.GetSnapshotAsync(1);

        Assert.False(result.Failed);
        Assert.Null(result.Snapshot);
        Assert.Equal("not_a_card", result.NotFoundReason);
    }

    [Fact]
    public async Task GetSnapshotAsync_reports_failed_when_the_request_throws()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("unreachable"));
        var client = new CardApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await client.GetSnapshotAsync(1);

        Assert.True(result.Failed);
        Assert.Null(result.Snapshot);
        Assert.Null(result.NotFoundReason);
    }
}

/// <summary>A single-handler stub: routes every request through one delegate,
/// so tests can branch on the request or throw to simulate a dead worker.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
}

/// <summary>Minimal, valid wire DTOs for tests that only care about identity fields.</summary>
internal static class Fixtures
{
    public static CardPageSnapshotDto Snapshot(
        long cardId = 630417,
        string title = "Umbreon VMAX (Alt Art)",
        string setName = "Evolving Skies",
        string? collectorNumber = "215",
        int? setSize = null,
        DateTimeOffset? lastVisitedAt = null) =>
        new(
            cardId,
            new IdentityDto(title, collectorNumber, setSize, setName, HasImage: true, DelistedAt: null),
            new PricesDto("2026-08", []),
            new CensusDto([], PsaTotal: 0, CgcTotal: 0, ObservedAt: null, QualifyingObservations: 0, Metrics: []),
            new SignalsDto(0, 0, []),
            new FreshnessDto(lastVisitedAt));
}
