using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>One Browse filter attribute: raw key (the chips' terminal voice),
/// display name (the popover menu), value extraction, option ordering.</summary>
public sealed record FilterAttribute(
    string Key,
    string DisplayName,
    Func<SpeciesTileDto, IEnumerable<string>> ValuesOf,
    Func<IReadOnlyList<SpeciesTileDto>, IEnumerable<string>> OrderedValues);

/// <summary>The 8-attribute algebra (browse.md §3.4–§3.6, spec §6): AND across
/// attributes, OR within one — a Grass/Poison species matches a type filter on
/// either value. Vocabularies derive from the complete species list, which is
/// the whole table, never from a filtered page.</summary>
public static class SpeciesFilters
{
    private static readonly string[] RegionOrder =
        ["Kanto", "Johto", "Hoenn", "Sinnoh", "Unova", "Kalos", "Alola", "Galar", "Paldea"];

    private static readonly string[] StatusOrder = ["Ordinary", "Legendary", "Mythical"];

    public static readonly IReadOnlyList<FilterAttribute> Attributes =
    [
        new("type", "Type", s => s.Types,
            all => all.SelectMany(s => s.Types).Distinct().Order()),
        new("gen", "Generation", s => [s.Generation.ToString()],
            all => all.Select(s => s.Generation).Distinct().Order().Select(g => g.ToString())),
        new("region", "Region", s => [s.Region],
            all => RegionOrder.Where(r => all.Any(s => s.Region == r))),
        new("status", "Status", s => [s.Status],
            all => StatusOrder.Where(v => all.Any(s => s.Status == v))),
        new("stage", "Evolution stage", s => [s.Stage.ToString()],
            all => all.Select(s => s.Stage).Distinct().Order().Select(v => v.ToString())),
        new("color", "Pokédex color", s => [s.Color],
            all => all.Select(s => s.Color).Distinct().Order()),
        new("egg", "Egg group", s => s.EggGroups,
            all => all.SelectMany(s => s.EggGroups).Distinct().Order()),
        new("habitat", "Habitat", s => s.Habitat is null ? [] : [s.Habitat],
            all => all.Where(s => s.Habitat is not null).Select(s => s.Habitat!).Distinct().Order()),
    ];

    public static bool Matches(
        SpeciesTileDto species, IReadOnlyDictionary<string, IReadOnlySet<string>> active) =>
        active.All(filter => Attributes.Single(a => a.Key == filter.Key)
            .ValuesOf(species).Any(filter.Value.Contains));

    public static IReadOnlyList<(string Value, string Label)> Options(
        string key, IReadOnlyList<SpeciesTileDto> all) =>
        Attributes.Single(a => a.Key == key).OrderedValues(all)
            .Select(v => (v, Label(key, v))).ToList();

    public static string Label(string key, string value) => key switch
    {
        "gen" => $"Gen {value}",
        "stage" => value == "0" ? "Basic" : $"Stage {value}",
        _ => value,
    };
}
