namespace CardStock.Domain.Prices;

/// <summary>One month of one tier, already resolved to a single observation.</summary>
public sealed record MonthlyPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt);
