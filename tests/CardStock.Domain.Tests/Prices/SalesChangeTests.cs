using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class SalesChangeTests
{
    private static readonly DateOnly Today = new(2026, 12, 1);

    private static SaleObservation Sold(int daysAgo, int cents) =>
        new(PriceTier.Psa10, Today.AddDays(-daysAgo), cents);

    [Fact]
    public void Three_sales_in_each_window_produce_a_change()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(55, 1000),
        ], Today);

        var available = Assert.IsType<ChangeAvailable>(change);
        Assert.Equal(0.10m, available.Fraction, 4);
        Assert.Equal(3, available.RecentSales);
        Assert.Equal(3, available.PriorSales);
    }

    [Fact]
    public void A_fall_produces_a_negative_change()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 900), Sold(5, 900), Sold(20, 900),
            Sold(35, 1000), Sold(40, 1000), Sold(55, 1000),
        ], Today);

        Assert.Equal(-0.10m, Assert.IsType<ChangeAvailable>(change).Fraction, 4);
    }

    [Fact]
    public void Too_few_recent_sales_is_insufficient_and_carries_no_number()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(55, 1000),
        ], Today);

        var insufficient = Assert.IsType<ChangeInsufficient>(change);
        Assert.Equal(2, insufficient.RecentSales);
        Assert.Equal(3, insufficient.PriorSales);
    }

    [Fact]
    public void Too_few_prior_sales_is_insufficient_even_when_recent_is_healthy()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000),
        ], Today);

        var insufficient = Assert.IsType<ChangeInsufficient>(change);
        Assert.Equal(3, insufficient.RecentSales);
        Assert.Equal(1, insufficient.PriorSales);
    }

    /// <summary>Every card looks like this until roughly November 2026.</summary>
    [Fact]
    public void No_sales_at_all_is_insufficient_with_zero_counts()
    {
        var insufficient = Assert.IsType<ChangeInsufficient>(SalesChange.Evaluate([], Today));

        Assert.Equal(0, insufficient.RecentSales);
        Assert.Equal(0, insufficient.PriorSales);
    }

    [Fact]
    public void A_sale_thirty_days_old_falls_in_the_recent_window()
    {
        var change = SalesChange.Evaluate([
            Sold(30, 1100), Sold(1, 1100), Sold(5, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(45, 1000),
        ], Today);

        Assert.Equal(3, Assert.IsType<ChangeAvailable>(change).RecentSales);
    }

    [Fact]
    public void A_sale_thirty_one_days_old_falls_in_the_prior_window()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(31, 1000), Sold(40, 1000), Sold(45, 1000),
        ], Today);

        var available = Assert.IsType<ChangeAvailable>(change);
        Assert.Equal(3, available.RecentSales);
        Assert.Equal(3, available.PriorSales);
    }

    [Fact]
    public void A_sale_sixty_days_old_is_the_oldest_that_still_counts()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(60, 1000),
        ], Today);

        Assert.Equal(3, Assert.IsType<ChangeAvailable>(change).PriorSales);
    }

    [Fact]
    public void A_sale_sixty_one_days_old_falls_outside_both_windows()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(61, 1000),
        ], Today);

        Assert.Equal(2, Assert.IsType<ChangeInsufficient>(change).PriorSales);
    }

    [Fact]
    public void Sales_older_than_sixty_days_are_ignored_entirely()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(45, 1000),
            Sold(200, 1), Sold(365, 1), Sold(400, 1),
        ], Today);

        var available = Assert.IsType<ChangeAvailable>(change);
        Assert.Equal(3, available.PriorSales);
        Assert.Equal(0.10m, available.Fraction, 4);
    }
}
