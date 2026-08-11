using CardStock.Infrastructure.Persistence.Entities;
using CardStock.Infrastructure.Persistence.ScraperReadModels;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The architecture of ADR-0001, exercised against real PostgreSQL: CardStock's
/// schema and the crawler's in one database, joined in one statement, with the
/// write path closed by construction rather than by convention.
/// </summary>
public class ScraperReadTests : CardStockDatabaseTest
{
    [SkippableFact]
    public async Task A_cardstock_row_and_a_scraper_row_join_in_one_query()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        await using var db = NewContext();

        // Seeded with raw SQL because CardStock's model cannot write these
        // tables -- which is the point being demonstrated.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (1, 'base-set', 'Base Set', now(), now());

            INSERT INTO public.cards (id, set_id, url, name, first_seen_at, last_seen_at,
                                      any_bucket_at_cap, failure_streak)
            VALUES (42, 1, '/game/pokemon-base-set/charizard-4', 'Charizard', now(), now(), false, 0);
            """);

        db.Users.Add(new AppUser
        {
            Email = "owner@example.com",
            PasswordHash = "not-a-real-hash",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        // The query shape the entire product rests on. A separate database
        // could not express this in one statement at all.
        var row = await db.ScraperCards
            .Where(c => c.Id == 42)
            .Join(db.ScraperSets, c => c.SetId, s => s.Id, (c, s) => new { Card = c.Name, Set = s.Name })
            .SingleAsync();

        Assert.Equal("Charizard", row.Card);
        Assert.Equal("Base Set", row.Set);

        // And CardStock's own schema is genuinely populated alongside it.
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [SkippableFact]
    public async Task Writing_a_scraper_entity_through_EF_is_impossible()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        await using var db = NewContext();

        db.ScraperSets.Add(new ScraperSet
        {
            Id = 999,
            Slug = "should-not-save",
            Name = "Should Not Save",
        });

        // ToView means "not mapped to a table", so EF refuses before PostgreSQL
        // is ever asked. This guarantee holds in a test database owned by
        // cardstock_tester, where the production grants do not exist -- which is
        // exactly the case a grant-only defence would miss.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());

        Assert.Contains("not mapped to a table", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task The_two_migration_histories_do_not_collide()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        await using var db = NewContext();

        var cardStockHistory = await db.Database
            .SqlQuery<string?>($"SELECT to_regclass('cardstock.__cardstock_migrations_history')::text AS \"Value\"")
            .SingleAsync();

        var crawlerHistory = await db.Database
            .SqlQuery<string?>($"SELECT to_regclass('public.\"__EFMigrationsHistory\"')::text AS \"Value\"")
            .SingleAsync();

        // Each lineage has its own table, in its own schema. Without the
        // MigrationsHistoryTable override, CardStock's rows would land in the
        // crawler's table -- HasDefaultSchema alone does not move it.
        Assert.NotNull(cardStockHistory);
        Assert.NotNull(crawlerHistory);
        Assert.NotEqual(cardStockHistory, crawlerHistory);
    }
}
