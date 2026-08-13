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
                (c, s) => new { c.Name, SetName = s.Name, c.ImageHash, c.DelistedAt, c.NotACardAt })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var parsed = CardTitle.Parse(row.Name);
        return new CardIdentity(
            cardId, parsed.Title, parsed.CollectorNumber, SetSize: null,
            row.SetName, row.ImageHash, row.DelistedAt, row.NotACardAt);
    }
}
