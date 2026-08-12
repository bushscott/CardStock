using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceStalenessTests
{
    private static DateOnly M(int year, int month) => new(year, month, 1);

    private static TierSeries SeriesEndingAt(int year, int month, int cents = 1486) =>
        new(PriceTier.Psa10, [
            new MonthlyPrice(M(year, month), cents,
                new DateTimeOffset(year, month, 2, 0, 0, 0, TimeSpan.Zero)),
        ]);

    [Fact]
    public void A_price_from_the_current_month_is_available_and_marked_as_this_month()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2026, 8), M(2026, 8));

        var available = Assert.IsType<PriceAvailable>(price);
        Assert.Equal(1486, available.PriceCents);
        Assert.True(available.IsCurrentMonth);
    }

    /// <summary>
    /// The 15% case, and the one most likely to be got wrong, because "does the
    /// price render" and "does the provisional marker show" look like one
    /// decision and are two. Early in a month the source has not yet posted an
    /// average for every tier, so one month behind is healthy, not stale.
    /// </summary>
    [Fact]
    public void A_price_from_last_month_still_renders_but_is_not_this_month()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2026, 7), M(2026, 8));

        var available = Assert.IsType<PriceAvailable>(price);
        Assert.Equal(1486, available.PriceCents);
        Assert.False(available.IsCurrentMonth);
    }

    [Fact]
    public void A_price_two_months_behind_is_stale_and_carries_no_number()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2026, 6), M(2026, 8));

        var stale = Assert.IsType<PriceStale>(price);
        Assert.Equal(M(2026, 6), stale.NewestMonth);
    }

    [Fact]
    public void A_grade_that_last_traded_years_ago_is_stale()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2022, 3), M(2026, 8));

        Assert.IsType<PriceStale>(price);
    }

    [Fact]
    public void A_tier_that_never_had_a_price_says_so()
    {
        var price = PriceStaleness.Evaluate(new TierSeries(PriceTier.Grade7, []), M(2026, 8));

        Assert.IsType<NoPriceSeries>(price);
    }

    [Fact]
    public void Staleness_counts_months_not_days_across_a_year_boundary()
    {
        Assert.IsType<PriceAvailable>(PriceStaleness.Evaluate(SeriesEndingAt(2025, 12), M(2026, 1)));
        Assert.IsType<PriceStale>(PriceStaleness.Evaluate(SeriesEndingAt(2025, 11), M(2026, 1)));
    }

    [Fact]
    public void The_newest_point_is_used_even_when_older_points_exist()
    {
        var series = new TierSeries(PriceTier.Psa10, [
            new MonthlyPrice(M(2026, 6), 100, new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero)),
            new MonthlyPrice(M(2026, 8), 999, new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero)),
        ]);

        var available = Assert.IsType<PriceAvailable>(PriceStaleness.Evaluate(series, M(2026, 8)));
        Assert.Equal(999, available.PriceCents);
    }
}
