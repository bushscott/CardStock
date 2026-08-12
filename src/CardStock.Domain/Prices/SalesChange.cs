namespace CardStock.Domain.Prices;

/// <summary>
/// The thirty-day movement: mean sale price over the last 30 days against the
/// mean over the 30 before that.
///
/// Both windows are fixed and never widen. Today they return a handful of rows;
/// in a year they return a full window; the code is identical either way, which
/// is the point -- there is no early-days special case to unpick later.
/// </summary>
public static class SalesChange
{
    public const int WindowDays = 30;

    /// <summary>
    /// How many sales each window needs before a change is worth stating.
    /// Deliberately one number in one place: it cannot be tuned from evidence
    /// until real windows exist (~Nov 2026), and tuning it must stay a value
    /// change rather than a rewrite.
    /// </summary>
    public const int MinimumSalesPerWindow = 3;

    public static TierChange Evaluate(IReadOnlyList<SaleObservation> sales, DateOnly today)
    {
        var recentFrom = today.AddDays(-WindowDays);
        var priorFrom = today.AddDays(-WindowDays * 2);

        var recent = sales.Where(s => s.SoldOn >= recentFrom).ToList();
        var prior = sales.Where(s => s.SoldOn >= priorFrom && s.SoldOn < recentFrom).ToList();

        if (recent.Count < MinimumSalesPerWindow || prior.Count < MinimumSalesPerWindow)
        {
            return new ChangeInsufficient(recent.Count, prior.Count);
        }

        var recentMean = recent.Average(s => (decimal)s.PriceCents);
        var priorMean = prior.Average(s => (decimal)s.PriceCents);

        // Unreachable against today's data -- price_cents = 0 occurs in 0 of
        // 10.3M rows, and a mean of three positive integers cannot be zero. Kept
        // because the alternative to two cheap lines is a divide-by-zero crash
        // if that ever stops being true upstream.
        return priorMean == 0m
            ? new ChangeInsufficient(recent.Count, prior.Count)
            : new ChangeAvailable((recentMean - priorMean) / priorMean, recent.Count, prior.Count);
    }
}
