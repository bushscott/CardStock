using CardStock.Application.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class SpeciesFiltersTests
{
    private static SpeciesTileDto Species(int id, string name, string[] types, short gen,
        string region, string status = "Ordinary", short stage = 1, string color = "Black",
        string[]? eggs = null, string? habitat = "Urban") => new(
        id, name, name.ToLowerInvariant(), 10, 1000, types, gen, region,
        status, stage, color, eggs ?? ["Field"], habitat);

    private static readonly SpeciesTileDto[] All =
    [
        Species(197, "Umbreon", ["Dark"], 2, "Johto"),
        Species(1, "Bulbasaur", ["Grass", "Poison"], 1, "Kanto", stage: 0, color: "Green",
            eggs: ["Monster", "Grass"], habitat: "Grassland"),
        Species(471, "Glaceon", ["Ice"], 4, "Sinnoh", habitat: null),
    ];

    [Fact]
    public void And_across_attributes_or_within_one()
    {
        var active = new Dictionary<string, IReadOnlySet<string>>
        {
            ["type"] = new HashSet<string> { "Grass", "Dark" },
            ["gen"] = new HashSet<string> { "1" },
        };
        Assert.Equal(["Bulbasaur"],
            All.Where(s => SpeciesFilters.Matches(s, active)).Select(s => s.Name).ToArray());
    }

    [Fact]
    public void A_multi_valued_attribute_matches_on_either_value()
    {
        var active = new Dictionary<string, IReadOnlySet<string>>
        {
            ["type"] = new HashSet<string> { "Poison" },
        };
        Assert.Single(All, s => SpeciesFilters.Matches(s, active));
    }

    [Fact]
    public void Species_without_a_habitat_match_no_habitat_value()
    {
        var active = new Dictionary<string, IReadOnlySet<string>>
        {
            ["habitat"] = new HashSet<string> { "Urban" },
        };
        Assert.DoesNotContain(All.Where(s => SpeciesFilters.Matches(s, active)),
            s => s.Name == "Glaceon");
    }

    [Fact]
    public void Stage_labels_read_Basic_then_Stage_N()
    {
        Assert.Equal("Basic", SpeciesFilters.Label("stage", "0"));
        Assert.Equal("Stage 2", SpeciesFilters.Label("stage", "2"));
        Assert.Equal("Gen 4", SpeciesFilters.Label("gen", "4"));
    }

    [Fact]
    public void The_eight_attributes_come_in_the_prototype_order()
    {
        Assert.Equal(["type", "gen", "region", "status", "stage", "color", "egg", "habitat"],
            SpeciesFilters.Attributes.Select(a => a.Key).ToArray());
    }

    [Fact]
    public void Region_options_order_by_generation_not_alphabet()
    {
        var options = SpeciesFilters.Options("region", All).Select(o => o.Value).ToArray();
        Assert.Equal(["Kanto", "Johto", "Sinnoh"], options);
    }
}
