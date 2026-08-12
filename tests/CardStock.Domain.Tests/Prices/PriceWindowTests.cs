using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceWindowTests
{
    private static DateOnly M(int year, int month) => new(year, month, 1);

    private static MonthlyPrice P(int year, int month, int cents) =>
        new(M(year, month), cents, new DateTimeOffset(year, month, 2, 0, 0, 0, TimeSpan.Zero));

    private static TierSeries Series(params MonthlyPrice[] points) =>
        new(PriceTier.Psa10, points);

    [Fact]
    public void The_window_is_one_slot_per_month_oldest_first_ending_at_the_month_asked_for()
    {
        var slots = PriceWindow.Of(Series(P(2026, 6, 100)), M(2026, 8), 3);

        Assert.Equal([M(2026, 6), M(2026, 7), M(2026, 8)], slots.Select(s => s.Month));
    }

    [Fact]
    public void A_month_with_a_point_is_observed_and_carries_its_price()
    {
        var slots = PriceWindow.Of(Series(P(2026, 8, 1486)), M(2026, 8), 1);

        var observed = Assert.IsType<ObservedPrice>(Assert.Single(slots));
        Assert.Equal(1486, observed.PriceCents);
    }

    /// <summary>
    /// The Charmeleon #24 case, from the live database: Grade 8 runs 2021-05 to
    /// 2026-08 with September 2021 missing, $299.99 before it and $40.00 after.
    /// Carrying the earlier value across the hole would draw an 87% single-month
    /// crash that never happened.
    /// </summary>
    [Fact]
    public void A_hole_inside_the_series_is_a_gap_and_never_a_carried_value()
    {
        var slots = PriceWindow.Of(
            Series(P(2021, 8, 29999), P(2021, 10, 4000)),
            M(2021, 10), 3);

        Assert.IsType<ObservedPrice>(slots[0]);
        Assert.IsType<MissingMonth>(slots[1]);
        Assert.IsType<ObservedPrice>(slots[2]);
        Assert.Equal(M(2021, 9), slots[1].Month);
    }

    [Fact]
    public void Months_before_the_series_starts_are_outside_it_not_gaps()
    {
        var slots = PriceWindow.Of(Series(P(2026, 8, 100)), M(2026, 8), 3);

        Assert.IsType<OutsideSeries>(slots[0]);
        Assert.IsType<OutsideSeries>(slots[1]);
        Assert.IsType<ObservedPrice>(slots[2]);
    }

    [Fact]
    public void Months_after_the_series_ends_are_outside_it_not_gaps()
    {
        var slots = PriceWindow.Of(Series(P(2026, 6, 100)), M(2026, 8), 3);

        Assert.IsType<ObservedPrice>(slots[0]);
        Assert.IsType<OutsideSeries>(slots[1]);
        Assert.IsType<OutsideSeries>(slots[2]);
    }

    [Fact]
    public void An_empty_series_is_outside_everywhere()
    {
        var slots = PriceWindow.Of(new TierSeries(PriceTier.Grade7, []), M(2026, 8), 12);

        Assert.Equal(12, slots.Count);
        Assert.All(slots, s => Assert.IsType<OutsideSeries>(s));
    }

    [Fact]
    public void The_window_crosses_a_year_boundary_correctly()
    {
        var slots = PriceWindow.Of(Series(P(2025, 12, 100)), M(2026, 2), 3);

        Assert.Equal([M(2025, 12), M(2026, 1), M(2026, 2)], slots.Select(s => s.Month));
    }
}
