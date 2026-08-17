using System.Net;

namespace CardStock.Api.Tests;

public class SpeciesIconEndpointTests
{
    [Fact]
    public async Task A_stored_icon_serves_as_immutable_png()
    {
        using var app = new TestApp();
        File.WriteAllBytes(Path.Combine(app.SpeciesIconDirectory, "197.png"),
            [0x89, 0x50, 0x4E, 0x47]);
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/species/197/icon");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(response.Headers.CacheControl!.Extensions, e => e.Name == "immutable");
    }

    [Fact]
    public async Task A_missing_icon_is_a_404_the_client_gradient_covers()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/v1/species/9999/icon")).StatusCode);
    }
}
