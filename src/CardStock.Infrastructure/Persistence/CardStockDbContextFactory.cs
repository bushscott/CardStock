using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace CardStock.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef</c>. Generating migrations never
/// connects; applying them uses CARDSTOCK_DB, so the runtime role never holds
/// DDL rights.
/// </summary>
public class CardStockDbContextFactory : IDesignTimeDbContextFactory<CardStockDbContext>
{
    public CardStockDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CARDSTOCK_DB")
            ?? "Host=localhost;Database=pokemon;Username=cardstock_owner";

        Guard(connectionString);

        var options = new DbContextOptionsBuilder<CardStockDbContext>()
            .UseCardStock(connectionString)
            .Options;

        return new CardStockDbContext(options);
    }

    /// <summary>
    /// A stale POKEMON_DB left in the shell would create CardStock's tables
    /// owned by pokemon_owner, which silently hands pokemon_app access to
    /// cardstock.users through the crawler's own default privileges. Refusing is
    /// cheaper than detecting it afterwards.
    /// </summary>
    private static void Guard(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var isThrowawayTestDatabase =
            builder.Database?.StartsWith("cardstock_test_", StringComparison.Ordinal) == true;

        if (builder.Username is not "cardstock_owner" && !isThrowawayTestDatabase)
        {
            throw new InvalidOperationException(
                $"Refusing to migrate as '{builder.Username}'. CARDSTOCK_DB must use cardstock_owner. " +
                "Creating CardStock's tables under another role grants that role's peers access to user data.");
        }
    }
}
