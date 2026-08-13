using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Cards;

namespace CardStock.Web.Services;

/// <summary>What a snapshot fetch can come back as — the page's three top states.</summary>
public sealed record SnapshotResult(CardPageSnapshotDto? Snapshot, string? NotFoundReason, bool Failed);

public sealed class CardApiClient(HttpClient http)
{
    public async Task<SnapshotResult> GetSnapshotAsync(long id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync($"api/v1/cards/{id}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(ct);
                return new SnapshotResult(null,
                    problem?.GetValueOrDefault("reason")?.ToString() ?? "unknown", Failed: false);
            }

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<CardPageSnapshotDto>(ct);
            return new SnapshotResult(dto, null, Failed: false);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return new SnapshotResult(null, null, Failed: true);
        }
    }

    public async Task<IReadOnlyList<SaleDto>?> GetSalesAsync(long id, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<List<SaleDto>>($"api/v1/cards/{id}/sales", ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return null; // the ledger panel's own error state; the page lives on
        }
    }

    public async Task<int> RefreshAsync(long id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsync($"api/v1/cards/{id}/refresh", content: null, ct);
            return (int)response.StatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return 502;
        }
    }

    public static string ImageUrl(long id) => $"api/v1/cards/{id}/image";
}
