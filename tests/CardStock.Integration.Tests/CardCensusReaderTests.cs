using CardStock.Application.Cards;
using CardStock.Infrastructure.Cards;
using CardStock.Infrastructure.Persistence;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The census reader against real PostgreSQL. Domain proves the aggregation
/// rules; these prove the rows arrive in the shape Domain expects.
/// </summary>
public class CardCensusReaderTests : CardStockDatabaseTest
{
    private CardCensusReader Reader() => new(NewContextFactory());

    private static async Task SeedCardAsync(CardStockDbContext db, long cardId)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (1, 'base-set', 'Base Set', now(), now())
            ON CONFLICT (id) DO NOTHING;
            """);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.cards (id, set_id, url, name, image_hash, first_seen_at, last_seen_at,
                                       any_bucket_at_cap, failure_streak, last_visited_at, delisted_at, not_a_card_at)
             VALUES ({cardId}, 1, '/game/pokemon-base-set/test-card', 'Test Card', null,
                     now(), now(), false, 0, now(), null, null)
             ON CONFLICT (id) DO NOTHING;
             """);
    }

    [SkippableFact]
    public async Task Change_only_semantics_latest_observation_wins()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var earlier = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var later = new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.populations (card_id, grader, grade, population, observed_at)
             VALUES ({42}, 'psa', 8, {100}, {earlier}),
                    ({42}, 'psa', 8, {150}, {later});
             """);

        var census = await Reader().GetAsync(42);

        var psaBar = census.Bars.First(b => b.Grader == "psa" && b.Grade == 8);
        Assert.Equal(150, psaBar.Count);
    }

    [SkippableFact]
    public async Task Absent_cells_are_zero_and_totals_sum_all_grades()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var obs = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.populations (card_id, grader, grade, population, observed_at)
             VALUES ({42}, 'psa', 1, {10}, {obs}),
                    ({42}, 'psa', 6, {20}, {obs}),
                    ({42}, 'psa', 10, {30}, {obs}),
                    ({42}, 'cgc', 3, {15}, {obs});
             """);

        var census = await Reader().GetAsync(42);

        var psaBar8 = census.Bars.First(b => b.Grader == "psa" && b.Grade == 8);
        var psaBar9 = census.Bars.First(b => b.Grader == "psa" && b.Grade == 9);
        var psaBar10 = census.Bars.First(b => b.Grader == "psa" && b.Grade == 10);

        Assert.Equal(0, psaBar8.Count);
        Assert.Equal(0, psaBar9.Count);
        Assert.Equal(30, psaBar10.Count);

        Assert.Equal(60, census.PsaTotal);
        Assert.Equal(15, census.CgcTotal);
    }

    [SkippableFact]
    public async Task Qualifying_observations_respect_the_floor()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var before = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.populations (card_id, grader, grade, population, observed_at)
             VALUES ({42}, 'psa', 8, {10}, {before}),
                    ({42}, 'psa', 8, {20}, {after});
             """);

        var census = await Reader().GetAsync(42);

        Assert.Equal(1, census.QualifyingObservations);
    }

    [SkippableFact]
    public async Task A_card_with_no_census_rows_is_all_zeros()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var census = await Reader().GetAsync(42);

        foreach (var bar in census.Bars)
        {
            Assert.Equal(0, bar.Count);
        }

        Assert.Equal(0, census.PsaTotal);
        Assert.Equal(0, census.CgcTotal);
        Assert.Null(census.ObservedAt);
        Assert.Equal(0, census.QualifyingObservations);
    }
}
