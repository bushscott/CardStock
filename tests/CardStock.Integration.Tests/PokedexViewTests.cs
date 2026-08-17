using CardStock.Infrastructure.Persistence.ScraperReadModels;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

public class PokedexViewTests : CardStockDatabaseTest
{
    [SkippableFact]
    public async Task The_five_pokedex_views_read_seeded_rows()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end)
            VALUES (133, 'Eevee', 'eevee', 1, 'Kanto', 'Brown', 'Urban', 0, 0,
                    NULL, '#8B5A2B', '#D2B48C');
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end)
            VALUES (197, 'Umbreon', 'umbreon', 2, 'Johto', 'Black', 'Urban', 0, 1,
                    133, '#2B2D42', '#5C6B9E');
            INSERT INTO public.species_types (species_id, slot, type) VALUES (197, 1, 'Dark');
            INSERT INTO public.species_egg_groups (species_id, egg_group) VALUES (197, 'Field');
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (7, 'pokemon-evolving-skies', 'Evolving Skies', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era)
            VALUES (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH');
            INSERT INTO public.cards (id, set_id, name, url, first_seen_at, last_seen_at, any_bucket_at_cap)
            VALUES (630001, 7, 'Umbreon VMAX (Alternate Art Secret)',
                    'https://www.pricecharting.com/game/x/y', now(), now(), false);
            INSERT INTO public.card_species (card_id, species_id, method)
            VALUES (630001, 197, 0);
            """);

        var species = await db.ScraperSpecies.SingleAsync(s => s.Id == 197);
        Assert.Equal("umbreon", species.Slug);
        Assert.Equal("Johto", species.Region);
        Assert.Equal(133, species.EvolvesFromSpeciesId);

        Assert.Equal("Dark",
            (await db.ScraperSpeciesTypes.SingleAsync(t => t.SpeciesId == 197)).Type);
        Assert.Equal("Field",
            (await db.ScraperSpeciesEggGroups.SingleAsync(g => g.SpeciesId == 197)).EggGroup);

        var link = await db.ScraperCardSpecies.SingleAsync();
        Assert.Equal(630001, link.CardId);

        var detail = await db.ScraperSetDetails.SingleAsync(d => d.SetId == 7);
        Assert.Equal("swsh7", detail.Code);
        Assert.Equal(new DateOnly(2021, 8, 27), detail.ReleasedOn);
        Assert.Equal("SWSH", detail.Era);
    }

    [SkippableFact]
    public async Task Writing_a_scraper_view_throws_before_reaching_the_database()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        db.Add(new ScraperCardSpecies { CardId = 1, SpeciesId = 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
