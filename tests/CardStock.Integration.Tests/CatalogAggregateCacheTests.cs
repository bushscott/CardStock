using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The corpus aggregate cache against real PostgreSQL. Proves what a unit test
/// cannot: the alias-free SqlQuery&lt;LatestPsa10Row&gt; resolves correctly under
/// UseSnakeCaseNamingConvention, the D-078 revision case picks the newest
/// observed_at, and the TTL gate genuinely re-queries the database on expiry
/// rather than serving stale rows forever.
/// </summary>
public class CatalogAggregateCacheTests : CardStockDatabaseTest
{
    private sealed class MutableTime(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now = start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (7, 'es', 'Evolving Skies', now(), now());
            INSERT INTO public.cards
                (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap) VALUES
              (1, 7, 'A', 'https://x/1', now(), now(), false),
              (2, 7, 'B', 'https://x/2', now(), now(), false);
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (1, 5, '2026-07-01', 12000, '2026-08-01T00:00:00Z'),
              (1, 5, '2026-07-01', 12500, '2026-08-10T00:00:00Z'),
              (2, 0, '2026-07-01',  9999, '2026-08-01T00:00:00Z');
            """);
    }

    [SkippableFact]
    public async Task The_dictionary_holds_the_revised_latest_for_psa10_only()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();
        var time = new MutableTime(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero));
        var cache = new CatalogAggregateCache(NewContextFactory(), time, TimeSpan.FromMinutes(5));

        var latest = await cache.LatestPsa10ByCardAsync();

        Assert.Equal(12500, latest[1]);        // the D-078 revision wins
        Assert.False(latest.ContainsKey(2));   // Ungraded tier never enters
    }

    [SkippableFact]
    public async Task Within_the_ttl_the_computation_runs_once_and_after_it_refreshes()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();
        var time = new MutableTime(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero));
        var cache = new CatalogAggregateCache(NewContextFactory(), time, TimeSpan.FromMinutes(5));

        var first = await cache.LatestPsa10ByCardAsync();
        // A new row lands after the first computation.
        await using (var db = NewContext())
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at)
                VALUES (1, 5, '2026-08-01', 13000, '2026-08-16T00:00:00Z');
                """);
        }

        Assert.Same(first, await cache.LatestPsa10ByCardAsync());   // still cached

        time.Now = time.Now.AddMinutes(6);
        var refreshed = await cache.LatestPsa10ByCardAsync();
        Assert.Equal(13000, refreshed[1]);                          // recomputed
    }
}
