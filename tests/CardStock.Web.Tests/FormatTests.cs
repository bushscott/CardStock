using CardStock.Web.Services;

namespace CardStock.Web.Tests;

public class FormatTests
{
    [Fact]
    public void Money_rounds_to_whole_dollars_with_en_US_grouping()
    {
        Assert.Equal("$1,486", Format.Money(148600));
    }

    [Fact]
    public void ChangePercent_uses_the_true_minus_sign_for_negatives()
    {
        var text = Format.ChangePercent(-0.002m);

        Assert.Equal("−0.2%", text);
        Assert.Equal('−', text[0]);
    }

    [Fact]
    public void ChangePercent_signs_positive_and_zero_values_with_a_plus()
    {
        Assert.Equal("+6.2%", Format.ChangePercent(0.062m));
        Assert.Equal("+0.0%", Format.ChangePercent(0m));
    }

    [Fact]
    public void MonthLabel_renders_the_typographic_apostrophe_from_a_full_date()
    {
        var text = Format.MonthLabel("2026-08-01");

        Assert.Equal("Aug ’26", text);
        Assert.Contains('’', text);
    }

    [Fact]
    public void MonthLabel_accepts_a_bare_yyyy_MM_month()
    {
        Assert.Equal("Aug ’26", Format.MonthLabel("2026-08"));
    }

    [Theory]
    [InlineData(999_900L, "$9,999")]        // below the 10K floor: full dollars
    [InlineData(1_000_000L, "$10K")]        // exactly $10,000
    [InlineData(9_640_000L, "$96.4K")]
    [InlineData(120_000_000L, "$1.2M")]
    [InlineData(100_000_000L, "$1M")]
    public void AbbrevMoney_abbreviates_at_ten_thousand(long cents, string expected) =>
        Assert.Equal(expected, Format.AbbrevMoney(cents));

    [Fact]
    public void MonthYear_prints_the_full_year() =>
        Assert.Equal("Dec 2021", Format.MonthYear("2021-12"));
}
