using System.Net;
using System.Text;
using CardStock.Application.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class CatalogApiClientTests
{
    private sealed class Stub(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? RequestedPath;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            RequestedPath = request.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static CatalogApiClient Client(Stub stub) =>
        new(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task A_set_dto_round_trips()
    {
        var stub = new Stub(HttpStatusCode.OK,
            """{"setId":7,"name":"Evolving Skies","metadataStatus":"matched","code":"swsh7","era":"SWSH","cardsTracked":237,"firstSaleMonth":"2021-12","roster":[]}""");

        var result = await Client(stub).GetSetAsync(7);

        Assert.Equal("/api/v1/sets/7", stub.RequestedPath);
        Assert.False(result.NotFound);
        Assert.False(result.Failed);
        Assert.Equal("Evolving Skies", result.Value!.Name);
    }

    [Fact]
    public async Task A_character_dto_round_trips()
    {
        var stub = new Stub(HttpStatusCode.OK,
            """{"speciesId":32,"name":"Nidoran♀","gradientStart":"#2B2D42","gradientEnd":"#5C6B9E","chips":[],"printings":0,"setsCount":0,"totalValueCents":0,"pricedPrintings":0,"roster":[]}""");

        var result = await Client(stub).GetCharacterAsync("nidoran-f");

        Assert.Equal("/api/v1/characters/nidoran-f", stub.RequestedPath);
        Assert.False(result.NotFound);
        Assert.False(result.Failed);
        Assert.Equal("Nidoran♀", result.Value!.Name);
    }

    [Fact]
    public async Task A_slug_with_a_space_is_escaped_in_the_request_path()
    {
        // Synthetic input, not a real slug -- species slugs are purpose-built clean by
        // construction (character.md §1: "umbreon", "nidoran-f"; the "♀"-style display
        // name never reaches the URL). This pins Uri.EscapeDataString's behavior as
        // defensive coverage, not a claim that escaping is load-bearing in practice.
        var stub = new Stub(HttpStatusCode.OK,
            """{"speciesId":1,"name":"Test","gradientStart":"#000000","gradientEnd":"#FFFFFF","chips":[],"printings":0,"setsCount":0,"totalValueCents":0,"pricedPrintings":0,"roster":[]}""");

        await Client(stub).GetCharacterAsync("mr mime");

        Assert.Equal("/api/v1/characters/mr%20mime", stub.RequestedPath);
    }

    [Fact]
    public async Task A_404_is_NotFound_not_a_failure()
    {
        var result = await Client(new Stub(HttpStatusCode.NotFound,
            """{"reason":"unknown"}""")).GetSetAsync(999);
        Assert.True(result.NotFound);
        Assert.False(result.Failed);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task A_transport_error_is_Failed()
    {
        var throwing = new HttpClient(new ThrowingHandler()) { BaseAddress = new Uri("http://localhost/") };
        var result = await new CatalogApiClient(throwing).GetSetAsync(7);
        Assert.True(result.Failed);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("down");
    }
}
