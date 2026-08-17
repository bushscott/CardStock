using CardStock.Domain.Signals;

namespace CardStock.Domain.Prices;

/// <summary>Per-card roster math. Mirrors ChipEngine's month rule exactly
/// (At: month = currentMonth − 1 − offset), so a roster ROC always agrees
/// with the same card's signals panel.</summary>
public static class RosterMath
{
    public static decimal? Roc3M(
        IReadOnlyDictionary<DateOnly, int> psa10CentsByMonth, DateOnly currentMonth)
    {
        var m1 = currentMonth.AddMonths(-1);
        var m4 = currentMonth.AddMonths(-4);
        if (!psa10CentsByMonth.TryGetValue(m1, out var now) ||
            !psa10CentsByMonth.TryGetValue(m4, out var then))
        {
            return null;
        }

        return Indicators.Roc(now / 100m, then / 100m);
    }
}
