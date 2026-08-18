namespace CardStock.Application.Catalog;

/// <summary>Corpus-wide aggregates too slow to compute per request (1,427 ms
/// measured on the Pi, 2026-08-15). Interim until the analytics worker
/// materializes them (D-039); refreshes on a short TTL.</summary>
public interface ICatalogAggregates
{
    public Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(CancellationToken ct = default);
}

public interface IBrowseReader
{
    public Task<IReadOnlyList<SetTile>> GetSetsAsync(CancellationToken ct = default);

    /// <summary>Ordered by TotalValueCents descending — the Browse caption's
    /// explicit ORDER BY (browse.md §6.3).</summary>
    public Task<IReadOnlyList<SpeciesTile>> GetSpeciesAsync(CancellationToken ct = default);
}

public sealed record SetTile(
    long SetId, string Name, int Cards, long? TopCardId,
    string MetadataStatus, string? Era, DateOnly? ReleasedOn);

public sealed record SpeciesTile(
    int SpeciesId, string Name, string Slug,
    int Printings, long TotalValueCents, IReadOnlyList<string> Types, short Generation,
    string Region, string Status, short Stage, string Color,
    IReadOnlyList<string> EggGroups, string? Habitat);
