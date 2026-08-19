using CardStock.Application.Cards;
using CardStock.Domain.Cards;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Cards;

public sealed class CardIdentityReader(IDbContextFactory<CardStockDbContext> dbFactory) : ICardIdentityReader
{
    public async Task<CardIdentity?> GetAsync(long cardId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var row = await db.ScraperCards.AsNoTracking()
            .Where(c => c.Id == cardId)
            .Join(db.ScraperSets.AsNoTracking(), c => c.SetId, s => s.Id,
                (c, s) => new { c.SetId, c.Name, SetName = s.Name, c.ImageHash, c.DelistedAt, c.NotACardAt })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        // D-122: the tagged species arm the subline's character links — dex order for a
        // deterministic tag-team sequence; empty for D-108's honest no-species verdicts.
        var species = await db.ScraperCardSpecies.AsNoTracking()
            .Where(cs => cs.CardId == cardId)
            .Join(db.ScraperSpecies.AsNoTracking(), cs => cs.SpeciesId, sp => sp.Id,
                (cs, sp) => new { sp.Id, sp.Name, sp.Slug })
            .OrderBy(sp => sp.Id)
            .ToListAsync(cancellationToken);

        var parsed = CardTitle.Parse(row.Name);
        return new CardIdentity(
            cardId, parsed.Title, parsed.CollectorNumber, SetSize: null,
            row.SetId, row.SetName,
            [.. species.Select(sp => new CardSpeciesRef(sp.Name, sp.Slug))],
            row.ImageHash, row.DelistedAt, row.NotACardAt);
    }
}
