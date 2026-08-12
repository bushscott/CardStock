namespace CardStock.Domain.Prices;

/// <summary>
/// What the strip's bottom line shows. Only one case carries a number; the
/// other renders a dash.
/// </summary>
public abstract record TierChange;

/// <summary><paramref name="Fraction"/> is a fraction, not a percentage: 0.062m is +6.2%.</summary>
public sealed record ChangeAvailable(decimal Fraction, int RecentSales, int PriorSales) : TierChange;

/// <summary>
/// Too few sales in one or both windows. A PERMANENT possibility, not a phase
/// of the data filling in: a quiet card will not have three sales in 30 days in
/// 2028 either. Renders a dash, with no countdown and no unlock date -- see
/// D-075, where a countdown was proposed and deliberately rejected.
/// </summary>
public sealed record ChangeInsufficient(int RecentSales, int PriorSales) : TierChange;
