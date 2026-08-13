using CardStock.Application.Cards;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Cards;

public sealed class CardSalesReader(IDbContextFactory<CardStockDbContext> dbFactory) : ICardSalesReader
{
    public async Task<IReadOnlyList<LedgerSale>> GetAsync(long cardId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var sales = await db.ScraperSales.AsNoTracking()
            .Where(s => s.CardId == cardId)
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
