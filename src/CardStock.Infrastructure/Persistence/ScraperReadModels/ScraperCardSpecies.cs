namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.card_species — the card ↔ species junction.
/// Current-state, not append-only (sibling ADR-0011 deviation one).</summary>
public class ScraperCardSpecies : IScraperOwned
{
    public long CardId { get; init; }

    public int SpeciesId { get; init; }
}
