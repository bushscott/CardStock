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

    /// <summary>Set by hand when the product is gone from the source outright.</summary>
    public DateTimeOffset? DelistedAt { get; init; }

    /// <summary>Set when the parser proved the page is not a card at all.</summary>
    public DateTimeOffset? NotACardAt { get; init; }
}
