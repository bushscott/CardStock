using CardStock.Domain.Census;

namespace CardStock.Application.Catalog;

public interface ISetPageReader
{
    public Task<SetPageSnapshot?> GetAsync(long setId, CancellationToken ct = default);
}

/// <summary>One set page in one read: header facts plus the full roster
/// (full-roster-virtualized, D-110 — no cap, no "most-traded" fiction).</summary>
public sealed record SetPageSnapshot(
    long SetId,
    string Name,
    string MetadataStatus,
    string? Code,
    string? Era,
    int CardsTracked,
    DateOnly? FirstSale,
    IReadOnlyList<RosterCard> Roster);

public sealed record RosterCard(
    long CardId,
    string Name,
    bool HasImage,
    int? PriceCents,
    decimal? Roc3M,
    PopulationDelta.Result Pop,
    int Sales30d);
