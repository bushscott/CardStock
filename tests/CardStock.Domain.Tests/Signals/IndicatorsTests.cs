using CardStock.Domain.Signals;

namespace CardStock.Domain.Tests.Signals;

public class IndicatorsTests
{
    [Fact]
    public void Ema_3_over_a_known_run()
    {
        // alpha = 2/(3+1) = 0.5, seeded with the first value:
        // 100 -> 105 (=.5*110+.5*100) -> 112.5 (=.5*120+.5*105)
        var ema = Indicators.Ema([100m, 110m, 120m], 3);
        Assert.Equal([100m, 105m, 112.5m], ema);
    }

    [Fact]
    public void Roc_is_a_plain_fraction()
    {
        Assert.Equal(0.20m, Indicators.Roc(now: 120m, then: 100m));
        Assert.Equal(-0.25m, Indicators.Roc(now: 90m, then: 120m));
    }

    [Fact]
    public void ZScore_uses_sample_stddev_over_the_inclusive_window()
    {
        // [10,10,10,10,10,16]: mean 11, SS = 5*1 + 25 = 30, sample var 6,
        // sigma = sqrt(6) ~ 2.449 -> z = 5 / 2.449 ~ +2.04
        var z = Indicators.ZScore([10m, 10m, 10m, 10m, 10m, 16m]);
        Assert.NotNull(z);
        Assert.Equal(2.04m, Math.Round(z.Value, 2));
    }

    [Fact]
    public void ZScore_is_null_on_a_flat_window()
    {
        Assert.Null(Indicators.ZScore([10m, 10m, 10m]));
    }

    [Fact]
    public void LogTrend_on_a_perfect_exponential_is_r2_one()
    {
        // P_t = 100 * 1.1^t -> ln P is exactly linear, slope ln(1.1) ~ 0.0953
        var values = Enumerable.Range(0, 6).Select(t => 100m * (decimal)Math.Pow(1.1, t)).ToList();
        var (slope, r2) = Indicators.LogTrend(values);
        Assert.Equal(0.0953m, Math.Round(slope, 4));
        Assert.Equal(1.0m, Math.Round(r2, 6));
    }

    [Fact]
    public void LogTrend_on_noise_is_weak()
    {
        var (_, r2) = Indicators.LogTrend([100m, 180m, 90m, 170m, 95m, 160m]);
        Assert.True(r2 < 0.5m);
    }

    [Fact]
    public void Drawdown_measures_from_the_window_peak()
    {
        // last 90 vs peak 120 -> -25%
        Assert.Equal(-0.25m, Indicators.Drawdown([100m, 120m, 90m]));
    }
}
