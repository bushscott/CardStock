using Bunit;
using CardStock.Web.Pages;
using Xunit;

namespace CardStock.Web.Tests;

public class AboutDataPageTests : BunitContext
{
    [Fact]
    public void The_source_is_named_and_the_seam_fiction_is_gone()
    {
        var markup = Render<AboutDataPage>().Markup;
        Assert.Contains("pricecharting.com", markup);
        Assert.DoesNotContain("April 2025", markup);
        Assert.DoesNotContain("Apr ’25", markup);
        Assert.DoesNotContain("sale counts", markup);
    }

    [Fact]
    public void The_floor_section_states_the_date_and_the_reason()
    {
        var markup = Render<AboutDataPage>().Markup;
        Assert.Contains("1 September 2026", markup);
        Assert.Contains("deliberate cutoff", markup);
    }

    [Fact]
    public void The_five_sufficiency_states_print_and_no_authored_unlock_dates_do()
    {
        var markup = Render<AboutDataPage>().Markup;
        foreach (var state in new[] { "OK", "LOW DATA", "LOCKED", "UNDEFINED window", "UNSTABLE FIT" })
        {
            Assert.Contains(state, markup);
        }
        Assert.DoesNotContain("Jan 2027", markup);
    }

    [Fact]
    public void Eight_pills_anchor_eight_sections()
    {
        var cut = Render<AboutDataPage>();
        var hrefs = cut.FindAll(".pill-row a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Equal(8, hrefs.Count);
        foreach (var id in new[]
        {
            "sources", "holdings", "cannot-know", "pooled-grades",
            "freshness", "floor", "honesty", "disclaimers",
        })
        {
            Assert.Contains($"about-data#{id}", hrefs);
            Assert.NotNull(cut.Find($"#{id}"));
        }
    }

    [Fact]
    public void The_restatement_promise_stops_at_what_is_built()
    {
        var markup = Render<AboutDataPage>().Markup;
        Assert.Contains("We never rewrite history", markup);
        Assert.DoesNotContain("mark the affected window on charts", markup);
    }
}
