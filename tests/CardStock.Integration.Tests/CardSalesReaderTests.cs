using CardStock.Application.Cards;
using CardStock.Infrastructure.Cards;
using CardStock.Infrastructure.Persistence;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The sales reader against real PostgreSQL. Rows are seeded with raw SQL because
/// CardStock's model cannot write the scraper's tables.
/// </summary>
public class CardSalesReaderTests : CardStockDatabaseTest
{
    private CardSalesReader Reader() => new(NewContextFactory());

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
    public async Task Rows_come_back_newest_first_with_a_stable_tiebreak()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var date1 = new DateOnly(2026, 8, 1);
        var date2 = new DateOnly(2026, 8, 2);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.sales (card_id, source, source_id, sold_on, grade_tier, price_cents, listed_price_cents, title, captured_at)
             VALUES ({42}, 'ebay', 'id1', {date1}, 'PSA 10', {10000}, {12000}, 'Test Sale 1', now()),
                    ({42}, 'tcgplayer', 'id2', {date2}, 'Ungraded', {5000}, null, 'Test Sale 2', now()),
                    ({42}, 'ebay', 'id3', {date2}, 'PSA 9', {8000}, {9000}, 'Test Sale 3', now());
             """);

        var sales = await Reader().GetAsync(42);

        Assert.Equal(3, sales.Count);
        Assert.Equal(date2, sales[0].SoldOn);
        Assert.Equal(date2, sales[1].SoldOn);
        Assert.Equal(date1, sales[2].SoldOn);

        // Verify stable tiebreak on ID for same date
        Assert.Equal("Test Sale 3", sales[0].Title); // id3
        Assert.Equal("Test Sale 2", sales[1].Title); // id2
    }

    [SkippableFact]
    public async Task Listed_price_is_null_when_the_source_had_none()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var date = new DateOnly(2026, 8, 1);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.sales (card_id, source, source_id, sold_on, grade_tier, price_cents, listed_price_cents, title, captured_at)
             VALUES ({42}, 'ebay', 'id1', {date}, 'PSA 10', {10000}, {12000}, 'With Listed Price', now()),
                    ({42}, 'tcgplayer', 'id2', {date}, 'Ungraded', {5000}, null, 'Without Listed Price', now());
             """);

        var sales = await Reader().GetAsync(42);

        var withListed = sales.First(s => s.Title == "With Listed Price");
        var withoutListed = sales.First(s => s.Title == "Without Listed Price");

        Assert.Equal(12000, withListed.ListedPriceCents);
        Assert.Null(withoutListed.ListedPriceCents);
    }

    [SkippableFact]
    public async Task A_hostile_title_round_trips_verbatim()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42);

        var date = new DateOnly(2026, 8, 1);
        var hostileTitle = "<script>alert(1)</script> 🔥 “PSA 10”";

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.sales (card_id, source, source_id, sold_on, grade_tier, price_cents, listed_price_cents, title, captured_at)
             VALUES ({42}, 'ebay', 'id1', {date}, 'PSA 10', {10000}, {12000}, {hostileTitle}, now());
             """);

        var sales = await Reader().GetAsync(42);

        Assert.Single(sales);
        Assert.Equal(hostileTitle, sales[0].Title);
    }
}
