using System.Net;

namespace CardStock.Api.Tests;

public class RefreshEndpointTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task The_workers_status_passes_through_verbatim(HttpStatusCode status)
    {
        using var app = new TestApp { WorkerIntakeHandler = new StubHandler(status) };
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/v1/cards/42/refresh", content: null);

        Assert.Equal(status, response.StatusCode);
    }

    [Fact]
    public async Task An_unreachable_worker_is_a_502()
    {
        using var app = new TestApp { WorkerIntakeHandler = new ThrowingHandler() };
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/v1/cards/42/refresh", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task A_third_request_within_the_hour_is_rate_limited()
    {
        using var app = new TestApp
        {
            WorkerIntakeHandler = new StubHandler(HttpStatusCode.OK),
            ExpressPerHour = 2,
        };
        using var client = app.CreateClient();

        var first = await client.PostAsync("/api/v1/cards/1/refresh", content: null);
        var second = await client.PostAsync("/api/v1/cards/2/refresh", content: null);
        var third = await client.PostAsync("/api/v1/cards/3/refresh", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("The worker is unreachable.");
    }
}
