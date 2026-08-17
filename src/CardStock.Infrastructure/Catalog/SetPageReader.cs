using CardStock.Application.Catalog;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>Row shape for the DISTINCT ON latest-price query. Shared by every
/// catalog reader (Tasks 5, 13, 16).</summary>
public sealed record LatestPsa10Row(long CardId, int PriceCents);

/// <summary>
/// One set page in bounded queries. Latest-per-key resolves in SQL here — a
/// named deviation from the one-card readers' load-everything shape: 2,531
/// cards × ~113 rows is not the Card page's situation (D-110 spec §3.1).
/// </summary>
public sealed class SetPageReader(
    IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time) : ISetPageReader
{
    public async Task<SetPageSnapshot?> GetAsync(long setId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var header = await db.ScraperSets.AsNoTracking()
            .Where(s => s.Id == setId)
            .Select(s => new
            {
                s.Id,
                s.Name,
                Detail = db.ScraperSetDetails.SingleOrDefault(d => d.SetId == s.Id),
            })
            .SingleOrDefaultAsync(ct);
        if (header is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var cards = await db.ScraperCards.AsNoTracking()
            .Where(c => c.SetId == setId && c.DelistedAt == null && c.NotACardAt == null)
            .Select(c => new { c.Id, c.Name, HasImage = c.ImageHash != null })
            .ToListAsync(ct);
        var ids = cards.Select(c => c.Id).ToArray();

        var latest = ids.Length == 0
            ? []
            : await db.Database.SqlQuery<LatestPsa10Row>($"""
                SELECT DISTINCT ON (card_id) card_id, price_cents
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                ORDER BY card_id, month DESC, observed_at DESC
                """).ToListAsync(ct);
        var latestByCard = latest.ToDictionary(r => r.CardId, r => r.PriceCents);

        // ROC anchors: the two months the rule reads, latest-per-cell (D-078).
        var m1 = currentMonth.AddMonths(-1);
        var m4 = currentMonth.AddMonths(-4);
        var anchorRows = ids.Length == 0
            ? []
            : await db.Database.SqlQuery<AnchorRow>($"""
                SELECT DISTINCT ON (card_id, month) card_id, month, price_cents
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                  AND month IN ({m1}, {m4})
                ORDER BY card_id, month, observed_at DESC
                """).ToListAsync(ct);
        var anchorsByCard = anchorRows
            .GroupBy(r => r.CardId)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyDictionary<DateOnly, int>)g.ToDictionary(r => r.Month, r => r.PriceCents));

        var censusRows = await db.ScraperPopulations.AsNoTracking()
            .Where(p => p.Grader == "psa" && p.Grade == 10 && ids.Contains(p.CardId))
            .Select(p => new { p.CardId, p.ObservedAt, p.Population })
            .ToListAsync(ct);
        var censusByCard = censusRows
            .GroupBy(p => p.CardId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PopulationObservation>)g
                .Select(p => new PopulationObservation(
                    DateOnly.FromDateTime(p.ObservedAt.UtcDateTime), p.Population))
                .ToList());

        var salesSince = today.AddDays(-SalesChange.WindowDays);
        var salesCounts = await db.ScraperSales.AsNoTracking()
            .Where(s => ids.Contains(s.CardId) && s.SoldOn >= salesSince)
            .GroupBy(s => s.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CardId, g => g.Count, ct);

        var firstSale = await db.ScraperSales.AsNoTracking()
            .Where(s => ids.Contains(s.CardId))
            .Select(s => (DateOnly?)s.SoldOn)
            .MinAsync(ct);

        var roster = cards
            .Select(c => new RosterCard(
                c.Id, c.Name, c.HasImage,
                latestByCard.TryGetValue(c.Id, out var cents) ? cents : null,
                anchorsByCard.TryGetValue(c.Id, out var anchors)
                    ? RosterMath.Roc3M(anchors, currentMonth)
                    : null,
                PopulationDelta.Evaluate(
                    censusByCard.TryGetValue(c.Id, out var census) ? census : [], today),
                salesCounts.GetValueOrDefault(c.Id)))
            .ToList();

        var detail = header.Detail;
        return new SetPageSnapshot(
            header.Id, header.Name,
            detail is { MatchStatus: 0 } ? "matched" : "pending",
            detail?.Code, detail?.Era,
            cards.Count, firstSale, roster);
    }

    private sealed record AnchorRow(long CardId, DateOnly Month, int PriceCents);
}
