using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>What any catalog fetch can come back as — the pages' three top states.</summary>
public sealed record CatalogResult<T>(T? Value, bool NotFound, bool Failed) where T : class;

/// <summary>
/// The measured sprite art boxes behind D-113's normalization — a static wwwroot asset,
/// not an API route, regenerated only when the icon corpus changes. Values are
/// [x, y, w, h, canvasW, canvasH] per species id.
/// </summary>
public sealed record SpriteArtDoc(string GeneratedOn, string Method, Dictionary<int, int[]> Sprites);

public sealed class CatalogApiClient(HttpClient http)
{
    public Task<CatalogResult<SetPageDto>> GetSetAsync(long id, CancellationToken ct = default) =>
        GetAsync<SetPageDto>($"api/v1/sets/{id}", ct);

    public Task<CatalogResult<CharacterPageDto>> GetCharacterAsync(string slug, CancellationToken ct = default) =>
        GetAsync<CharacterPageDto>($"api/v1/characters/{Uri.EscapeDataString(slug)}", ct);

    public Task<CatalogResult<BrowseSetsDto>> GetBrowseSetsAsync(CancellationToken ct = default) =>
        GetAsync<BrowseSetsDto>("api/v1/browse/sets", ct);

    public Task<CatalogResult<BrowseSpeciesDto>> GetBrowseSpeciesAsync(CancellationToken ct = default) =>
        GetAsync<BrowseSpeciesDto>("api/v1/browse/species", ct);

    public static string SpeciesIconUrl(int id) => $"api/v1/species/{id}/icon";

    /// <summary>Null degrades the wall to the plain 1×-canvas draw — never an error state.</summary>
    public async Task<IReadOnlyDictionary<int, int[]>?> GetSpriteArtAsync(CancellationToken ct = default)
    {
        var result = await GetAsync<SpriteArtDoc>("sprite-art.json", ct);
        return result.Value?.Sprites;
    }

    private async Task<CatalogResult<T>> GetAsync<T>(string path, CancellationToken ct)
        where T : class
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new CatalogResult<T>(null, NotFound: true, Failed: false);
            }

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<T>(ct);
            return new CatalogResult<T>(dto, NotFound: false, Failed: dto is null);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return new CatalogResult<T>(null, NotFound: false, Failed: true);
        }
    }
}
