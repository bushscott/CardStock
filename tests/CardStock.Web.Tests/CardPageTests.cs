using System.Net;
using System.Net.Http.Json;
using Bunit;
using CardStock.Web.Pages;
using CardStock.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CardStock.Web.Tests;

public class CardPageTests : BunitContext
{
    private const long CardId = 630417;

    public CardPageTests()
    {
        // CardPage now mounts PriceChart, which drives lwcInterop.* on every render; loose mode
        // auto-satisfies those calls so this file's identity/error-path tests (which don't care
        // about the chart) don't need to configure every JS call by hand.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_the_title_and_subline_on_a_successful_snapshot()
    {
        RegisterClient(RespondingWith(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/sales")
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<object>()) }
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(Fixtures.Snapshot(
                        cardId: CardId, title: "Umbreon VMAX (Alt Art)", setName: "Evolving Skies",
                        collectorNumber: "215")),
                }));

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        cut.WaitForAssertion(() => Assert.Equal("Umbreon VMAX (Alt Art)", cut.Find("h1").TextContent));
        Assert.Contains("Evolving Skies", cut.Find(".subline").TextContent);
        Assert.Contains("#215", cut.Find(".subline").TextContent);
    }

    [Theory]
    [InlineData("unknown", "No card with id 630417.")]
    [InlineData("not_a_card", "Id 630417 isn't a card.")]
    public void Renders_the_not_found_copy_for_each_reason(string reason, string expectedCopy)
    {
        RegisterClient(RespondingWith(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new Dictionary<string, object> { ["reason"] = reason }),
            }));

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        cut.WaitForAssertion(() => Assert.Contains(expectedCopy, cut.Markup));
    }

    [Fact]
    public void Shows_the_error_view_and_issues_a_second_request_on_retry()
    {
        var requestCount = 0;
        RegisterClient(RespondingWith(_ =>
        {
            Interlocked.Increment(ref requestCount);
            throw new HttpRequestException("unreachable");
        }));

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        cut.WaitForAssertion(() => Assert.Contains("Couldn't reach the data service.", cut.Markup));
        var requestsAfterFirstLoad = requestCount;

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.True(requestCount > requestsAfterFirstLoad));
        Assert.Contains("Couldn't reach the data service.", cut.Markup);
    }

    private static HttpClient RespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri("http://localhost/") };

    private void RegisterClient(HttpClient http) =>
        Services.AddScoped(_ => new CardApiClient(http));
}
