using CardStock.Infrastructure.Persistence;
using CardStock.Infrastructure.Persistence.ScraperReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardStock.Infrastructure.Tests.Persistence;

/// <summary>
/// The boundary guarantees of ADR-0001, asserted against the compiled EF model
/// with no database. Each catches a failure that is otherwise silent until it
/// reaches production, and one of them would destroy data nobody can rebuild.
/// </summary>
public class SchemaModelTests
{
    private static DbContextOptions<CardStockDbContext> Options() =>
        new DbContextOptionsBuilder<CardStockDbContext>()
            .UseCardStock("Host=model-only")
            .Options;

    private static IModel Model()
    {
        using var context = new CardStockDbContext(Options());
        return context.Model;
    }

    /// <summary>
    /// Catches a mapping that would put a CardStock table in the crawler's
    /// schema -- and, in Down(), drop it.
    /// </summary>
    [Fact]
    public void Nothing_CardStock_migrates_lives_outside_its_own_schema()
    {
        foreach (var entity in Model().GetEntityTypes())
        {
            if (entity.GetTableName() is not null)
            {
                Assert.Equal(CardStockDbContext.Schema, entity.GetSchema());
            }
        }
    }

    /// <summary>
    /// Catches a crawler type mapped with ToTable instead of ToView, which
    /// re-opens both the cross-schema foreign key and the write path.
    /// </summary>
    [Fact]
    public void Every_scraper_owned_type_is_mapped_to_a_view_and_never_a_table()
    {
        var scraperTypes = Model().GetEntityTypes()
            .Where(e => typeof(IScraperOwned).IsAssignableFrom(e.ClrType))
            .ToList();

        Assert.Equal(5, scraperTypes.Count);

        foreach (var entity in scraperTypes)
        {
            Assert.Null(entity.GetTableName());
            Assert.Equal(CardStockDbContext.ScraperSchema, entity.GetViewSchema());
            Assert.NotNull(entity.GetViewName());
        }
    }

    /// <summary>
    /// HasDefaultSchema alone does NOT relocate the migrations history table --
    /// it stays unqualified and resolves onto the crawler's own. This override
    /// is the only thing keeping the two EF lineages apart.
    /// </summary>
    [Fact]
    public void Migrations_history_table_is_pinned_to_the_cardstock_schema()
    {
        var extension = RelationalOptionsExtension.Extract(Options());

        Assert.Equal("__cardstock_migrations_history", extension.MigrationsHistoryTableName);
        Assert.Equal(CardStockDbContext.Schema, extension.MigrationsHistoryTableSchema);
    }
}
