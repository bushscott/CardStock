using System.Net;
using CardStock.Application.Cards;

namespace CardStock.Api.Tests;

public class ImageEndpointTests
{
    private static CardIdentity Identity(
        long cardId = 42, string? imageHash = null, DateTimeOffset? notACardAt = null) => new(
        CardId: cardId,
        Title: "Charizard #4/102",
        CollectorNumber: "4",
        SetSize: null,
        SetId: 7,
        SetName: "Base Set",
        Species: [],
        ImageHash: imageHash,
        DelistedAt: null,
        NotACardAt: notACardAt);

    private static void WriteFakeImage(TestApp app, string hash)
    {
        var dir = Path.Combine(app.ImageDirectory, hash);
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "1600.jpg"), [0xFF, 0xD8, 0xFF, 0xD9]);
    }

    [Fact]
    public async Task A_card_with_a_stored_image_serves_it_with_an_immutable_cache_header()
    {
        using var app = new TestApp { Identity = Identity(imageHash: "abc123") };
        WriteFakeImage(app, "abc123");
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/image");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl!.Public);
        Assert.Equal(TimeSpan.FromSeconds(31536000), cacheControl.MaxAge);
        Assert.Contains(cacheControl.Extensions, e => e.Name == "immutable");
    }

    [Fact]
    public async Task No_image_hash_is_a_404()
    {
        using var app = new TestApp { Identity = Identity(imageHash: null) };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_hash_with_no_file_on_disk_is_a_404()
    {
        using var app = new TestApp { Identity = Identity(imageHash: "noFileForThisHash") };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_hash_containing_a_path_traversal_segment_is_a_404()
    {
        using var app = new TestApp { Identity = Identity(imageHash: "../../etc/passwd") };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_card_id_is_a_404()
    {
        using var app = new TestApp { Identity = null };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/999/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_not_a_card_id_is_a_404_even_with_a_valid_hash_and_file()
    {
        using var app = new TestApp
        {
            Identity = Identity(
                imageHash: "abc123",
                notACardAt: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)),
        };
        WriteFakeImage(app, "abc123");
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/cards/42/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
