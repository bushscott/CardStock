namespace CardStock.Domain.Prices;

/// <summary>
/// One month of a windowed series. Exactly one of three things, and only one of
/// them carries a number -- so drawing a line across a hole is a compile error
/// rather than a rule somebody has to remember.
/// </summary>
public abstract record PriceSlot(DateOnly Month);

/// <summary>The source published a price for this month.</summary>
public sealed record ObservedPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt)
    : PriceSlot(Month);

/// <summary>
/// Inside the series, but the source published nothing for this month. A HOLE,
/// with real data either side: the line must break here. Distinct from
/// OutsideSeries, because drawing them alike would claim the card's history
/// begins later than it does.
/// </summary>
public sealed record MissingMonth(DateOnly Month) : PriceSlot(Month);

/// <summary>
/// Before the series' first month or after its last. Not a hole -- there is
/// simply no series here yet, or not any more.
/// </summary>
public sealed record OutsideSeries(DateOnly Month) : PriceSlot(Month);
