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

    /// <summary>Null when <paramref name="then"/> is zero -- decimal division throws
    /// DivideByZeroException on a zero divisor, and a zero anchor price is defensive-only here
    /// (I2: CardPriceReader filters price_months to PriceCents > 0 at the source; the scraper's
    /// own semantics treat a stored zero as "no sales," not a real price --
    /// PokemonInvestBatch.Domain.Parsing.GradeMonotonicity.cs:23). Callers treat null exactly
    /// like any other insufficient-data case.</summary>
    public static decimal? Roc(decimal now, decimal then) => then == 0 ? null : now / then - 1;

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
        // I2: log is undefined at zero (-Infinity) and below it (NaN), either of which would
        // poison the whole regression. A non-positive price is a data defect this fit cannot
        // honestly represent (defensive: CardPriceReader filters PriceCents > 0 at the source)
        // -- report zero confidence so callers' R2Floor check (ChipEngine.AddTrendR2) treats it
        // as insufficient, the same as any other UNSTABLE FIT, rather than propagating NaN.
        if (values.Any(v => v <= 0))
        {
            return (0m, 0m);
        }

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

    /// <summary>Wilder's RSI over closed-month closes: simple averages over the first
    /// <paramref name="period"/> deltas, Wilder-smoothed thereafter. Null when fewer
    /// than period+1 values are present or any close is non-positive (the same
    /// defensive contract as the other indicators). A window with no losses saturates
    /// at 100; a window with no movement at all reads 50 — neither side has momentum,
    /// and the no-loss convention would otherwise call a flat series overbought.</summary>
    public static decimal? Rsi(IReadOnlyList<decimal> closesOldestFirst, int period)
    {
        if (closesOldestFirst.Count < period + 1 || closesOldestFirst.Any(v => v <= 0))
        {
            return null;
        }

        decimal avgGain = 0, avgLoss = 0;
        for (var i = 1; i <= period; i++)
        {
            var delta = closesOldestFirst[i] - closesOldestFirst[i - 1];
            avgGain += Math.Max(delta, 0);
            avgLoss += Math.Max(-delta, 0);
        }

        avgGain /= period;
        avgLoss /= period;

        for (var i = period + 1; i < closesOldestFirst.Count; i++)
        {
            var delta = closesOldestFirst[i] - closesOldestFirst[i - 1];
            avgGain = (avgGain * (period - 1) + Math.Max(delta, 0)) / period;
            avgLoss = (avgLoss * (period - 1) + Math.Max(-delta, 0)) / period;
        }

        if (avgLoss == 0)
        {
            return avgGain == 0 ? 50m : 100m;
        }

        return 100m - 100m / (1m + avgGain / avgLoss);
    }

    public static decimal Drawdown(IReadOnlyList<decimal> trailingWindowInclusive)
    {
        var peak = trailingWindowInclusive.Max();

        // I2: a non-positive peak means the window carries no real price to measure a drawdown
        // against (defensive: CardPriceReader filters PriceCents > 0 at the source) -- report no
        // drawdown rather than dividing by a value that shouldn't occur.
        return peak <= 0 ? 0m : trailingWindowInclusive[^1] / peak - 1;
    }
}
