using CardStock.Domain.Cards;

namespace CardStock.Domain.Tests.Cards;

public class CardTitleTests
{
    [Theory]
    [InlineData("Umbreon VMAX #215", "Umbreon VMAX", "215")]
    [InlineData("Charizard [Shadowless] #4", "Charizard [Shadowless]", "4")]
    [InlineData("Umbreon VMAX #TG23", "Umbreon VMAX", "TG23")]
    [InlineData("Moltres & Zapdos & Articuno GX #SM210", "Moltres & Zapdos & Articuno GX", "SM210")]
    [InlineData("Pikachu with Grey Felt Hat #85", "Pikachu with Grey Felt Hat", "85")]
    [InlineData("Flabébé #151", "Flabébé", "151")]
    public void Trailing_number_splits_off(string raw, string title, string number)
    {
        var parsed = CardTitle.Parse(raw);
        Assert.Equal(title, parsed.Title);
        Assert.Equal(number, parsed.CollectorNumber);
    }

    [Theory]
    [InlineData("Ancient Mew")]                 // no number at all
    [InlineData("Booster Box [1st Edition]")]   // sealed product
    [InlineData("Mew #")]                       // dangling hash
    [InlineData("#4")]                          // number with no name
    [InlineData("")]                            // empty
    public void Anything_else_returns_the_raw_name_and_no_number(string raw)
    {
        var parsed = CardTitle.Parse(raw);
        Assert.Equal(raw.Trim(), parsed.Title);
        Assert.Null(parsed.CollectorNumber);
    }

    [Fact]
    public void A_hash_mid_name_is_not_a_collector_number()
    {
        // Only a TRAILING #token parses; anything after the token kills it.
        var parsed = CardTitle.Parse("Weird #1 Promo Thing");
        Assert.Equal("Weird #1 Promo Thing", parsed.Title);
        Assert.Null(parsed.CollectorNumber);
    }
}
