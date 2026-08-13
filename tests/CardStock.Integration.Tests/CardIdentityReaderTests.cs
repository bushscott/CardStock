using CardStock.Application.Cards;
using CardStock.Infrastructure.Cards;
using CardStock.Infrastructure.Persistence;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The queries, against real PostgreSQL. Domain already proves the rules; these
/// prove the rows arrive in the shape Domain expects.
/// </summary>
public class CardIdentityReaderTests : CardStockDatabaseTest
{
    private CardIdentityReader Reader() => new(NewContextFactory());

    /// <summary>
    /// Seeded with raw SQL because CardStock's model cannot write these tables --
    /// which is the guarantee ScraperReadTests exists to demonstrate.
    ///
    /// ExecuteSqlInterpolated, not ExecuteSqlRaw with an interpolated string: the
    /// former parameterises each hole, the latter concatenates and trips EF's
    /// raw-SQL analyzer, which TreatWarningsAsErrors turns into a build failure.
    /// </summary>
    private static async Task SeedCardAsync(
        CardStockDbContext db,
        long cardId,
        string? name = null,
        string? imageHash = null,
        DateTimeOffset? delistedAt = null,
        DateTimeOffset? notACardAt = null)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (1, 'base-set', 'Base Set', now(), now())
            ON CONFLICT (id) DO NOTHING;
            """);

        var cardName = name ?? "Test Card";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.cards (id, set_id, url, name, image_hash, first_seen_at, last_seen_at,
                                       any_bucket_at_cap, failure_streak, last_visited_at, delisted_at, not_a_card_at)
             VALUES ({cardId}, 1, '/game/pokemon-base-set/test-card', {cardName}, {imageHash},
                     now(), now(), false, 0, now(), {delistedAt}, {notACardAt});
             """);
    }

    [SkippableFact]
    public async Task An_unknown_id_returns_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        Assert.Null(await Reader().GetAsync(999_999));
    }

    [SkippableFact]
    public async Task The_name_parses_into_title_and_number()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, name: "Umbreon VMAX #215");

        var identity = await Reader().GetAsync(42);

        Assert.NotNull(identity);
        Assert.Equal("Umbreon VMAX", identity.Title);
        Assert.Equal("215", identity.CollectorNumber);
        Assert.Equal("Base Set", identity.SetName);
        Assert.Null(identity.SetSize);
    }

    [SkippableFact]
    public async Task A_delisted_card_carries_its_date_and_a_not_a_card_carries_its_flag()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        var delistedDate = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var notACardDate = new DateTimeOffset(2026, 8, 11, 14, 30, 0, TimeSpan.Zero);

        await SeedCardAsync(db, 42, name: "Delisted Card", delistedAt: delistedDate);
        await SeedCardAsync(db, 43, name: "Not A Card", notACardAt: notACardDate);

        var delisted = await Reader().GetAsync(42);
        var notACard = await Reader().GetAsync(43);

        Assert.NotNull(delisted);
        Assert.Equal(delistedDate, delisted.DelistedAt);
        Assert.Null(delisted.NotACardAt);

        Assert.NotNull(notACard);
        Assert.Null(notACard.DelistedAt);
        Assert.Equal(notACardDate, notACard.NotACardAt);
    }

    [SkippableFact]
    public async Task HasImage_maps_from_the_hash()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, name: "Card With Image", imageHash: "abc123hash");
        await SeedCardAsync(db, 43, name: "Card Without Image", imageHash: null);

        var withImage = await Reader().GetAsync(42);
        var withoutImage = await Reader().GetAsync(43);

        Assert.NotNull(withImage);
        Assert.Equal("abc123hash", withImage.ImageHash);

        Assert.NotNull(withoutImage);
        Assert.Null(withoutImage.ImageHash);
    }
}
