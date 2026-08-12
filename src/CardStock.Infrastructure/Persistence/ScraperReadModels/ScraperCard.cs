namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.cards. Owned by PokemonInvestBatch. Only the
/// columns CardStock actually reads are carried; the crawler's scheduler state
/// is none of this application's business.
/// </summary>
public class ScraperCard : IScraperOwned
{
    /// <summary>PriceCharting's own product id. Never generated locally.</summary>
    public long Id { get; init; }

    public long SetId { get; init; }

    public required string Name { get; init; }

    public required string Url { get; init; }

    public string? ImageHash { get; init; }

    /// <summary>
    /// When the crawler last fetched this card's page. Drives the 24h refresh
    /// decision (D-062) and the as-of stamp (D-077).
    ///
    /// DATA_MODEL.md:163 classifies this as mutable scheduler state under Rule 3,
    /// with the durable history in the visits table, which CardStock does not
    /// mirror. That warning is about treating caches as analytical FACTS; for
    /// "when did we last look", this cache is the answer.
    /// </summary>
    public DateTimeOffset? LastVisitedAt { get; init; }

    /// <summary>Set by hand when the product is gone from the source outright.</summary>
    public DateTimeOffset? DelistedAt { get; init; }

    /// <summary>Set when the parser proved the page is not a card at all.</summary>
    public DateTimeOffset? NotACardAt { get; init; }
}
