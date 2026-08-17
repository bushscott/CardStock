namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.species_types. 1–2 rows per species, ordered by Slot.</summary>
public class ScraperSpeciesType : IScraperOwned
{
    public int SpeciesId { get; init; }

    public short Slot { get; init; }

    public required string Type { get; init; }
}
