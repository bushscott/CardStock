namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.species_egg_groups. 1–2 display-named rows per species.</summary>
public class ScraperSpeciesEggGroup : IScraperOwned
{
    public int SpeciesId { get; init; }

    public required string EggGroup { get; init; }
}
