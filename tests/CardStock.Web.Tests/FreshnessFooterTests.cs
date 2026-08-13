using Bunit;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

public class FreshnessFooterTests : BunitContext
{
    [Fact]
    public void Renders_both_stamps_with_their_verbatim_tooltips_when_both_dates_are_known()
    {
        var cut = Render<FreshnessFooter>(p => p
            .Add(x => x.LastVisitedAt, new DateTimeOffset(2026, 8, 13, 9, 0, 0, TimeSpan.Zero))
            .Add(x => x.CensusObservedAt, new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero)));

        var stamps = cut.FindAll(".freshness-stamp");
        Assert.Equal(2, stamps.Count);

        Assert.Equal("Sales & prices refreshed 2026-08-13", stamps[0].TextContent);
        Assert.Equal(
            "Opening a card page triggers a fresh scrape — the ledger and prices you see include sales up to right now",
            stamps[0].GetAttribute("title"));

        Assert.Equal("Census as of 2026-07-30", stamps[1].TextContent);
        Assert.Equal(
            "Census updates when the graders publish; we capture it on the same visits as prices.",
            stamps[1].GetAttribute("title"));
    }

    [Fact]
    public void Renders_the_honest_never_states_when_either_date_is_null()
    {
        var cut = Render<FreshnessFooter>(p => p
            .Add(x => x.LastVisitedAt, (DateTimeOffset?)null)
            .Add(x => x.CensusObservedAt, (DateTimeOffset?)null));

        var stamps = cut.FindAll(".freshness-stamp");
        Assert.Equal("Sales & prices never visited", stamps[0].TextContent);
        Assert.Equal("Census never observed", stamps[1].TextContent);
    }

    [Fact]
    public void Credits_TradingView_with_a_link_to_the_real_site()
    {
        var cut = Render<FreshnessFooter>(p => p
            .Add(x => x.LastVisitedAt, (DateTimeOffset?)null)
            .Add(x => x.CensusObservedAt, (DateTimeOffset?)null));

        Assert.Contains("Charts by", cut.Find(".freshness-attribution").TextContent);
        var link = cut.Find(".freshness-attribution a");
        Assert.Equal("https://www.tradingview.com/", link.GetAttribute("href"));
        Assert.Equal("TradingView", link.TextContent);
    }
}
