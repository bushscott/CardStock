using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class GradeTierMapTests
{
    [Theory]
    [InlineData("Ungraded", PriceTier.Ungraded)]
    [InlineData("Grade 7", PriceTier.Grade7)]
    [InlineData("Grade 8", PriceTier.Grade8)]
    [InlineData("Grade 9", PriceTier.Grade9)]
    [InlineData("Grade 9.5", PriceTier.Grade9Half)]
    [InlineData("PSA 10", PriceTier.Psa10)]
    public void The_six_labels_with_a_price_series_map_to_it(string label, PriceTier expected)
    {
        Assert.Equal(expected, GradeTierMap.ToPriceTier(label));
    }

    /// <summary>
    /// price_months carries nothing below Grade 7 (D-012), so these sales have no
    /// price to change against. They still appear in the ledger.
    /// </summary>
    [Theory]
    [InlineData("Grade 1")]
    [InlineData("Grade 2")]
    [InlineData("Grade 3")]
    [InlineData("Grade 4")]
    [InlineData("Grade 5")]
    [InlineData("Grade 6")]
    public void Grades_below_seven_map_to_nothing(string label)
    {
        Assert.Null(GradeTierMap.ToPriceTier(label));
    }

    /// <summary>
    /// The source splits grading companies at 10 and price_months has exactly one
    /// grade-10 tier. Folding these into PSA 10 is the substitution D-022 and
    /// D-057 both rejected as statistically dishonest.
    /// </summary>
    [Theory]
    [InlineData("CGC 10")]
    [InlineData("CGC 10 Prist.")]
    [InlineData("BGS 10")]
    [InlineData("BGS 10 Black")]
    [InlineData("SGC 10")]
    [InlineData("TAG 10")]
    [InlineData("ACE 10")]
    public void Tens_from_other_graders_map_to_nothing(string label)
    {
        Assert.Null(GradeTierMap.ToPriceTier(label));
    }

    /// <summary>
    /// GradeTierVocabulary.cs:16-18 says the list grows -- TAG and ACE are recent.
    /// A deny-list would fold the next new grader's 10 into PSA 10 silently, with
    /// no error, in the cell users read first.
    /// </summary>
    [Fact]
    public void A_grader_that_does_not_exist_yet_maps_to_nothing()
    {
        Assert.Null(GradeTierMap.ToPriceTier("PGX 10"));
        Assert.Null(GradeTierMap.ToPriceTier("Grade 9.7"));
        Assert.Null(GradeTierMap.ToPriceTier(""));
    }

    [Theory]
    [InlineData("psa 10")]
    [InlineData("PSA  10")]
    [InlineData(" PSA 10 ")]
    [InlineData("PSA\n 10")]
    public void Casing_and_ragged_whitespace_still_match(string label)
    {
        Assert.Equal(PriceTier.Psa10, GradeTierMap.ToPriceTier(label));
    }

    /// <summary>The UI says Raw; the data says Ungraded. Only the UI may say Raw.</summary>
    [Fact]
    public void Raw_is_a_display_word_and_is_not_a_stored_value()
    {
        Assert.Null(GradeTierMap.ToPriceTier("Raw"));
    }
}
