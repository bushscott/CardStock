using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>Browse set-mode ordering (D-110 spec §6): shelves are data-driven —
/// never a hard-coded era list — with two honest tail shelves. Direction (D-115):
/// date/name-ordered content mirrors when descending, but the unknowable tail
/// shelves stay pinned last and keep their stated internal order — a reversed wall
/// must not promote the "we don't know" buckets to the top.</summary>
public static class SetShelves
{
    public sealed record Shelf(string Title, IReadOnlyList<SetTileDto> Sets, bool Alphabetical = false);

    /// <summary>The years a shelf can honestly claim (D-123): known dates only — null when
    /// a shelf has none (the pending tails), a single year when the span collapses.</summary>
    public static string? YearSpan(IReadOnlyList<SetTileDto> sets)
    {
        var years = sets.Where(s => s.ReleasedOn is not null)
            .Select(s => s.ReleasedOn!.Value.Year).ToList();
        if (years.Count == 0)
        {
            return null;
        }

        var min = years.Min();
        var max = years.Max();
        return min == max ? $"{min}" : $"{min}–{max}";
    }

    public static IReadOnlyList<SetTileDto> Alphabetical(
        IReadOnlyList<SetTileDto> sets, bool descending = false) =>
        (descending
            ? sets.OrderByDescending(s => s.Name, StringComparer.OrdinalIgnoreCase)
            : sets.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)).ToList();

    public static IReadOnlyList<Shelf> ByReleaseDate(
        IReadOnlyList<SetTileDto> sets, bool descending = false)
    {
        var dated = sets.Where(s => s.ReleasedOn is not null);
        var ordered = (descending
                ? dated.OrderByDescending(s => s.ReleasedOn)
                : dated.OrderBy(s => s.ReleasedOn))
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var undated = sets.Where(s => s.ReleasedOn is null)
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shelves = new List<Shelf> { new("By release date", ordered) };
        if (undated.Count > 0)
        {
            shelves.Add(new("awaiting metadata", undated, Alphabetical: true));
        }

        return shelves;
    }

    public static IReadOnlyList<Shelf> ByEra(
        IReadOnlyList<SetTileDto> sets, bool descending = false)
    {
        // Eras with a known earliest date order by it (mirrored when descending); eras
        // whose dates are all unknown are pinned after them in both directions.
        var grouped = sets
            .Where(s => s.Era is not null)
            .GroupBy(s => s.Era!)
            .ToList();
        var datedGroups = grouped.Where(g => g.Any(s => s.ReleasedOn is not null));
        DateOnly EarliestKnown(IGrouping<string, SetTileDto> g) =>
            g.Where(s => s.ReleasedOn is not null).Min(s => s.ReleasedOn!.Value);
        var ordered = (descending
                ? datedGroups.OrderByDescending(EarliestKnown)
                : datedGroups.OrderBy(EarliestKnown))
            .Concat(grouped.Where(g => g.All(s => s.ReleasedOn is null))
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase));
        var shelves = ordered
            .Select(g => new Shelf(g.Key, OrderWithin(g, descending)))
            .ToList();

        var noEra = OrderWithin(
            sets.Where(s => s is { Era: null, MetadataStatus: "matched" }), descending);
        if (noEra.Count > 0)
        {
            shelves.Add(new("no era", noEra));
        }

        var pending = sets.Where(s => s.MetadataStatus != "matched")
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (pending.Count > 0)
        {
            shelves.Add(new("metadata pending", pending, Alphabetical: true));
        }

        return shelves;
    }

    // Within a shelf: dated sets mirror with the direction; undated sets sit last either
    // way, alphabetically — reversing never promotes an unknown.
    private static List<SetTileDto> OrderWithin(IEnumerable<SetTileDto> sets, bool descending)
    {
        var list = sets.ToList();
        var dated = list.Where(s => s.ReleasedOn is not null);
        return (descending
                ? dated.OrderByDescending(s => s.ReleasedOn)
                : dated.OrderBy(s => s.ReleasedOn))
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Concat(list.Where(s => s.ReleasedOn is null)
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
