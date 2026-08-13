namespace CardStock.Domain.Prices;

/// <summary>
/// The tier→display-name mapping. Single source so the chip engine's tooltips
/// and the wire mapper's strip labels can never drift apart (R-2).
/// </summary>
public static class TierLabels
{
    public static string For(PriceTier tier) => tier switch
    {
        PriceTier.Psa10 => "PSA 10",
        PriceTier.Grade9Half => "Grade 9.5",
        PriceTier.Grade9 => "Grade 9",
        PriceTier.Grade8 => "Grade 8",
        PriceTier.Grade7 => "Grade 7",
        _ => "Raw",
    };
}
