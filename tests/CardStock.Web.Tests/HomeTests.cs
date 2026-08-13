using Bunit;
using CardStock.Web.Pages;

namespace CardStock.Web.Tests;

public class HomeTests : BunitContext
{
    [Fact]
    public void Home_renders_the_deferred_placeholder_not_template_filler()
    {
        // D-087: the root is a real page whose feature hasn't arrived, so it renders
        // the honest placeholder — never the Blazor template's "Hello, world!".
        var cut = Render<Home>();

        Assert.Contains("Home arrives in a later phase", cut.Markup);
        Assert.DoesNotContain("Hello, world", cut.Markup);
        Assert.Empty(cut.FindAll("a"));
    }
}
