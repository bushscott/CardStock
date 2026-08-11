namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.sets. Owned by PokemonInvestBatch.</summary>
public class ScraperSet : IScraperOwned
{
    public long Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }
}
