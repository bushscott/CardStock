using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>Browse set-mode ordering (D-110 spec §6): shelves are data-driven —
/// never a hard-coded era list — with two honest tail shelves.</summary>
public static class SetShelves
{
    public sealed record Shelf(string Title, IReadOnlyList<SetTileDto> Sets);

    public static IReadOnlyList<SetTileDto> Alphabetical(IReadOnlyList<SetTileDto> sets) =>
        sets.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public static IReadOnlyList<Shelf> ByReleaseDate(IReadOnlyList<SetTileDto> sets)
    {
        var dated = sets.Where(s => s.ReleasedOn is not null)
            .OrderBy(s => s.ReleasedOn).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var undated = sets.Where(s => s.ReleasedOn is null)
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shelves = new List<Shelf> { new("By release date", dated) };
        if (undated.Count > 0)
        {
            shelves.Add(new($"{undated.Count} sets awaiting metadata — alphabetical", undated));
        }

        return shelves;
    }

    public static IReadOnlyList<Shelf> ByEra(IReadOnlyList<SetTileDto> sets)
    {
        var shelves = sets
            .Where(s => s.Era is not null)
            .GroupBy(s => s.Era!)
            .OrderBy(g => g.Min(s => s.ReleasedOn ?? DateOnly.MaxValue))
            .Select(g => new Shelf(g.Key, g
                .OrderBy(s => s.ReleasedOn ?? DateOnly.MaxValue)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()))
            .ToList();

        var noEra = sets.Where(s => s is { Era: null, MetadataStatus: "matched" })
            .OrderBy(s => s.ReleasedOn ?? DateOnly.MaxValue)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (noEra.Count > 0)
        {
            shelves.Add(new("no era", noEra));
        }

        var pending = sets.Where(s => s.MetadataStatus != "matched")
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (pending.Count > 0)
        {
            shelves.Add(new("metadata pending", pending));
        }

        return shelves;
    }
}
