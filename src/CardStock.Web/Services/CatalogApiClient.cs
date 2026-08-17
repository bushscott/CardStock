using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>What any catalog fetch can come back as — the pages' three top states.</summary>
public sealed record CatalogResult<T>(T? Value, bool NotFound, bool Failed) where T : class;

public sealed class CatalogApiClient(HttpClient http)
{
    public Task<CatalogResult<SetPageDto>> GetSetAsync(long id, CancellationToken ct = default) =>
        GetAsync<SetPageDto>($"api/v1/sets/{id}", ct);

    public static string SpeciesIconUrl(int id) => $"api/v1/species/{id}/icon";

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
