using CardStock.Domain.Signals;
using Skender.Stock.Indicators;

namespace CardStock.Domain.Tests.Signals;

/// <summary>
/// External referee for the hand-written indicator arithmetic (owner ask,
/// 2026-08-13): the recursive formulas are cross-validated against
/// Skender.Stock.Indicators — the standard .NET TA library, itself validated
/// against TA-Lib — on every run, forever. Hand-computed fixtures prove the
/// code matches our own math; these prove our math matches the canon.
///
/// Where our seeding convention deliberately differs (our EMA seeds with the
/// first value so a 10-month window yields full-length values; Skender seeds
/// with an SMA), the check asserts convergence on a 120-point series, where
/// seed influence has decayed below tolerance: agreement there proves the
/// recursion coefficients are right — a wrong alpha or a wrong recursion
/// diverges, it does not converge.
///
/// Not referee-checked here, and why: z-score uses SAMPLE stddev (n−1, spec
/// §12, pinned by a hand fixture and a stdlib-statistics receipt in D-092)
/// while Skender's StdDev/ZScore uses population; drawdown is the arithmetic
/// identity last/peak − 1 with nothing to referee.
/// </summary>
public class IndicatorsCrossValidationTests
{
    private const double Tolerance = 1e-6;

    private static List<Quote> Quotes(IEnumerable<decimal> closes) =>
        [.. closes.Select((c, i) => new Quote
        {
            Date = new DateTime(2020, 1, 1).AddMonths(i),
            Close = c,
        })];

    /// <summary>Ten years of months, deterministic, strictly positive, trending with waves.</summary>
    private static List<decimal> LongSeries() =>
        [.. Enumerable.Range(0, 120).Select(i => 100m + i + 40m * (decimal)Math.Sin(i / 3.0))];

    [Fact]
    public void Rsi_matches_the_reference_on_the_unit_fixture()
    {
        List<decimal> closes = [100m, 110m, 105m, 115m, 120m, 118m, 125m];

        var theirs = Quotes(closes).GetRsi(6).Last().Rsi;
        var ours = Indicators.Rsi(closes, 6);

        Assert.NotNull(theirs);
        Assert.NotNull(ours);
        Assert.Equal(theirs.Value, (double)ours.Value, Tolerance);
    }

    [Fact]
    public void Rsi_matches_the_reference_through_a_wilder_smoothing_step()
    {
        List<decimal> closes = [100m, 110m, 105m, 115m, 120m, 118m, 125m, 130m];

        var theirs = Quotes(closes).GetRsi(6).Last().Rsi;
        var ours = Indicators.Rsi(closes, 6);

        Assert.NotNull(theirs);
        Assert.NotNull(ours);
        Assert.Equal(theirs.Value, (double)ours.Value, Tolerance);
    }

    [Fact]
    public void Rsi_tracks_the_reference_across_a_long_series()
    {
        var closes = LongSeries();

        var theirs = Quotes(closes).GetRsi(6).Last().Rsi;
        var ours = Indicators.Rsi(closes, 6);

        Assert.NotNull(theirs);
        Assert.NotNull(ours);
        Assert.Equal(theirs.Value, (double)ours.Value, Tolerance);
    }

    [Fact]
    public void Roc_matches_the_reference()
    {
        var closes = LongSeries();

        var theirs = Quotes(closes).GetRoc(3).Last().Roc;
        var ours = Indicators.Roc(closes[^1], closes[^4]);

        Assert.NotNull(theirs);
        Assert.NotNull(ours);
        Assert.Equal(theirs.Value, (double)(ours.Value * 100), Tolerance);
    }

    [Fact]
    public void Ema_converges_to_the_reference_once_seeding_has_decayed()
    {
        var closes = LongSeries();
        foreach (var span in new[] { 3, 6, 9 })
        {
            var theirs = Quotes(closes).GetEma(span).Last().Ema;
            var ours = Indicators.Ema(closes, span)[^1];

            Assert.NotNull(theirs);
            Assert.Equal(theirs.Value, (double)ours, Tolerance);
        }
    }

    [Fact]
    public void Macd_3_6_4_converges_to_the_reference()
    {
        var closes = LongSeries();

        var theirs = Quotes(closes).GetMacd(3, 6, 4).Last();

        var macd = Indicators.Ema(closes, 3).Zip(Indicators.Ema(closes, 6), (f, s) => f - s).ToList();
        var signal = Indicators.Ema(macd, 4);
        var histogram = macd[^1] - signal[^1];

        Assert.NotNull(theirs.Macd);
        Assert.NotNull(theirs.Signal);
        Assert.NotNull(theirs.Histogram);
        Assert.Equal(theirs.Macd.Value, (double)macd[^1], Tolerance);
        Assert.Equal(theirs.Signal.Value, (double)signal[^1], Tolerance);
        Assert.Equal(theirs.Histogram.Value, (double)histogram, Tolerance);
    }

    [Fact]
    public void Log_trend_matches_the_reference_regression_on_a_perfect_exponential()
    {
        // Feed the reference library our LOG closes: its linear regression over
        // them is exactly LogTrend's OLS — same slope per month, same R².
        var values = Enumerable.Range(0, 6).Select(t => 100m * (decimal)Math.Pow(1.1, t)).ToList();
        var logCloses = values.Select(v => (decimal)Math.Log((double)v));

        var theirs = Quotes(logCloses).GetSlope(6).Last();
        var (slope, r2) = Indicators.LogTrend(values);

        Assert.NotNull(theirs.Slope);
        Assert.NotNull(theirs.RSquared);
        Assert.Equal(theirs.Slope.Value, (double)slope, Tolerance);
        Assert.Equal(theirs.RSquared.Value, (double)r2, Tolerance);
    }

    [Fact]
    public void Log_trend_matches_the_reference_regression_on_noise()
    {
        List<decimal> values = [100m, 180m, 90m, 170m, 95m, 160m];
        var logCloses = values.Select(v => (decimal)Math.Log((double)v));

        var theirs = Quotes(logCloses).GetSlope(6).Last();
        var (slope, r2) = Indicators.LogTrend(values);

        Assert.NotNull(theirs.Slope);
        Assert.NotNull(theirs.RSquared);
        Assert.Equal(theirs.Slope.Value, (double)slope, Tolerance);
        Assert.Equal(theirs.RSquared.Value, (double)r2, Tolerance);
    }
}
