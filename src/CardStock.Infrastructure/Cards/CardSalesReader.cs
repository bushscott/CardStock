using CardStock.Application.Cards;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Cards;

public sealed class CardSalesReader(
    IDbContextFactory<CardStockDbContext> dbFactory,
    TimeProvider time) : ICardSalesReader
{
    public async Task<IReadOnlyList<LedgerSale>> GetAsync(long cardId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // D-090: the ledger is a rolling twelve-month window, capped in the query so
        // older rows never leave the database. The cutoff date itself is included.
        var cutoff = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddMonths(-12);

        var sales = await db.ScraperSales.AsNoTracking()
            .Where(s => s.CardId == cardId && s.SoldOn >= cutoff)
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
