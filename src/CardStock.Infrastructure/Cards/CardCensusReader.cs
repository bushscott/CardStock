using CardStock.Application.Cards;
using CardStock.Domain.Census;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Cards;

public sealed class CardCensusReader(IDbContextFactory<CardStockDbContext> dbFactory) : ICardCensusReader
{
    public async Task<CardCensus> GetAsync(long cardId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.ScraperPopulations.AsNoTracking()
            .Where(p => p.CardId == cardId)
            .Select(p => new CensusObservation(p.Grader, p.Grade, p.Population, p.ObservedAt))
            .ToListAsync(cancellationToken);

        // The full history rides the domain record: latest-per-cell, the
        // qualifying-observation count, and the census metrics all derive from
        // it in Domain (D-093) — the reader stays a dumb fetch.
        return CardCensus.From(rows);
    }
}
