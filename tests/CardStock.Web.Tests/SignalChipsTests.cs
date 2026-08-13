using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

public class SignalChipsTests : BunitContext
{
    private static ChipDto Chip(string text) => new("▲", text, $"{text} tooltip", "pos");

    [Fact]
    public void Renders_the_first_four_and_reveals_the_rest_on_click()
    {
        var chips = Enumerable.Range(1, 6).Select(i => Chip($"Signal {i}")).ToList();

        var cut = Render<SignalChips>(p => p.Add(x => x.Chips, chips));

        Assert.Equal(4, cut.FindAll(".chip").Count);
        var more = cut.Find(".chip-more");
        Assert.Equal("+2 more", more.TextContent);

        more.Click();

        Assert.Equal(6, cut.FindAll(".chip").Count);
    }

    [Fact]
    public void Renders_an_empty_row_with_no_more_button_when_there_are_no_chips()
    {
        var cut = Render<SignalChips>(p => p.Add(x => x.Chips, Array.Empty<ChipDto>()));

        Assert.Empty(cut.FindAll(".chip"));
        Assert.Empty(cut.FindAll(".chip-more"));
        cut.Find(".chip-row");
    }
}
