namespace CardStock.Application.Catalog;

/// <summary>Corpus-wide aggregates too slow to compute per request (1,427 ms
/// measured on the Pi, 2026-08-15). Interim until the analytics worker
/// materializes them (D-039); refreshes on a short TTL.</summary>
public interface ICatalogAggregates
{
    public Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(CancellationToken ct = default);
}
