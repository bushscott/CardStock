using CardStock.Infrastructure.Persistence.ScraperReadModels;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Persistence;

/// <summary>
/// Every mapping to a PokemonInvestBatch table, in one file so none can be
/// forgotten piecemeal.
///
/// ToView, not ToTable(..., ExcludeFromMigrations()). Verified 2026-08-11 by
/// reading scaffolded migrations: the ExcludeFromMigrations form still emits
/// cross-schema foreign keys into public whenever a relationship is configured,
/// and omitting a mapping entirely emits CreateTable(schema: "public") in Up()
/// with DropTable(schema: "public") in Down(). ToView makes both impossible by
/// construction, and additionally turns an EF-level write into an
/// InvalidOperationException rather than a permission error found in production.
/// </summary>
internal static class ScraperViews
{
    public static void Map(ModelBuilder builder)
    {
        builder.Entity<ScraperSet>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToView("sets", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperCard>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToView("cards", CardStockDbContext.ScraperSchema);
        });

        // Composite key mirrors the crawler's PokemonDbContext.cs:52.
        builder.Entity<ScraperPriceMonth>(entity =>
        {
            entity.HasKey(x => new { x.CardId, x.Tier, x.Month, x.ObservedAt });
            entity.ToView("price_months", CardStockDbContext.ScraperSchema);
        });

        // Composite key mirrors the crawler's PokemonDbContext.cs:58.
        builder.Entity<ScraperPopulation>(entity =>
        {
            entity.HasKey(x => new { x.CardId, x.Grader, x.Grade, x.ObservedAt });
            entity.ToView("populations", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSale>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToView("sales", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSpecies>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToView("species", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSpeciesType>(entity =>
        {
            entity.HasKey(x => new { x.SpeciesId, x.Slot });
            entity.ToView("species_types", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSpeciesEggGroup>(entity =>
        {
            entity.HasKey(x => new { x.SpeciesId, x.EggGroup });
            entity.ToView("species_egg_groups", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperCardSpecies>(entity =>
        {
            entity.HasKey(x => new { x.CardId, x.SpeciesId });
            entity.ToView("card_species", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSetDetail>(entity =>
        {
            entity.HasKey(x => x.SetId);
            entity.ToView("set_details", CardStockDbContext.ScraperSchema);
        });

        // Deliberately not mapped: species_names (nothing reads it until a later
        // phase) and card_tagging (lane bookkeeping). A mapping with no consumer
        // is drift waiting to happen.
    }
}
