namespace CardStock.Domain.Prices;

/// <summary>
/// Which price series a sale belongs to, if any.
///
/// An ALLOW-LIST, and not as a matter of taste. The crawler's
/// GradeTierVocabulary.cs:16-18 records that the vocabulary grows -- "TAG and
/// ACE are recent" -- so a deny-list would quietly fold the next grading
/// company's 10 into the PSA 10 cell. That is the substitution D-022 and D-057
/// both rejected, it would happen without an error, and it would happen in the
/// cell users look at first.
///
/// Six of the nineteen labels map. The other thirteen have no price series to
/// change against; they still render in the sales ledger.
/// </summary>
public static class GradeTierMap
{
    private static readonly Dictionary<string, PriceTier> Tiers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ungraded"] = PriceTier.Ungraded,
            ["Grade 7"] = PriceTier.Grade7,
            ["Grade 8"] = PriceTier.Grade8,
            ["Grade 9"] = PriceTier.Grade9,
            ["Grade 9.5"] = PriceTier.Grade9Half,
            ["PSA 10"] = PriceTier.Psa10,
        };

    public static PriceTier? ToPriceTier(string gradeTier) =>
        Tiers.TryGetValue(Squeeze(gradeTier), out var tier) ? tier : null;

    /// <summary>
    /// The source's option text arrives with nested spans and unclosed tags, so
    /// the same label reaches the database with varying whitespace. Mirrors the
    /// crawler's GradeTierVocabulary.Normalize so both sides agree on equality.
    /// </summary>
    private static string Squeeze(string label) =>
        string.Join(' ', label.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
