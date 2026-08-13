using CardStock.Application.Prices;
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Prices;

/// <summary>
/// Two narrow reads, then Domain does the thinking. Nothing about change-only
/// semantics lives in SQL, deliberately: the rules are worth more under test
/// than under a query planner.
/// </summary>
public sealed class CardPriceReader(IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time) : ICardPriceReader
{
    public async Task<CardPriceSnapshot?> GetAsync(
        long cardId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var card = await db.ScraperCards.AsNoTracking()
            .Where(c => c.Id == cardId)
            .Select(c => new { c.Id, c.LastVisitedAt })
            .SingleOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        // Every row for the card. At ~113 rows on average and ~410 for a fully
        // populated one, loading the lot rides the primary key
        // (card_id, tier, month, observed_at) and is cheaper than being clever.
        // The crawler does the same thing (CardPageWriter.cs:61).
        // I2: price_cents = 0 means the scraper's own semantics for "no sales that month," not
        // "worthless" -- PokemonInvestBatch.Domain.Parsing.GradeMonotonicity.cs:23 ("Latest
        // non-zero price per tier; zero means 'no sales', not 'worthless'"). Rendering a stored
        // zero as a $0 price would fabricate a value the source never claimed to mean that;
        // excluding it here makes that (tier, month) cell a hole -- MissingMonth, same as a
        // month the site never published at all -- rather than a false floor. Rare in
        // production (three rows across one card, verified live 2026-08-13), but real.
        var prices = await db.ScraperPriceMonths.AsNoTracking()
            .Where(p => p.CardId == cardId && p.PriceCents > 0)
            .Select(p => new PriceObservation(p.Tier, p.Month, p.PriceCents, p.ObservedAt))
            .ToListAsync(cancellationToken);

        // Two windows' worth, no more. Rides the sales(card_id, sold_on) index,
        // and the source's ~30-rows-per-bucket ceiling keeps it small regardless.
        var since = today.AddDays(-SalesChange.WindowDays * 2);
        var sold = await db.ScraperSales.AsNoTracking()
            .Where(s => s.CardId == cardId && s.SoldOn >= since)
            .Select(s => new { s.GradeTier, s.SoldOn, s.PriceCents })
            .ToListAsync(cancellationToken);

        // Sales at grades with no price series -- 13 of the 19 labels -- have
        // nothing to change against and never reach Domain.
        var sales = sold
            .Select(s => (Tier: GradeTierMap.ToPriceTier(s.GradeTier), s.SoldOn, s.PriceCents))
            .Where(s => s.Tier is not null)
            .Select(s => new SaleObservation(s.Tier!.Value, s.SoldOn, s.PriceCents))
            .ToList();

        return CardPriceSnapshotBuilder.Build(card.Id, card.LastVisitedAt, prices, sales, today);
    }
}
