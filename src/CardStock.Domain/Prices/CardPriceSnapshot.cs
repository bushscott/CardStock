namespace CardStock.Domain.Prices;

/// <summary>
/// Everything the price surfaces need for one card.
///
/// <paramref name="Tiers"/> is ALWAYS six, in strip order, however little the
/// card has. A short list would push "which tiers came back?" onto every caller,
/// and 11% of cards have no prices at all.
/// </summary>
public sealed record CardPriceSnapshot(
    long CardId,
    DateTimeOffset? LastVisitedAt,
    IReadOnlyList<TierSnapshot> Tiers);

/// <summary>
/// One tier. <paramref name="Price"/> is derivable from <paramref name="Series"/>
/// and is carried anyway, so no caller re-implements "newest point, unless it is
/// too old" -- the chart and the strip must never disagree about the same number.
/// </summary>
public sealed record TierSnapshot(
    PriceTier Tier,
    TierSeries Series,
    TierPrice Price,
    TierChange Change);
