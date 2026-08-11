namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.populations. Owned by PokemonInvestBatch.
/// Change-only append, same as price history; deltas come from LAG() over
/// ObservedAt.
///
/// History begins at each card's first crawler visit and the source publishes
/// none, so this series is ragged and young (D-001). Every census metric stays
/// LOCKED well into 2027 under the 2026-09-01 floor (D-033).
/// </summary>
public class ScraperPopulation : IScraperOwned
{
    public long CardId { get; init; }

    /// <summary>"psa" or "cgc". The crawler's parser rejects anything else as drift.</summary>
    public required string Grader { get; init; }

    /// <summary>1..10, stored as smallint.</summary>
    public short Grade { get; init; }

    public int Population { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
}
