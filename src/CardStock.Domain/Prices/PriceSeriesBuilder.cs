namespace CardStock.Domain.Prices;

/// <summary>
/// Turns raw price_months rows into six resolved series.
///
/// This is where the change-only contract is honoured: a (tier, month) cell can
/// carry several rows because the current month revises between visits, so the
/// one with the greatest ObservedAt wins. It fires on roughly 0.17% of rows,
/// which is exactly why it must be encoded once rather than remembered.
/// </summary>
public static class PriceSeriesBuilder
{
    /// <summary>
    /// Descending by grade with Raw last, matching the Card page's fixed
    /// six-cell grid (Cardstock Card.dc.html:395). Callers rely on this order,
    /// so the list is always six long and always in it.
    /// </summary>
    public static IReadOnlyList<PriceTier> StripOrder { get; } =
    [
        PriceTier.Psa10,
        PriceTier.Grade9Half,
        PriceTier.Grade9,
        PriceTier.Grade8,
        PriceTier.Grade7,
        PriceTier.Ungraded,
    ];

    public static IReadOnlyList<TierSeries> Build(IEnumerable<PriceObservation> observations)
    {
        var byTier = observations
            .GroupBy(o => o.Tier)
            .ToDictionary(
                tier => tier.Key,
                tier => (IReadOnlyList<MonthlyPrice>)tier
                    .GroupBy(o => o.Month)
                    // MaxBy cannot tie: observed_at is part of the primary key,
                    // so one (card, tier, month) never holds two identical stamps.
                    .Select(month => month.MaxBy(o => o.ObservedAt)!)
                    .OrderBy(o => o.Month)
                    .Select(o => new MonthlyPrice(o.Month, o.PriceCents, o.ObservedAt))
                    .ToList());

        return [.. StripOrder.Select(tier =>
            new TierSeries(tier, byTier.GetValueOrDefault(tier, [])))];
    }
}
