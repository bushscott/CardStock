using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class CardPriceSnapshotBuilderTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);
    private static readonly DateTimeOffset Visited = new(2026, 8, 9, 14, 0, 0, TimeSpan.Zero);

    private static PriceObservation Price(PriceTier tier, int year, int month, int cents) =>
        new(tier, new DateOnly(year, month, 1), cents,
            new DateTimeOffset(year, month, 2, 0, 0, 0, TimeSpan.Zero));

    private static SaleObservation Sale(PriceTier tier, int daysAgo, int cents) =>
        new(tier, Today.AddDays(-daysAgo), cents);

    [Fact]
    public void The_snapshot_carries_the_card_id_and_when_we_last_looked()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited, [], [], Today);

        Assert.Equal(42, snapshot.CardId);
        Assert.Equal(Visited, snapshot.LastVisitedAt);
    }

    [Fact]
    public void A_card_with_nothing_still_returns_six_tiers()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, null, [], [], Today);

        Assert.Equal(6, snapshot.Tiers.Count);
        Assert.All(snapshot.Tiers, t => Assert.IsType<NoPriceSeries>(t.Price));
        Assert.All(snapshot.Tiers, t => Assert.IsType<ChangeInsufficient>(t.Change));
        Assert.All(snapshot.Tiers, t => Assert.True(t.Series.IsEmpty));
    }

    [Fact]
    public void Price_and_change_land_on_the_tier_they_belong_to()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited,
            [Price(PriceTier.Psa10, 2026, 8, 148600)],
            [
                Sale(PriceTier.Psa10, 1, 1100), Sale(PriceTier.Psa10, 5, 1100), Sale(PriceTier.Psa10, 20, 1100),
                Sale(PriceTier.Psa10, 35, 1000), Sale(PriceTier.Psa10, 40, 1000), Sale(PriceTier.Psa10, 55, 1000),
            ],
            Today);

        var psa10 = snapshot.Tiers.Single(t => t.Tier == PriceTier.Psa10);
        Assert.Equal(148600, Assert.IsType<PriceAvailable>(psa10.Price).PriceCents);
        Assert.Equal(3, Assert.IsType<ChangeAvailable>(psa10.Change).RecentSales);

        var grade9 = snapshot.Tiers.Single(t => t.Tier == PriceTier.Grade9);
        Assert.IsType<NoPriceSeries>(grade9.Price);
        Assert.IsType<ChangeInsufficient>(grade9.Change);
    }

    /// <summary>
    /// The everyday case for four cards in five: a price with no series at some
    /// grades, and no change anywhere because sales are two weeks old.
    /// </summary>
    [Fact]
    public void A_typical_card_has_prices_at_some_tiers_and_no_change_anywhere()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited,
            [
                Price(PriceTier.Psa10, 2026, 8, 148600),
                Price(PriceTier.Grade9, 2026, 8, 84200),
                Price(PriceTier.Ungraded, 2026, 8, 45500),
            ],
            [Sale(PriceTier.Psa10, 3, 150000)],
            Today);

        Assert.Equal(3, snapshot.Tiers.Count(t => t.Price is PriceAvailable));
        Assert.Equal(3, snapshot.Tiers.Count(t => t.Price is NoPriceSeries));
        Assert.All(snapshot.Tiers, t => Assert.IsType<ChangeInsufficient>(t.Change));
    }

    [Fact]
    public void The_series_is_carried_so_the_chart_and_the_strip_cannot_disagree()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited,
            [Price(PriceTier.Psa10, 2026, 7, 100), Price(PriceTier.Psa10, 2026, 8, 200)],
            [], Today);

        var psa10 = snapshot.Tiers.Single(t => t.Tier == PriceTier.Psa10);
        Assert.Equal(2, psa10.Series.Points.Count);
        Assert.Equal(200, Assert.IsType<PriceAvailable>(psa10.Price).PriceCents);
        Assert.Equal(psa10.Series.Points[^1].PriceCents, ((PriceAvailable)psa10.Price).PriceCents);
    }

    /// <summary>The current month comes from the supplied date, never from the machine clock.</summary>
    [Fact]
    public void Staleness_is_judged_against_the_date_passed_in()
    {
        var prices = new[] { Price(PriceTier.Psa10, 2026, 8, 100) };

        var inAugust = CardPriceSnapshotBuilder.Build(42, null, prices, [], new DateOnly(2026, 8, 31));
        var inNovember = CardPriceSnapshotBuilder.Build(42, null, prices, [], new DateOnly(2026, 11, 1));

        Assert.True(Assert.IsType<PriceAvailable>(
            inAugust.Tiers.Single(t => t.Tier == PriceTier.Psa10).Price).IsCurrentMonth);
        Assert.IsType<PriceStale>(inNovember.Tiers.Single(t => t.Tier == PriceTier.Psa10).Price);
    }
}
