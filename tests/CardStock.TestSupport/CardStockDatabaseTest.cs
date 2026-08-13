using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CardStock.TestSupport;

/// <summary>
/// A base class for tests that need real PostgreSQL. Each test gets its own
/// database: the crawler's schema is applied first, then CardStock's migrations
/// run on top — the same layering as production, so a test can exercise the
/// cross-schema join that the product depends on.
///
/// Building a database per test costs a second or two and buys honest
/// isolation: nothing leaks between tests, nothing depends on ordering, and two
/// suites can run side by side.
///
/// <c>CARDSTOCK_TEST_DB</c> supplies the host and credentials, pointing at the
/// Pi — database development happens there and there is no local Postgres by
/// decision (owner, 2026-08-11). The database it names is only a template and
/// is never written to. Unset, tests skip rather than fail.
/// </summary>
public abstract class CardStockDatabaseTest : IAsyncLifetime
{
    private string _databaseName = "";

    public static string? Template => Environment.GetEnvironmentVariable("CARDSTOCK_TEST_DB");

    public static bool Available => !string.IsNullOrWhiteSpace(Template);

    /// <summary>Points at this test's own database. Empty when skipping.</summary>
    protected string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        if (!Available)
        {
            return;
        }

        // A name no other run can collide with, so a crashed run leaves an
        // obvious orphan rather than corrupting the next one.
        _databaseName = $"cardstock_test_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(Maintenance()))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(Template)
        {
            Database = _databaseName,
        }.ConnectionString;

        await ApplyScraperSchemaAsync();

        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// The crawler's tables must exist before CardStock's migrations run. The
    /// script replays the sibling's full migration chain, including the
    /// shapes → fingerprints rename, so the result is its true current schema.
    /// </summary>
    private async Task ApplyScraperSchemaAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "scraper-schema.sql");
        var sql = await File.ReadAllTextAsync(path);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_databaseName.Length == 0)
        {
            return;
        }

        // Npgsql keeps pooled connections open, and PostgreSQL will not drop a
        // database anyone is still attached to.
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(Maintenance());
        await admin.OpenAsync();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            // Plain DROP asks only that our own connections be gone, which
            // ClearAllPools just arranged. WITH (FORCE) terminates a straggler
            // instead, and is the one PostgreSQL can refuse — 42501 when a
            // backend belongs to a role we are not a member of. So it is the
            // fallback, not what we lead with: teardown of a scratch database
            // must never be the reason a passing test reports failure.
            if (await TryDropAsync(admin, force: false) || await TryDropAsync(admin, force: true))
            {
                return;
            }

            NpgsqlConnection.ClearAllPools();
            await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
        }

        // Out of tries. Leave it: the name is a GUID, so the orphan is inert and
        // DROP DATABASE over cardstock_test_% sweeps it up later.
    }

    private async Task<bool> TryDropAsync(NpgsqlConnection admin, bool force)
    {
        try
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{_databaseName}\"{(force ? " WITH (FORCE)" : "")}", admin);
            await drop.ExecuteNonQueryAsync();
            return true;
        }
        catch (PostgresException e) when (
            e.SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.ObjectInUse)
        {
            return false;
        }
    }

    protected CardStockDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CardStockDbContext>()
            .UseCardStock(ConnectionString)
            .Options);

    /// <summary>
    /// A factory over this test's own database, for readers that acquire their
    /// own context per call instead of being handed one.
    /// </summary>
    protected IDbContextFactory<CardStockDbContext> NewContextFactory() => new FixedFactory(ConnectionString);

    private sealed class FixedFactory(string connectionString) : IDbContextFactory<CardStockDbContext>
    {
        public CardStockDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<CardStockDbContext>()
                .UseCardStock(connectionString)
                .Options);
    }

    /// <summary>CREATE DATABASE cannot run inside the database being created,
    /// so administration goes through the always-present <c>postgres</c>.</summary>
    private static string Maintenance() =>
        new NpgsqlConnectionStringBuilder(Template) { Database = "postgres" }.ConnectionString;
}
