using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Persistence;

public static class CardStockDbContextOptions
{
    /// <summary>
    /// The single configuration point for every CardStockDbContext, wherever it
    /// is built: API DI, Worker DI, the design-time factory, and the test
    /// harness. One place, so a fourth call site cannot quietly omit one of the
    /// three settings -- the sibling repeats UseSnakeCaseNamingConvention() at
    /// three sites with nothing preventing exactly that.
    ///
    /// The migrations history override is load-bearing, not decorative:
    /// HasDefaultSchema does NOT relocate the history table. Without this it
    /// stays unqualified and resolves onto the crawler's own.
    /// </summary>
    public static DbContextOptionsBuilder UseCardStock(
        this DbContextOptionsBuilder builder,
        string connectionString) =>
        builder
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__cardstock_migrations_history", CardStockDbContext.Schema))
            .UseSnakeCaseNamingConvention();

    /// <summary>
    /// The generic overload, for the design-time factory and the test harness.
    /// AddDbContext hands callers the non-generic builder, so both exist and
    /// share one implementation rather than one call site drifting from the
    /// other.
    /// </summary>
    public static DbContextOptionsBuilder<CardStockDbContext> UseCardStock(
        this DbContextOptionsBuilder<CardStockDbContext> builder,
        string connectionString)
    {
        ((DbContextOptionsBuilder)builder).UseCardStock(connectionString);
        return builder;
    }
}
