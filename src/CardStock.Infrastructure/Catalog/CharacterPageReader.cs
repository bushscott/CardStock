using CardStock.Application.Catalog;
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>One species page. Same bounded-SQL shape as SetPageReader; the
/// junction (card_species) supplies membership, set_details supplies Year.</summary>
public sealed class CharacterPageReader(
    IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time) : ICharacterPageReader
{
    public async Task<CharacterPageSnapshot?> GetAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var species = await db.ScraperSpecies.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Slug == slug, ct);
        if (species is null)
        {
            return null;
        }

        var evolvesFrom = species.EvolvesFromSpeciesId is { } parentId
            ? await db.ScraperSpecies.AsNoTracking()
                .Where(s => s.Id == parentId).Select(s => s.Name).SingleOrDefaultAsync(ct)
            : null;
        var types = await db.ScraperSpeciesTypes.AsNoTracking()
            .Where(t => t.SpeciesId == species.Id).OrderBy(t => t.Slot)
            .Select(t => t.Type).ToListAsync(ct);
        var eggGroups = await db.ScraperSpeciesEggGroups.AsNoTracking()
            .Where(g => g.SpeciesId == species.Id).OrderBy(g => g.EggGroup)
            .Select(g => g.EggGroup).ToListAsync(ct);

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var cards = await db.ScraperCardSpecies.AsNoTracking()
            .Where(link => link.SpeciesId == species.Id)
            .Join(db.ScraperCards.AsNoTracking()
                    .Where(c => c.DelistedAt == null && c.NotACardAt == null),
                link => link.CardId, c => c.Id,
                (_, c) => new { c.Id, c.Name, HasImage = c.ImageHash != null, c.SetId })
            .ToListAsync(ct);
        var ids = cards.Select(c => c.Id).ToArray();

        var setIds = cards.Select(c => c.SetId).Distinct().ToArray();
        var setNames = await db.ScraperSets.AsNoTracking()
            .Where(s => setIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var years = await db.ScraperSetDetails.AsNoTracking()
            .Where(d => setIds.Contains(d.SetId) && d.ReleasedOn != null)
            .ToDictionaryAsync(d => d.SetId, d => (short)d.ReleasedOn!.Value.Year, ct);

        var latestByCard = ids.Length == 0
            ? []
            : (await db.Database.SqlQuery<LatestPsa10Row>($"""
                SELECT DISTINCT ON (card_id) card_id, price_cents
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                ORDER BY card_id, month DESC, observed_at DESC
                """).ToListAsync(ct)).ToDictionary(r => r.CardId, r => r.PriceCents);

        var m1 = currentMonth.AddMonths(-1);
        var m4 = currentMonth.AddMonths(-4);
        var anchorsByCard = ids.Length == 0
            ? []
            : (await db.Database.SqlQuery<AnchorRow>($"""
                SELECT DISTINCT ON (card_id, month) card_id, month, price_cents
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                  AND month IN ({m1}, {m4})
                ORDER BY card_id, month, observed_at DESC
                """).ToListAsync(ct))
            .GroupBy(r => r.CardId)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyDictionary<DateOnly, int>)g.ToDictionary(r => r.Month, r => r.PriceCents));

        var salesSince = today.AddDays(-SalesChange.WindowDays);
        var salesCounts = await db.ScraperSales.AsNoTracking()
            .Where(s => ids.Contains(s.CardId) && s.SoldOn >= salesSince)
            .GroupBy(s => s.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CardId, g => g.Count, ct);

        var roster = cards
            .Select(c => new CharacterRosterCard(
                c.Id, c.Name, c.HasImage, c.SetId, setNames[c.SetId],
                years.TryGetValue(c.SetId, out var year) ? year : null,
                latestByCard.TryGetValue(c.Id, out var cents) ? cents : null,
                anchorsByCard.TryGetValue(c.Id, out var anchors)
                    ? RosterMath.Roc3M(anchors, currentMonth)
                    : null,
                salesCounts.GetValueOrDefault(c.Id)))
            .ToList();

        return new CharacterPageSnapshot(
            species.Id, species.Name, species.Slug, species.GradientStart, species.GradientEnd,
            species.Generation, species.Region, species.Color, species.Habitat,
            species.Status, species.Stage, evolvesFrom, types, eggGroups,
            setIds.Length, roster.Sum(r => (long)(r.PriceCents ?? 0)),
            roster.Count(r => r.PriceCents is not null), roster);
    }

    private sealed record AnchorRow(long CardId, DateOnly Month, int PriceCents);
}
