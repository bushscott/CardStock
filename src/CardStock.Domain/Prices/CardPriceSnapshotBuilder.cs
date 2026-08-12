namespace CardStock.Domain.Prices;

/// <summary>
/// Joins resolved prices and sales into the shape callers receive. Pure: the
/// current date arrives as an argument, so "what month is it" is a test input
/// rather than ambient state.
/// </summary>
public static class CardPriceSnapshotBuilder
{
    public static CardPriceSnapshot Build(
        long cardId,
        DateTimeOffset? lastVisitedAt,
        IEnumerable<PriceObservation> prices,
        IEnumerable<SaleObservation> sales,
        DateOnly today)
    {
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var salesByTier = sales
            .GroupBy(s => s.Tier)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleObservation>)g.ToList());

        return new CardPriceSnapshot(cardId, lastVisitedAt, [
            .. PriceSeriesBuilder.Build(prices).Select(series => new TierSnapshot(
                series.Tier,
                series,
                PriceStaleness.Evaluate(series, currentMonth),
                SalesChange.Evaluate(salesByTier.GetValueOrDefault(series.Tier, []), today))),
        ]);
    }
}
