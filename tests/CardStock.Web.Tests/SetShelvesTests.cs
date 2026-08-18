using System.Globalization;
using CardStock.Application.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class SetShelvesTests
{
    private static SetTileDto Tile(long id, string name, string status = "matched",
        string? era = null, string? released = null) => new(
        id, name, 100, null, status, era,
        released is null ? null : DateOnly.Parse(released, CultureInfo.InvariantCulture));

    private static readonly SetTileDto[] Sets =
    [
        Tile(1, "Base Set", era: "WOTC", released: "1999-01-09"),
        Tile(2, "Evolving Skies", era: "SWSH", released: "2021-08-27"),
        Tile(3, "Brilliant Stars", era: "SWSH", released: "2022-02-25"),
        Tile(4, "POP Series 5", era: null, released: "2006-03-01"),          // matched, no era
        Tile(5, "Pokemon Japanese Promo", status: "pending"),
        Tile(6, "Aquapolis", status: "pending"),
    ];

    [Fact]
    public void Era_shelves_are_data_driven_chronological_with_the_two_tails()
    {
        var shelves = SetShelves.ByEra(Sets);
        Assert.Equal(["WOTC", "SWSH", "no era", "metadata pending"],
            shelves.Select(s => s.Title).ToArray());
        Assert.Equal([2L, 3L], shelves[1].Sets.Select(t => t.SetId).ToArray()); // date order
        Assert.Equal([4L], shelves[2].Sets.Select(t => t.SetId).ToArray());
        Assert.Equal(["Aquapolis", "Pokemon Japanese Promo"],
            shelves[3].Sets.Select(t => t.Name).ToArray());                    // alphabetical
    }

    [Fact]
    public void Empty_tails_do_not_render()
    {
        var shelves = SetShelves.ByEra([Tile(1, "Base Set", era: "WOTC", released: "1999-01-09")]);
        Assert.Equal(["WOTC"], shelves.Select(s => s.Title).ToArray());
    }

    [Fact]
    public void Release_order_puts_dated_first_then_the_labeled_pending_block()
    {
        var shelves = SetShelves.ByReleaseDate(Sets);
        Assert.Equal([1L, 4L, 2L, 3L], shelves[0].Sets.Select(t => t.SetId).ToArray());
        Assert.Equal("2 sets awaiting metadata — alphabetical", shelves[1].Title);
    }

    [Fact]
    public void Alphabetical_is_case_insensitive_and_total()
    {
        var ordered = SetShelves.Alphabetical(Sets);
        Assert.Equal(6, ordered.Count);
        Assert.Equal("Aquapolis", ordered[0].Name);
    }

    // D-115 direction rule: date/name-ordered content mirrors when descending; the
    // unknowable tail shelves stay pinned last and keep their stated internal order.
    [Fact]
    public void Descending_alphabetical_is_z_first()
    {
        var ordered = SetShelves.Alphabetical(Sets, descending: true);
        Assert.Equal("POP Series 5", ordered[0].Name);
        Assert.Equal("Aquapolis", ordered[^1].Name);
    }

    [Fact]
    public void Descending_release_order_is_newest_first_with_the_pending_block_still_last()
    {
        var shelves = SetShelves.ByReleaseDate(Sets, descending: true);
        Assert.Equal([3L, 2L, 4L, 1L], shelves[0].Sets.Select(t => t.SetId).ToArray());
        Assert.Equal("2 sets awaiting metadata — alphabetical", shelves[1].Title);
        Assert.Equal(["Aquapolis", "Pokemon Japanese Promo"],
            shelves[1].Sets.Select(t => t.Name).ToArray());
    }

    [Fact]
    public void Descending_era_mirrors_shelves_and_contents_but_the_tails_stay_pinned()
    {
        var shelves = SetShelves.ByEra(Sets, descending: true);
        Assert.Equal(["SWSH", "WOTC", "no era", "metadata pending"],
            shelves.Select(s => s.Title).ToArray());
        Assert.Equal([3L, 2L], shelves[0].Sets.Select(t => t.SetId).ToArray()); // newest first
        Assert.Equal(["Aquapolis", "Pokemon Japanese Promo"],
            shelves[3].Sets.Select(t => t.Name).ToArray());                    // still alphabetical
    }

    // Reversing must not promote the unknowns: an era whose dates are all unknown, and an
    // undated set inside a dated era, both stay last in BOTH directions.
    [Fact]
    public void Unknown_dates_stay_last_in_both_directions()
    {
        SetTileDto[] withUnknowns =
        [
            .. Sets,
            Tile(7, "Mystery Era Set", era: "XY", released: null),
            Tile(8, "Undated Swsh", era: "SWSH", released: null),
        ];

        foreach (var descending in new[] { false, true })
        {
            var shelves = SetShelves.ByEra(withUnknowns, descending);
            Assert.Equal("XY", shelves[2].Title);                       // all-unknown era: after dated eras
            var swsh = shelves.Single(s => s.Title == "SWSH");
            Assert.Equal(8L, swsh.Sets[^1].SetId);                      // undated set: last in its shelf
        }
    }
}
