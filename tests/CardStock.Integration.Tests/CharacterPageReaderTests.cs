using CardStock.Application.Catalog;
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The character (species) page reader against real PostgreSQL. Proves what a
/// Domain-only test cannot: the junction (card_species) resolves membership,
/// the active-card rule excludes delisted links, and Year/TotalValueCents/
/// PricedPrintings come out of set_details and price_months correctly joined.
/// </summary>
public class CharacterPageReaderTests : CardStockDatabaseTest
{
    // Fixed clock: matches SetPageReaderTests' anchor month.
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private CharacterPageReader Reader() =>
        new(NewContextFactory(), new FixedClock(Now));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end) VALUES
              (133, 'Eevee', 'eevee', 1, 'Kanto', 'Brown', 'Urban', 0, 0, NULL, '#B98', '#DCA'),
              (197, 'Umbreon', 'umbreon', 2, 'Johto', 'Black', 'Urban', 0, 1, 133, '#2B2D42', '#5C6B9E');
            INSERT INTO public.species_types (species_id, slot, type) VALUES (197, 1, 'Dark');
            INSERT INTO public.species_egg_groups (species_id, egg_group) VALUES (197, 'Field');
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at) VALUES
              (7, 'es', 'Evolving Skies', now(), now()),
              (8, 'jp', 'Pokemon Japanese Promo', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era) VALUES
              (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH'),
              (8, 1, NULL, NULL, NULL, NULL);
            INSERT INTO public.cards
                (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap) VALUES
              (1, 7, 'Umbreon VMAX', 'https://x/1', now(), now(), false),
              (2, 8, 'Umbreon Promo', 'https://x/2', now(), now(), false);
            INSERT INTO public.cards
                (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap, delisted_at)
            VALUES (3, 7, 'Umbreon Gone', 'https://x/3', now(), now(), false, now());
            INSERT INTO public.card_species (card_id, species_id, method) VALUES
              (1, 197, 0), (2, 197, 0), (3, 197, 0);
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (1, 5, '2026-07-01', 45000, '2026-08-01T00:00:00Z');
            """);
    }

    [SkippableFact]
    public async Task The_species_resolves_by_slug_with_parent_name_and_active_roster()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();

        var snapshot = await Reader().GetAsync("umbreon");

        Assert.NotNull(snapshot);
        Assert.Equal("Umbreon", snapshot!.Name);
        Assert.Equal("Eevee", snapshot.EvolvesFrom);
        Assert.Equal(["Dark"], snapshot.Types);
        Assert.Equal(2, snapshot.Roster.Count);           // the delisted link is out
        Assert.Equal(2, snapshot.SetsCount);
        Assert.Equal(45_000, snapshot.TotalValueCents);
        Assert.Equal(1, snapshot.PricedPrintings);
    }

    [SkippableFact]
    public async Task Year_comes_from_matched_set_details_and_is_null_when_pending()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();

        var roster = (await Reader().GetAsync("umbreon"))!.Roster;
        Assert.Equal((short)2021, roster.Single(r => r.CardId == 1).Year);
        Assert.Null(roster.Single(r => r.CardId == 2).Year);
    }

    [SkippableFact]
    public async Task An_unknown_slug_returns_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        Assert.Null(await Reader().GetAsync("missingno"));
    }
}
