namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.sales. Owned by PokemonInvestBatch. Immutable
/// completed sales, deduped by (Source, SourceId).
///
/// Like the census, this series begins at each card's first crawler visit
/// (D-001), so it is young and ragged rather than historical.
/// </summary>
public class ScraperSale : IScraperOwned
{
    public long Id { get; init; }

    public long CardId { get; init; }

    /// <summary>One of ebay, tcgplayer, goldin, heritage, pwcc.</summary>
    public required string Source { get; init; }

    public required string SourceId { get; init; }

    public DateOnly SoldOn { get; init; }

    /// <summary>Grade bucket label exactly as the source page named it.</summary>
    public required string GradeTier { get; init; }

    public int PriceCents { get; init; }

    public int? ListedPriceCents { get; init; }

    /// <summary>
    /// Raw third-party listing title, stored exactly as scraped. MUST be
    /// HTML-encoded at render (D-029) -- Razor's @ does this by default, so the
    /// rule is simply that this value never passes through MarkupString.
    /// </summary>
    public required string Title { get; init; }

    public DateTimeOffset CapturedAt { get; init; }
}
