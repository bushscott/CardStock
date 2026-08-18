using Bunit;
using CardStock.Web.Components;
using Xunit;

namespace CardStock.Web.Tests;

public class LoadingRingTests : BunitContext
{
    [Fact]
    public void The_ring_carries_track_arc_and_the_busy_caption()
    {
        var cut = Render<LoadingRing>();

        var root = cut.Find(".loading-ring");
        Assert.Equal("true", root.GetAttribute("aria-busy"));
        Assert.Equal(2, cut.FindAll("svg circle").Count);
        Assert.NotNull(cut.Find("svg circle.track"));
        Assert.NotNull(cut.Find("svg circle.arc"));
        Assert.Contains("Loading…", cut.Find(".cap").TextContent);
    }
}
