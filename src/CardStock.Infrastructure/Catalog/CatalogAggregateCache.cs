using CardStock.Application.Catalog;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>Single-flight, TTL-bound cache of the latest-PSA-10 dictionary.
/// Registered as a singleton; loses nothing on restart but a warm-up.</summary>
public sealed class CatalogAggregateCache(
    IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time, TimeSpan ttl)
    : ICatalogAggregates
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Task<IReadOnlyDictionary<long, int>>? _current;
    private DateTimeOffset _computedAt;

    public async Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(
        CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        var current = _current;
        if (current is { IsFaulted: false } && now - _computedAt < ttl)
        {
            return await current.WaitAsync(ct);
        }

        await _gate.WaitAsync(ct);
        try
        {
            now = time.GetUtcNow();
            if (_current is { IsFaulted: false } && now - _computedAt < ttl)
            {
                return await _current.WaitAsync(ct);
            }

            // Not the caller's token: one caller's cancellation must not poison
            // the shared computation every other request is waiting on.
            _current = ComputeAsync(CancellationToken.None);
            _computedAt = now;
        }
        finally
        {
            _gate.Release();
        }

        return await _current.WaitAsync(ct);
    }

    private async Task<IReadOnlyDictionary<long, int>> ComputeAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQuery<LatestPsa10Row>($"""
            SELECT DISTINCT ON (card_id) card_id, price_cents
            FROM public.price_months
            WHERE tier = 5 AND price_cents > 0
            ORDER BY card_id, month DESC, observed_at DESC
            """).ToListAsync(ct);
        return rows.ToDictionary(r => r.CardId, r => r.PriceCents);
    }
}
