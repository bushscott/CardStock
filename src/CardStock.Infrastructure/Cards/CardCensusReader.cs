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

        var latestPerCell = rows
            .GroupBy(r => (r.Grader, r.Grade))
            .Select(g => g.OrderByDescending(r => r.ObservedAt).First())
            .ToList();

        var instants = rows.Select(r => r.ObservedAt).Distinct().ToList();

        return CardCensus.From(latestPerCell, instants);
    }
}
