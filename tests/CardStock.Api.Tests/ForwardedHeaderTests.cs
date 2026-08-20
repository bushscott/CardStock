using System.Net;

namespace CardStock.Api.Tests;

public class ForwardedHeaderTests
{
    [Fact]
    public async Task Cf_connecting_ip_from_the_loopback_proxy_partitions_the_limiter()
    {
        using var app = new TestApp
        {
            WorkerIntakeHandler = new StubHandler(HttpStatusCode.OK),
            ExpressPerHour = 1,
            RemoteIp = IPAddress.Loopback,
        };
        using var client = app.CreateClient();

        var first = await Post(client, "203.0.113.7");
        var second = await Post(client, "203.0.113.8");
        var third = await Post(client, "203.0.113.7");

        Assert.Equal(HttpStatusCode.OK, first);
        Assert.Equal(HttpStatusCode.OK, second);   // a different visitor gets a fresh bucket
        Assert.Equal(HttpStatusCode.TooManyRequests, third);
    }

    [Fact]
    public async Task A_forged_header_from_a_non_proxy_address_is_ignored()
    {
        using var app = new TestApp
        {
            WorkerIntakeHandler = new StubHandler(HttpStatusCode.OK),
            ExpressPerHour = 1,
            RemoteIp = IPAddress.Parse("192.168.0.99"),
        };
        using var client = app.CreateClient();

        var first = await Post(client, "203.0.113.7");
        var second = await Post(client, "203.0.113.8");

        Assert.Equal(HttpStatusCode.OK, first);
        // Different spoofed headers, same real connection: same bucket.
        Assert.Equal(HttpStatusCode.TooManyRequests, second);
    }

    private static async Task<HttpStatusCode> Post(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cards/1/refresh");
        request.Headers.TryAddWithoutValidation("CF-Connecting-IP", forwardedFor);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
