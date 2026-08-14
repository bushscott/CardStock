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
    public void Roc_is_null_when_the_anchor_price_is_zero()
    {
        // I2: price_months.price_cents = 0 means "no sales that month," not "worthless"
        // (PokemonInvestBatch.Domain.Parsing.GradeMonotonicity.cs:23) -- CardPriceReader now
        // filters those rows at the source, but decimal division throws DivideByZeroException
        // on a zero divisor regardless, so Indicators guards independently. Null reads exactly
        // like every other insufficient-data case to ChipEngine: no chip, not a crash.
        Assert.Null(Indicators.Roc(now: 120m, then: 0m));
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
    public void LogTrend_reports_zero_confidence_when_any_value_is_non_positive()
    {
        // I2: log is undefined at/below zero (Math.Log(0) = -Infinity, negative = NaN), which
        // would otherwise poison the whole regression. A non-positive price is a data defect a
        // log-regression cannot honestly fit -- report zero confidence, not NaN/Infinity, so
        // ChipEngine.AddTrendR2's existing R2Floor check treats it as insufficient, same as any
        // other UNSTABLE FIT.
        var (slope, r2) = Indicators.LogTrend([100m, 0m, 120m]);
        Assert.Equal(0m, slope);
        Assert.Equal(0m, r2);

        var (negSlope, negR2) = Indicators.LogTrend([100m, -5m, 120m]);
        Assert.Equal(0m, negSlope);
        Assert.Equal(0m, negR2);
    }

    [Fact]
    public void Rsi_over_the_initial_window_matches_the_hand_computation()
    {
        // Deltas: +10, −5, +10, +5, −2, +7 -> avgGain 32/6, avgLoss 7/6,
        // RS = 32/7 -> RSI = 100·32/39 ≈ 82.05.
        var rsi = Indicators.Rsi([100m, 110m, 105m, 115m, 120m, 118m, 125m], 6);

        Assert.NotNull(rsi);
        Assert.Equal(82.05m, Math.Round(rsi.Value, 2));
    }

    [Fact]
    public void Rsi_smooths_with_wilder_weighting_beyond_the_initial_window()
    {
        // One Wilder step on the run above (final delta +5):
        // avgGain (16/3·5+5)/6 = 95/18, avgLoss (7/6·5)/6 = 35/36
        // -> RS = 38/7 -> RSI = 100 − 700/45 ≈ 84.44.
        var rsi = Indicators.Rsi([100m, 110m, 105m, 115m, 120m, 118m, 125m, 130m], 6);

        Assert.NotNull(rsi);
        Assert.Equal(84.44m, Math.Round(rsi.Value, 2));
    }

    [Fact]
    public void Rsi_is_null_below_period_plus_one_values()
    {
        Assert.Null(Indicators.Rsi([100m, 110m, 105m, 115m, 120m, 118m], 6));
    }

    [Fact]
    public void Rsi_guards_non_positive_prices()
    {
        // Same defensive contract as the other indicators: a stored zero means
        // "no sales," never a price -- null, not a fabricated reading.
        Assert.Null(Indicators.Rsi([100m, 0m, 105m, 115m, 120m, 118m, 125m], 6));
    }

    [Fact]
    public void Rsi_saturates_at_100_when_the_window_has_no_losses()
    {
        Assert.Equal(100m, Indicators.Rsi([100m, 110m, 120m, 130m, 140m, 150m, 160m], 6));
    }

    [Fact]
    public void Rsi_reads_50_on_a_flat_run()
    {
        // No gains and no losses: RS is 0/0. Neither side has momentum, and 50
        // is the only reading that says so.
        Assert.Equal(50m, Indicators.Rsi([100m, 100m, 100m, 100m, 100m, 100m, 100m], 6));
    }

    [Fact]
    public void Drawdown_measures_from_the_window_peak()
    {
        // last 90 vs peak 120 -> -25%
        Assert.Equal(-0.25m, Indicators.Drawdown([100m, 120m, 90m]));
    }

    [Fact]
    public void Drawdown_is_zero_when_the_window_peak_is_non_positive()
    {
        // I2: a non-positive peak means the window carries no real price to measure a drawdown
        // against -- report no drawdown rather than dividing by a value that shouldn't occur.
        Assert.Equal(0m, Indicators.Drawdown([0m, 0m, 0m]));
    }
}
