namespace CardStock.Domain.Prices;

/// <summary>Whether a tier's newest published price is recent enough to show.</summary>
public static class PriceStaleness
{
    /// <summary>
    /// How far behind the current month a price may be and still render.
    ///
    /// One, and it was measured rather than chosen. Across a 500-card sample
    /// (1,802 series, 2026-08-11): 81.3% current month, 15.2% one behind, 3.5%
    /// two or more. The 15% are healthy -- early in a month the source has not
    /// yet posted an average for every tier -- so a current-month-only rule
    /// would have dashed 19% of series for no reason.
    /// </summary>
    public const int MaxMonthsBehind = 1;

    public static TierPrice Evaluate(TierSeries series, DateOnly currentMonth)
    {
        if (series.IsEmpty)
        {
            return new NoPriceSeries();
        }

        var newest = series.Points[^1];
        var behind = MonthsBetween(newest.Month, currentMonth);

        return behind > MaxMonthsBehind
            ? new PriceStale(newest.Month)
            : new PriceAvailable(newest.PriceCents, newest.Month, behind == 0);
    }

    /// <summary>Whole months from earlier to later; negative if later precedes earlier.</summary>
    internal static int MonthsBetween(DateOnly earlier, DateOnly later) =>
        ((later.Year - earlier.Year) * 12) + later.Month - earlier.Month;
}
