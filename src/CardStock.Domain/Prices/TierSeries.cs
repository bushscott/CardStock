namespace CardStock.Domain.Prices;

/// <summary>
/// One tier's published history, ascending by month.
///
/// Holds ONLY the months that have data. It is never padded and never filled:
/// a month absent here means the source published no point for it, and 33% of
/// real series contain at least one such hole.
/// </summary>
public sealed record TierSeries(PriceTier Tier, IReadOnlyList<MonthlyPrice> Points)
{
    public bool IsEmpty => Points.Count == 0;

    public DateOnly? FirstMonth => IsEmpty ? null : Points[0].Month;

    public DateOnly? LastMonth => IsEmpty ? null : Points[^1].Month;
}
