using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceSeriesBuilderTests
{
    private static DateOnly M(int year, int month) => new(year, month, 1);

    private static DateTimeOffset At(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Every_card_returns_six_series_in_strip_order()
    {
        var series = PriceSeriesBuilder.Build([]);

        Assert.Equal(6, series.Count);
        Assert.Equal(
            [PriceTier.Psa10, PriceTier.Grade9Half, PriceTier.Grade9,
             PriceTier.Grade8, PriceTier.Grade7, PriceTier.Ungraded],
            series.Select(s => s.Tier));
    }

    [Fact]
    public void A_card_with_no_prices_returns_six_empty_series_not_an_empty_list()
    {
        var series = PriceSeriesBuilder.Build([]);

        Assert.All(series, s => Assert.True(s.IsEmpty));
        Assert.All(series, s => Assert.Null(s.FirstMonth));
        Assert.All(series, s => Assert.Null(s.LastMonth));
    }

    /// <summary>
    /// The whole reason this layer exists. price_months appends rather than
    /// updates, so the current month legitimately carries several rows -- 17,804
    /// of them across the corpus. Charizard #24 held two for 2026-08-01 on the
    /// day this was written.
    /// </summary>
    [Fact]
    public void Two_rows_for_one_month_resolve_to_the_later_observation()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2861, At(2026, 8, 3)),
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2500, At(2026, 8, 11)),
        ]);

        var psa10 = series.Single(s => s.Tier == PriceTier.Psa10);
        var point = Assert.Single(psa10.Points);
        Assert.Equal(2500, point.PriceCents);
    }

    /// <summary>
    /// Same data, reversed. "The last one in the list" is the bug this rule
    /// exists to prevent, and it passes the test above by accident.
    /// </summary>
    [Fact]
    public void Resolution_does_not_depend_on_the_order_rows_arrive_in()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2500, At(2026, 8, 11)),
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2861, At(2026, 8, 3)),
        ]);

        var point = Assert.Single(series.Single(s => s.Tier == PriceTier.Psa10).Points);
        Assert.Equal(2500, point.PriceCents);
    }

    [Fact]
    public void Points_come_back_ascending_by_month_whatever_order_they_arrived()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Grade9, M(2026, 3), 300, At(2026, 3, 2)),
            new PriceObservation(PriceTier.Grade9, M(2026, 1), 100, At(2026, 1, 2)),
            new PriceObservation(PriceTier.Grade9, M(2026, 2), 200, At(2026, 2, 2)),
        ]);

        var grade9 = series.Single(s => s.Tier == PriceTier.Grade9);
        Assert.Equal([M(2026, 1), M(2026, 2), M(2026, 3)], grade9.Points.Select(p => p.Month));
        Assert.Equal(M(2026, 1), grade9.FirstMonth);
        Assert.Equal(M(2026, 3), grade9.LastMonth);
    }

    [Fact]
    public void Tiers_do_not_bleed_into_each_other()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Psa10, M(2026, 6), 1000, At(2026, 6, 2)),
            new PriceObservation(PriceTier.Ungraded, M(2026, 6), 50, At(2026, 6, 2)),
        ]);

        Assert.Equal(1000, series.Single(s => s.Tier == PriceTier.Psa10).Points[0].PriceCents);
        Assert.Equal(50, series.Single(s => s.Tier == PriceTier.Ungraded).Points[0].PriceCents);
        Assert.True(series.Single(s => s.Tier == PriceTier.Grade8).IsEmpty);
    }
}
