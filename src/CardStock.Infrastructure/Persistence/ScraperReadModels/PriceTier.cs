namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Mirrors PokemonInvestBatch.Domain.Parsing.PriceTier exactly. CardStock cannot
/// reference that assembly, so this is a copy and the two must stay in step.
///
/// Stored as <c>integer</c> in price_months.tier -- verified in the crawler's
/// 20260728032826_InitialCreate.cs:134, which is NOT the smallint used for
/// populations.grade. NEVER reorder or insert a member: the ordinal IS the
/// stored value, so a change here silently misreads every historical price.
/// </summary>
public enum PriceTier
{
    Ungraded,
    Grade7,
    Grade8,
    Grade9,
    Grade9Half,
    Psa10,
}
