namespace CardStock.Domain.Prices;

/// <summary>Projects a series onto a fixed run of months, one slot each.</summary>
public static class PriceWindow
{
    /// <param name="series">The tier's resolved history.</param>
    /// <param name="endMonth">The newest month in the window, inclusive.</param>
    /// <param name="months">How many months, counting back from endMonth.</param>
    public static IReadOnlyList<PriceSlot> Of(TierSeries series, DateOnly endMonth, int months)
    {
        var points = series.Points.ToDictionary(p => p.Month);

        return [.. Enumerable.Range(0, months)
            .Select(offset => endMonth.AddMonths(offset - months + 1))
            .Select(month => Slot(series, points, month))];
    }

    private static PriceSlot Slot(
        TierSeries series, IReadOnlyDictionary<DateOnly, MonthlyPrice> points, DateOnly month)
    {
        if (points.TryGetValue(month, out var point))
        {
            return new ObservedPrice(month, point.PriceCents, point.ObservedAt);
        }

        // No first/last month means no series at all, so nothing can be "inside" it.
        return series.FirstMonth is { } first && series.LastMonth is { } last
               && month >= first && month <= last
            ? new MissingMonth(month)
            : new OutsideSeries(month);
    }
}
