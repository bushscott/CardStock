using CardStock.Application.Catalog;
using CardStock.Domain.Census;
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The set page reader against real PostgreSQL. Domain proves the per-card
/// aggregation rules (PopulationDelta, RosterMath); this proves the rows
/// arrive in the shape Domain expects -- latest-per-key resolved in SQL,
/// the D-078 revision case, and the delisted-card exclusion.
/// </summary>
public class SetPageReaderTests : CardStockDatabaseTest
{
    // Fixed clock: current month = Aug 2026, so ROC anchors are Jul (m−1) and Apr (m−4).
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private SetPageReader Reader() =>
        new(NewContextFactory(), new FixedClock(Now));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (7, 'pokemon-evolving-skies', 'Evolving Skies', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era)
            VALUES (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH');
            INSERT INTO public.cards (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap) VALUES
              (1, 7, 'Umbreon VMAX', 'https://x/1', now(), now(), false),
              (2, 7, 'Glaceon V',    'https://x/2', now(), now(), false),
              (3, 7, 'Leafeon V',    'https://x/3', now(), now(), false);
            INSERT INTO public.cards
              (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap, delisted_at)
            VALUES (4, 7, 'Ghost Card', 'https://x/4', now(), now(), false, now());

            -- Card 1: Jul revised (12000 then 12500 — newer observed_at must win),
            -- Apr anchor present, plus a current-month row that must not matter.
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (1, 5, '2026-07-01', 12000, '2026-08-01T00:00:00Z'),
              (1, 5, '2026-07-01', 12500, '2026-08-10T00:00:00Z'),
              (1, 5, '2026-04-01', 10000, '2026-05-02T00:00:00Z'),
              (1, 5, '2026-08-01', 13000, '2026-08-16T00:00:00Z'),
              (2, 5, '2026-07-01',  5000, '2026-08-01T00:00:00Z'),
              (2, 5, '2026-03-01',  4000, '2026-04-02T00:00:00Z');
            -- Card 3 has no PSA 10 rows at all.

            -- Census: card 1 mature (first obs 1 Jun, grew 100 → 110);
            -- card 2 young (first obs 30 Jul).
            INSERT INTO public.populations (card_id, grader, grade, population, observed_at) VALUES
              (1, 'psa', 10, 100, '2026-06-01T00:00:00Z'),
              (1, 'psa', 10, 110, '2026-08-10T00:00:00Z'),
              (1, 'psa',  9,  40, '2026-06-01T00:00:00Z'),
              (2, 'psa', 10,  20, '2026-07-30T00:00:00Z');

            INSERT INTO public.sales
              (card_id, source, source_id, grade_tier, title, sold_on, price_cents, captured_at) VALUES
              (1, 'ebay', 'a1', 'PSA 10', 'Umbreon VMAX', '2026-08-05', 45000, now()),
              (1, 'ebay', 'a2', 'Ungraded', 'Umbreon VMAX', '2026-07-25', 9000, now()),
              (1, 'ebay', 'a3', 'PSA 10', 'Umbreon VMAX', '2026-06-01', 44000, now()),
              (2, 'ebay', 'b1', 'PSA 10', 'Glaceon V', '2021-12-15', 8000, now());
            """);
    }

    [SkippableFact]
    public async Task The_snapshot_carries_header_facts_and_active_cards_only()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();

        var snapshot = await Reader().GetAsync(7);

        Assert.NotNull(snapshot);
        Assert.Equal("Evolving Skies", snapshot!.Name);
        Assert.Equal("matched", snapshot.MetadataStatus);
        Assert.Equal("swsh7", snapshot.Code);
        Assert.Equal("SWSH", snapshot.Era);
        Assert.Equal(3, snapshot.CardsTracked);            // the delisted card is out
        Assert.Equal(new DateOnly(2021, 12, 15), snapshot.FirstSale);
        Assert.Equal(3, snapshot.Roster.Count);
        Assert.DoesNotContain(snapshot.Roster, r => r.Name == "Ghost Card");
    }

    [SkippableFact]
    public async Task Latest_price_takes_the_revised_row_and_roc_uses_closed_month_anchors()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();

        var roster = (await Reader().GetAsync(7))!.Roster;
        var umbreon = roster.Single(r => r.CardId == 1);

        Assert.Equal(13000, umbreon.PriceCents);           // current month IS the latest cell
        Assert.Equal(0.25m, umbreon.Roc3M);                // 12500 (revised Jul) vs 10000 (Apr)
        Assert.Equal(PopulationDeltaState.Available, umbreon.Pop.State);
        Assert.Equal(0.10m, umbreon.Pop.Fraction);
        Assert.Equal(2, umbreon.Sales30d);                 // Aug 5 + Jul 25, all grade labels
    }

    [SkippableFact]
    public async Task Gaps_and_absences_surface_as_nulls_and_states_never_fabrications()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();

        var roster = (await Reader().GetAsync(7))!.Roster;
        var glaceon = roster.Single(r => r.CardId == 2);
        var leafeon = roster.Single(r => r.CardId == 3);

        Assert.Equal(5000, glaceon.PriceCents);
        Assert.Null(glaceon.Roc3M);                        // Apr cell absent — a real gap
        Assert.Equal(PopulationDeltaState.Pending, glaceon.Pop.State);
        Assert.Equal(new DateOnly(2026, 7, 30), glaceon.Pop.FirstObservedOn);

        Assert.Null(leafeon.PriceCents);                   // no PSA 10 series at all
        Assert.Null(leafeon.Roc3M);
        Assert.Equal(PopulationDeltaState.None, leafeon.Pop.State);
        Assert.Equal(0, leafeon.Sales30d);
    }

    [SkippableFact]
    public async Task An_unknown_set_returns_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        Assert.Null(await Reader().GetAsync(999));
    }
}
