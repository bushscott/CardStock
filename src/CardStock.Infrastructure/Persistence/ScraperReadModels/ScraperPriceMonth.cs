namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.price_months. Owned by PokemonInvestBatch.
///
/// CHANGE-ONLY APPEND: a row exists only where the value CHANGED from the
/// previous observation, so absence means "unchanged", never "missing". A naive
/// WHERE month = X returns nothing for most cards in most months, and "latest"
/// means max(ObservedAt) per key, not the newest month. Any read that ignores
/// this computes plausible-looking wrong numbers.
///
/// This is the one deep series: backfilled to ~Dec 2020 at each card's first
/// visit (D-002).
/// </summary>
public class ScraperPriceMonth : IScraperOwned
{
    public long CardId { get; init; }

    public PriceTier Tier { get; init; }

    public DateOnly Month { get; init; }

    public int PriceCents { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
}
