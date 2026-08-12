using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceTierTests
{
    /// <summary>
    /// price_months.tier stores the ordinal as an integer, so these numbers are
    /// data, not implementation detail. Reordering the enum would silently
    /// reinterpret every historical price in the database -- 10.3M rows, with no
    /// error anywhere. This test is the tripwire.
    /// </summary>
    [Fact]
    public void Tier_ordinals_are_the_values_stored_in_the_database()
    {
        Assert.Equal(0, (int)PriceTier.Ungraded);
        Assert.Equal(1, (int)PriceTier.Grade7);
        Assert.Equal(2, (int)PriceTier.Grade8);
        Assert.Equal(3, (int)PriceTier.Grade9);
        Assert.Equal(4, (int)PriceTier.Grade9Half);
        Assert.Equal(5, (int)PriceTier.Psa10);
    }

    [Fact]
    public void There_are_exactly_six_price_tiers()
    {
        Assert.Equal(6, Enum.GetValues<PriceTier>().Length);
    }
}
