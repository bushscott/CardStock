using System.Net;

namespace CardStock.Api.Tests;

public class HostFilteringTests
{
    [Fact]
    public async Task An_unlisted_host_gets_400_and_a_listed_one_gets_through()
    {
        using var app = new TestApp { AllowedHosts = "cardstock.pro;localhost;127.0.0.1" };
        using var client = app.CreateClient();

        using var evil = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        evil.Headers.Host = "evil.example";
        var refused = await client.SendAsync(evil);

        using var listed = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        listed.Headers.Host = "cardstock.pro";
        var served = await client.SendAsync(listed);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
    }
}
