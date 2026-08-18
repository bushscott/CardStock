using CardStock.Application.Catalog;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>Both Browse walls. Counts and joins are cheap GROUP BYs; anything
/// touching latest prices rides the aggregate cache's dictionary in memory.</summary>
public sealed class BrowseReader(
    IDbContextFactory<CardStockDbContext> dbFactory, ICatalogAggregates aggregates) : IBrowseReader
{
    public async Task<IReadOnlyList<SetTile>> GetSetsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latest = await aggregates.LatestPsa10ByCardAsync(ct);

        var sets = await db.ScraperSets.AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.Name,
                Detail = db.ScraperSetDetails.SingleOrDefault(d => d.SetId == s.Id),
            })
            .ToListAsync(ct);

        var activeCards = await db.ScraperCards.AsNoTracking()
            .Where(c => c.DelistedAt == null && c.NotACardAt == null)
            .Select(c => new { c.Id, c.SetId })
            .ToListAsync(ct);

        var bySet = activeCards.ToLookup(c => c.SetId);

        return sets
            .Select(s =>
            {
                var members = bySet[s.Id].ToList();
                var top = members
                    .Where(c => latest.ContainsKey(c.Id))
                    .OrderByDescending(c => latest[c.Id])
                    .ThenBy(c => c.Id)
                    .Select(c => (long?)c.Id)
                    .FirstOrDefault();
                return new SetTile(
                    s.Id, s.Name, members.Count, top,
                    s.Detail is { MatchStatus: 0 } ? "matched" : "pending",
                    s.Detail?.Era, s.Detail?.ReleasedOn);
            })
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<SpeciesTile>> GetSpeciesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latest = await aggregates.LatestPsa10ByCardAsync(ct);

        var species = await db.ScraperSpecies.AsNoTracking().ToListAsync(ct);
        var types = (await db.ScraperSpeciesTypes.AsNoTracking()
                .OrderBy(t => t.Slot).ToListAsync(ct))
            .ToLookup(t => t.SpeciesId, t => t.Type);
        var eggGroups = (await db.ScraperSpeciesEggGroups.AsNoTracking().ToListAsync(ct))
            .ToLookup(g => g.SpeciesId, g => g.EggGroup);

        var links = await db.ScraperCardSpecies.AsNoTracking()
            .Join(db.ScraperCards.AsNoTracking()
                    .Where(c => c.DelistedAt == null && c.NotACardAt == null),
                link => link.CardId, c => c.Id,
                (link, c) => new { link.SpeciesId, c.Id })
            .ToListAsync(ct);
        var bySpecies = links.ToLookup(l => l.SpeciesId, l => l.Id);

        return species
            .Select(s =>
            {
                var cards = bySpecies[s.Id].ToList();
                return new SpeciesTile(
                    s.Id, s.Name, s.Slug,
                    cards.Count,
                    cards.Sum(id => (long)latest.GetValueOrDefault(id)),
                    types[s.Id].ToList(), s.Generation, s.Region,
                    s.Status switch { 1 => "Legendary", 2 => "Mythical", _ => "Ordinary" },
                    s.Stage, s.Color, eggGroups[s.Id].ToList(), s.Habitat);
            })
            .OrderByDescending(s => s.TotalValueCents)
            .ThenBy(s => s.SpeciesId)
            .ToList();
    }
}
