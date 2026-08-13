using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using CardStock.Infrastructure.Prices;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The queries, against real PostgreSQL. Domain already proves the rules; these
/// prove the rows arrive in the shape Domain expects -- including date
/// arithmetic, which LINQ-to-objects cannot vouch for.
/// </summary>
public class CardPriceReaderTests : CardStockDatabaseTest
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    private CardPriceReader Reader() => new(NewContextFactory(), new FixedClock(Now));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>
    /// Seeded with raw SQL because CardStock's model cannot write these tables --
    /// which is the guarantee ScraperReadTests exists to demonstrate.
    ///
    /// ExecuteSqlInterpolated, not ExecuteSqlRaw with an interpolated string: the
    /// former parameterises each hole, the latter concatenates and trips EF's
    /// raw-SQL analyzer, which TreatWarningsAsErrors turns into a build failure.
    /// </summary>
    private static async Task SeedCardAsync(
        CardStockDbContext db, long cardId, DateTimeOffset? lastVisited)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (1, 'base-set', 'Base Set', now(), now())
            ON CONFLICT (id) DO NOTHING;
            """);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.cards (id, set_id, url, name, first_seen_at, last_seen_at,
                                       any_bucket_at_cap, failure_streak, last_visited_at)
             VALUES ({cardId}, 1, '/game/pokemon-base-set/test-card', 'Test Card',
                     now(), now(), false, 0, {lastVisited});
             """);
    }

    [SkippableFact]
    public async Task An_unknown_card_id_returns_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        Assert.Null(await Reader().GetAsync(999_999));
    }

    [SkippableFact]
    public async Task A_card_with_no_prices_returns_six_empty_tiers_not_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now.AddDays(-3));

        var snapshot = await Reader().GetAsync(42);

        Assert.NotNull(snapshot);
        Assert.Equal(6, snapshot.Tiers.Count);
        Assert.All(snapshot.Tiers, t => Assert.IsType<NoPriceSeries>(t.Price));
    }

    [SkippableFact]
    public async Task Last_visited_at_comes_back_so_the_page_can_decide_about_refreshing()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        var visited = Now.AddDays(-3);
        await SeedCardAsync(db, 42, visited);

        var snapshot = await Reader().GetAsync(42);

        Assert.NotNull(snapshot);
        Assert.Equal(visited, snapshot.LastVisitedAt);
    }

    /// <summary>
    /// The Charmeleon #24 shape, from the live database: Grade 8 with September
    /// 2021 missing, $299.99 before it and $40.00 after. The gap must survive the
    /// round trip, and nothing may smooth the cliff.
    /// </summary>
    [SkippableFact]
    public async Task A_gap_in_the_month_axis_survives_the_query()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (42, 2, DATE '2021-08-01', 29999, now()),
              (42, 2, DATE '2021-10-01',  4000, now());
            """);

        var snapshot = await Reader().GetAsync(42);
        var grade8 = snapshot!.Tiers.Single(t => t.Tier == PriceTier.Grade8);
        var window = PriceWindow.Of(grade8.Series, new DateOnly(2021, 10, 1), 3);

        Assert.IsType<ObservedPrice>(window[0]);
        Assert.IsType<MissingMonth>(window[1]);
        Assert.IsType<ObservedPrice>(window[2]);
    }

    /// <summary>
    /// Two rows for one month, resolved through the real query rather than in
    /// memory. Charizard #24 held exactly this for 2026-08-01.
    /// </summary>
    [SkippableFact]
    public async Task The_later_observation_of_a_revised_month_wins()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (42, 5, DATE '2026-08-01', 2861, TIMESTAMPTZ '2026-08-03 00:00:00Z'),
              (42, 5, DATE '2026-08-01', 2500, TIMESTAMPTZ '2026-08-11 00:00:00Z');
            """);

        var snapshot = await Reader().GetAsync(42);
        var psa10 = snapshot!.Tiers.Single(t => t.Tier == PriceTier.Psa10);

        Assert.Equal(2500, Assert.Single(psa10.Series.Points).PriceCents);
        Assert.Equal(2500, Assert.IsType<PriceAvailable>(psa10.Price).PriceCents);
    }

    [SkippableFact]
    public async Task Sales_land_in_the_right_window_under_real_date_arithmetic()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sales
              (card_id, source, source_id, sold_on, grade_tier, price_cents, title, captured_at) VALUES
              (42, 'ebay', 'r1', DATE '2026-08-11', 'PSA 10', 1100, 't', now()),
              (42, 'ebay', 'r2', DATE '2026-08-01', 'PSA 10', 1100, 't', now()),
              (42, 'ebay', 'r3', DATE '2026-07-13', 'PSA 10', 1100, 't', now()),
              (42, 'ebay', 'p1', DATE '2026-07-12', 'PSA 10', 1000, 't', now()),
              (42, 'ebay', 'p2', DATE '2026-07-01', 'PSA 10', 1000, 't', now()),
              (42, 'ebay', 'p3', DATE '2026-06-14', 'PSA 10', 1000, 't', now()),
              (42, 'ebay', 'old', DATE '2025-01-01', 'PSA 10', 1, 't', now()),
              (42, 'ebay', 'bgs', DATE '2026-08-10', 'BGS 10 Black', 9999, 't', now());
            """);

        var snapshot = await Reader().GetAsync(42);
        var psa10 = snapshot!.Tiers.Single(t => t.Tier == PriceTier.Psa10);

        var change = Assert.IsType<ChangeAvailable>(psa10.Change);
        Assert.Equal(3, change.RecentSales);
        Assert.Equal(3, change.PriorSales);
        Assert.Equal(0.10m, change.Fraction, 4);
    }
}
