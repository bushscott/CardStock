namespace CardStock.Domain.Prices;

/// <summary>
/// One row of sales, with its grade label already resolved to a price tier by
/// GradeTierMap. Sales whose label maps to nothing never reach Domain.
/// </summary>
public sealed record SaleObservation(PriceTier Tier, DateOnly SoldOn, int PriceCents);
