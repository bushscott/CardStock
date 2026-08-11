namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Marks a type owned by PokemonInvestBatch. Every type carrying this is mapped
/// with ToView, never ToTable, so EF can neither migrate it nor write to it.
/// Asserted by SchemaModelTests.
/// </summary>
public interface IScraperOwned;
