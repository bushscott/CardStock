namespace CardStock.Domain.Signals;

/// <summary>
/// The arithmetic under the chips (spec §12), kept apart from firing rules so
/// each formula is provable alone. All inputs are oldest→newest closed-month
/// values — the caller (ChipEngine) owns window selection and the
/// closed-months-only rule.
/// </summary>
public static class Indicators
{
    public static IReadOnlyList<decimal> Ema(IReadOnlyList<decimal> values, int window)
    {
        var alpha = 2m / (window + 1);
        var result = new decimal[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            result[i] = i == 0 ? values[0] : alpha * values[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    public static decimal Roc(decimal now, decimal then) => now / then - 1;

    public static decimal? ZScore(IReadOnlyList<decimal> trailingWindowInclusive)
    {
        var mean = trailingWindowInclusive.Average();
        var sumSquares = trailingWindowInclusive.Sum(v => (v - mean) * (v - mean));
        if (sumSquares == 0)
        {
            return null;
        }

        var sigma = (decimal)Math.Sqrt((double)(sumSquares / (trailingWindowInclusive.Count - 1)));
        return (trailingWindowInclusive[^1] - mean) / sigma;
    }

    public static (decimal Slope, decimal R2) LogTrend(IReadOnlyList<decimal> values)
    {
        var n = values.Count;
        var xs = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var ys = values.Select(v => Math.Log((double)v)).ToArray();

        var meanX = xs.Average();
        var meanY = ys.Average();
        var covXY = xs.Zip(ys, (x, y) => (x - meanX) * (y - meanY)).Sum();
        var varX = xs.Sum(x => (x - meanX) * (x - meanX));
        var slope = covXY / varX;

        var totalSs = ys.Sum(y => (y - meanY) * (y - meanY));
        if (totalSs == 0)
        {
            return ((decimal)slope, 1m);
        }

        var residualSs = xs.Zip(ys, (x, y) =>
        {
            var predicted = meanY + slope * (x - meanX);
            return (y - predicted) * (y - predicted);
        }).Sum();

        return ((decimal)slope, (decimal)(1 - residualSs / totalSs));
    }

    public static decimal Drawdown(IReadOnlyList<decimal> trailingWindowInclusive) =>
        trailingWindowInclusive[^1] / trailingWindowInclusive.Max() - 1;
}
