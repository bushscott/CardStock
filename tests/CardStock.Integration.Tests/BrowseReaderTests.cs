using CardStock.Application.Catalog;
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The Browse reader against real PostgreSQL. Proves what a unit test cannot:
/// both active-card walls (set counts and the card_species junction join)
/// exclude not-a-card and delisted rows, TopCardId resolves against the
/// aggregate cache's dictionary, and the species list carries the corpus-wide
/// ORDER BY TotalValueCents DESC the Browse caption promises (browse.md §6.3).
/// </summary>
public class BrowseReaderTests : CardStockDatabaseTest
{
    private sealed class FixedAggregates(Dictionary<long, int> latest) : ICatalogAggregates
    {
        public Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<long, int>>(latest);
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at) VALUES
              (7, 'es', 'Evolving Skies', now(), now()),
              (8, 'jp', 'Pokemon Japanese Promo', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era) VALUES
              (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH'),
              (8, 1, NULL, NULL, NULL, NULL);
            INSERT INTO public.cards
                (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap) VALUES
              (1, 7, 'Umbreon VMAX', 'https://x/1', now(), now(), false),
              (2, 7, 'Glaceon V', 'https://x/2', now(), now(), false),
              (3, 8, 'Promo', 'https://x/3', now(), now(), false);
            INSERT INTO public.cards
                (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap, not_a_card_at)
            VALUES (4, 7, 'Not A Card', 'https://x/4', now(), now(), false, now());
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end) VALUES
              (197, 'Umbreon', 'umbreon', 2, 'Johto', 'Black', 'Urban', 0, 1, NULL, '#2B2D42', '#5C6B9E'),
              (471, 'Glaceon', 'glaceon', 4, 'Sinnoh', 'Blue', NULL, 0, 1, NULL, '#8AB', '#DEF');
            INSERT INTO public.species_types (species_id, slot, type) VALUES
              (197, 1, 'Dark'), (471, 1, 'Ice');
            INSERT INTO public.species_egg_groups (species_id, egg_group) VALUES
              (197, 'Field'), (471, 'Field');
            INSERT INTO public.card_species (card_id, species_id, method) VALUES
              (1, 197, 0), (2, 471, 0), (3, 197, 0);
            """);
    }

    [SkippableFact]
    public async Task Set_tiles_carry_active_counts_top_card_and_metadata()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();
        var reader = new BrowseReader(NewContextFactory(),
            new FixedAggregates(new() { [1] = 45000, [2] = 5000 }));

        var sets = await reader.GetSetsAsync();

        var es = sets.Single(s => s.SetId == 7);
        Assert.Equal(2, es.Cards);                 // not_a_card excluded
        Assert.Equal(1, es.TopCardId);             // 45000 beats 5000
        Assert.Equal("SWSH", es.Era);
        Assert.Equal(new DateOnly(2021, 8, 27), es.ReleasedOn);
        var jp = sets.Single(s => s.SetId == 8);
        Assert.Equal("pending", jp.MetadataStatus);
        Assert.Null(jp.TopCardId);                 // its card has no PSA 10 price
    }

    [SkippableFact]
    public async Task Species_tiles_aggregate_the_junction_and_order_by_total_value_desc()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await SeedAsync();
        var reader = new BrowseReader(NewContextFactory(),
            new FixedAggregates(new() { [1] = 45000, [2] = 99000 }));

        var species = await reader.GetSpeciesAsync();

        Assert.Equal([471, 197], species.Select(s => s.SpeciesId).ToArray()); // 99000 > 45000
        var umbreon = species.Single(s => s.SpeciesId == 197);
        Assert.Equal(2, umbreon.Printings);        // cards 1 and 3
        Assert.Equal(45_000, umbreon.TotalValueCents);
        Assert.Equal(["Dark"], umbreon.Types);
        Assert.Equal("Ordinary", umbreon.Status);
        Assert.Null(species.Single(s => s.SpeciesId == 471).Habitat);
    }
}
