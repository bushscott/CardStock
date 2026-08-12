namespace CardStock.Domain.Prices;

/// <summary>
/// What the strip's top line shows for one tier. Only one case carries a
/// number; the other two render a dash.
/// </summary>
public abstract record TierPrice;

/// <summary>
/// A price recent enough to stand as current.
/// <paramref name="IsCurrentMonth"/> drives the provisional marker, and is a
/// separate question from whether the price renders at all -- a price from last
/// month renders without the marker.
/// </summary>
public sealed record PriceAvailable(int PriceCents, DateOnly Month, bool IsCurrentMonth) : TierPrice;

/// <summary>
/// The newest published month is too far back to present as a current price.
/// A grade nobody has traded in a while; 3.5% of real series.
/// </summary>
public sealed record PriceStale(DateOnly NewestMonth) : TierPrice;

/// <summary>
/// The source has never published a price at this grade for this card. Only 19%
/// of cards carry all six tiers, so this is ordinary, not exceptional.
/// </summary>
public sealed record NoPriceSeries : TierPrice;
