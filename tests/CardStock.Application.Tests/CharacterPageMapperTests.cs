using CardStock.Application.Catalog;
using Xunit;

namespace CardStock.Application.Tests;

public class CharacterPageMapperTests
{
    private static CharacterPageSnapshot Umbreon() => new(
        197, "Umbreon", "umbreon", "#2B2D42", "#5C6B9E",
        Generation: 2, Region: "Johto", Color: "Black", Habitat: "Urban",
        Status: 0, Stage: 1, EvolvesFrom: "Eevee",
        Types: ["Dark"], EggGroups: ["Field"],
        SetsCount: 6, TotalValueCents: 9_640_000, PricedPrintings: 7,
        Roster: [new CharacterRosterCard(1, "Umbreon VMAX", true, 7, "Evolving Skies",
            2021, 45_000, 0.25m, 2)]);

    [Fact]
    public void Six_chip_kinds_in_order_with_the_ruled_tooltips()
    {
        var chips = CatalogMappers.ToDto(Umbreon()).Chips;
        Assert.Equal(
            ["Dark", "Gen 2", "Stage 1", "Black", "Field egg group", "Urban habitat"],
            chips.Select(c => c.Label).ToArray());
        Assert.Equal("First appeared in Generation 2 (Johto)", chips[1].Tooltip);
        Assert.Equal("Evolution stage — evolves from Eevee", chips[2].Tooltip);
    }

    [Fact]
    public void A_null_habitat_omits_the_chip_entirely()
    {
        var gen4 = Umbreon() with { Habitat = null };
        Assert.DoesNotContain(CatalogMappers.ToDto(gen4).Chips, c => c.Label.EndsWith("habitat"));
    }

    [Fact]
    public void Stage_zero_reads_Basic_with_no_parent_clause()
    {
        var basic = Umbreon() with { Stage = 0, EvolvesFrom = null };
        var stage = CatalogMappers.ToDto(basic).Chips.Single(c => c.Label is "Basic");
        Assert.Equal("Evolution stage", stage.Tooltip);
    }

    [Fact]
    public void Dual_types_get_two_chips()
    {
        var dual = Umbreon() with { Types = ["Grass", "Poison"] };
        var labels = CatalogMappers.ToDto(dual).Chips.Select(c => c.Label).ToList();
        Assert.Equal("Grass", labels[0]);
        Assert.Equal("Poison", labels[1]);
    }

    [Fact]
    public void Printings_is_the_roster_count()
    {
        Assert.Equal(1, CatalogMappers.ToDto(Umbreon()).Printings);
    }
}
