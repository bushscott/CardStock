namespace CardStock.Domain.Prices;

/// <summary>
/// One row of price_months, as Domain sees it. Domain cannot reference the EF
/// mirror, and should not: this is the only shape the rules need.
/// </summary>
public sealed record PriceObservation(
    PriceTier Tier,
    DateOnly Month,
    int PriceCents,
    DateTimeOffset ObservedAt);
