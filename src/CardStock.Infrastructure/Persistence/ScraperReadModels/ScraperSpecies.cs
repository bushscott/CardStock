namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.species (sibling ADR-0011). PK is the
/// national dex number. Owned by PokemonInvestBatch.</summary>
public class ScraperSpecies : IScraperOwned
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public short Generation { get; init; }

    public required string Region { get; init; }

    public required string Color { get; init; }

    /// <summary>Null for Generation 4 onward — PokéAPI stopped assigning habitats.</summary>
    public string? Habitat { get; init; }

    /// <summary>0 Ordinary · 1 Legendary · 2 Mythical (sibling's SpeciesStatus).</summary>
    public short Status { get; init; }

    /// <summary>Chain depth from the evolution root; 0 = basic.</summary>
    public short Stage { get; init; }

    public int? EvolvesFromSpeciesId { get; init; }

    public required string GradientStart { get; init; }

    public required string GradientEnd { get; init; }
}
