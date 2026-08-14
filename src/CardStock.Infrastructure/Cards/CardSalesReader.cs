using CardStock.Application.Cards;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Cards;

public sealed class CardSalesReader(IDbContextFactory<CardStockDbContext> dbFactory) : ICardSalesReader
{
    public async Task<IReadOnlyList<LedgerSale>> GetAsync(long cardId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // D-091: newest N per grade bucket, lifetime — no time window. The correlated
        // SelectMany translates to a LATERAL join, so fast buckets are capped inside
        // the database while a rare bucket's complete history (≤ cap) always ships.
        var cardSales = db.ScraperSales.AsNoTracking().Where(s => s.CardId == cardId);

        var sales = await cardSales
            .Select(s => s.GradeTier)
            .Distinct()
            .SelectMany(tier => cardSales
                .Where(s => s.GradeTier == tier)
                .OrderByDescending(s => s.SoldOn)
                .ThenByDescending(s => s.Id)
                .Take(ICardSalesReader.BucketCap))
            .OrderByDescending(s => s.SoldOn)
            .ThenByDescending(s => s.Id)
            .Select(s => new LedgerSale(
                s.SoldOn,
                s.GradeTier,
                s.PriceCents,
                s.ListedPriceCents,
                s.Source,
                s.Title))
            .ToListAsync(cancellationToken);

        return sales;
    }
}
