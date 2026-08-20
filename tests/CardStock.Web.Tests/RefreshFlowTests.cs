using System.Net;
using System.Net.Http.Json;
using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;
using CardStock.Web.Layout;
using CardStock.Web.Pages;
using CardStock.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace CardStock.Web.Tests;

/// <summary>
/// card.md §4.2.1 / D-062 / D-077: the refresh flow's badge-state machine, and the
/// page's full assembly. Fixed clock throughout -- 2026-08-12, matching the day-count
/// worked example D-077 itself gives ("as of 8 Aug ... 3d old") in spirit, and the
/// brief's own fixture (visited 2026-08-01, now 2026-08-12 -> 11d old).
/// </summary>
public class RefreshFlowTests : BunitContext
{
    private const long CardId = 630417;
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    public RefreshFlowTests()
    {
        // CardPage mounts PriceChart, which drives lwcInterop.* on every render.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<TimeProvider>(new FixedClock(Now));
    }

    [Fact]
    public async Task Stale_card_refreshes_exactly_once_then_lands_the_second_snapshot_in_place()
    {
        var refreshGate = new TaskCompletionSource<HttpResponseMessage>();
        var refreshCalls = 0;
        var snapshotCalls = 0;
        var salesCalls = 0;

        var staleVisited = Now - TimeSpan.FromDays(3);
        var staleSnapshot = Fixtures.Snapshot(cardId: CardId, lastVisitedAt: staleVisited);
        var landedSnapshot = Fixtures.Snapshot(cardId: CardId, lastVisitedAt: Now);

        var handler = new ScriptedHandler(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post && path.EndsWith("/refresh"))
            {
                Interlocked.Increment(ref refreshCalls);
                return await refreshGate.Task;
            }

            if (path.EndsWith("/sales"))
            {
                Interlocked.Increment(ref salesCalls);
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(Array.Empty<SaleDto>()) };
            }

            var call = Interlocked.Increment(ref snapshotCalls);
            var body = call == 1 ? staleSnapshot : landedSnapshot;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
        });

        RegisterClient(handler);

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        // Fetching: the stored (stale) as-of date is still what's on screen -- a fetch in
        // flight never changes what's already painted (D-077).
        cut.WaitForAssertion(() =>
            Assert.Contains("Checking for a newer price", cut.Find(".refresh-badge-fetching").TextContent));
        Assert.Equal(1, refreshCalls);
        Assert.Contains(CardStock.Domain.Dates.Full(staleVisited), cut.Markup);

        refreshGate.SetResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Landed: refetched snapshot painted in place (the footer's as-of date moves to
        // "now"), badge slot empties, and the refresh POST never fires a second time.
        cut.WaitForAssertion(() => Assert.Contains(CardStock.Domain.Dates.Full(Now), cut.Markup));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".refresh-badge")));
        Assert.Equal(1, refreshCalls);
        Assert.Equal(2, snapshotCalls);
        Assert.Equal(2, salesCalls);
    }

    [Fact]
    public async Task Stale_card_that_fails_to_refresh_shows_the_amber_day_count()
    {
        var visited = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var snapshot = Fixtures.Snapshot(cardId: CardId, lastVisitedAt: visited);

        var handler = new ScriptedHandler(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post && path.EndsWith("/refresh"))
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway); // 502
            }

            if (path.EndsWith("/sales"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(Array.Empty<SaleDto>()) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(snapshot) };
        });

        RegisterClient(handler);

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".refresh-badge-failed")));
        Assert.Equal("– as of 08-01-2026 · 11d old", cut.Find(".refresh-badge-failed").TextContent);

        // The prices were never wrong, only old -- the failure changes nothing else on screen.
        Assert.Equal("Umbreon VMAX (Alt Art)", cut.Find("h1").TextContent);
    }

    [Fact]
    public async Task Fresh_card_never_calls_refresh()
    {
        var visited = Now - TimeSpan.FromHours(2); // well within the 24h floor
        var snapshot = Fixtures.Snapshot(cardId: CardId, lastVisitedAt: visited);
        var refreshCalls = 0;

        var handler = new ScriptedHandler(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post && path.EndsWith("/refresh"))
            {
                Interlocked.Increment(ref refreshCalls);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (path.EndsWith("/sales"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(Array.Empty<SaleDto>()) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(snapshot) };
        });

        RegisterClient(handler);

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        cut.WaitForAssertion(() => Assert.Equal("Umbreon VMAX (Alt Art)", cut.Find("h1").TextContent));

        // There is no "eventually true" signal for an absence -- give any wrongly-fired
        // background call a beat to land before asserting silence.
        await Task.Delay(50);

        Assert.Equal(0, refreshCalls);
        Assert.Empty(cut.FindAll(".refresh-badge"));
    }

    [Fact]
    public async Task Never_visited_card_refreshes_and_reports_never_visited_on_failure()
    {
        var snapshot = Fixtures.Snapshot(cardId: CardId, lastVisitedAt: null);
        var refreshCalls = 0;

        var handler = new ScriptedHandler(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post && path.EndsWith("/refresh"))
            {
                Interlocked.Increment(ref refreshCalls);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError); // 500
            }

            if (path.EndsWith("/sales"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(Array.Empty<SaleDto>()) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(snapshot) };
        });

        RegisterClient(handler);

        var cut = Render<CardPage>(p => p.Add(x => x.Id, CardId));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".refresh-badge-failed")));
        Assert.Equal("– never visited", cut.Find(".refresh-badge-failed").TextContent);
        Assert.Equal(1, refreshCalls);
    }

    [Fact]
    public void Badge_slot_is_a_full_width_row_beneath_row_a_and_above_the_tier_strip()
    {
        var identity = new IdentityDto(
            "Umbreon VMAX (Alt Art)", "215", null, SetId: 7, "Evolving Skies",
            [new SpeciesRefDto("Umbreon", "umbreon")], HasImage: true, DelistedAt: null);
        var prices = new PricesDto("2026-08", []);

        var cut = Render<IdentityHeader>(p => p
            .Add(x => x.Identity, identity)
            .Add(x => x.CardId, CardId)
            .Add(x => x.Prices, prices)
            .Add(x => x.Signals, new SignalsDto(0, 0, 0, 0, 0, []))
            .Add(x => x.BadgeSlot, "<span class=\"probe\">badge</span>"));

        // D-097: the reservation lives under the action buttons inside row-a --
        // the dedicated full-width row surrendered its height so the header
        // reads as one solid block. D-077's invariant survives as a fixed-height
        // CSS slot: the badge appearing must never reflow the tiles.
        var badge = cut.Find(".badge-slot");
        Assert.Contains("row-a-actions", badge.ParentElement!.ClassName ?? "");
        Assert.Single(cut.FindAll(".probe"));

        // The right column is now exactly two rows: row-a, then tiles+signals.
        var classNames = cut.Find(".right-col").Children
            .Select(c => c.ClassName ?? string.Empty).ToList();
        Assert.Equal(2, classNames.Count);
        Assert.Contains("row-a", classNames[0]);
        Assert.Contains("tiles-and-signals", classNames[1]);
    }

    [Fact]
    public void Every_panel_mounts_from_one_fixture_snapshot()
    {
        var snapshot = Fixtures.Snapshot(cardId: CardId, lastVisitedAt: Now - TimeSpan.FromHours(1)); // fresh: no refresh noise

        var handler = new ScriptedHandler(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/sales"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(Array.Empty<SaleDto>()) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(snapshot) };
        });

        RegisterClient(handler);

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, CardPageFragment(CardId)));

        cut.WaitForAssertion(() => Assert.Equal("Umbreon VMAX (Alt Art)", cut.Find("h1").TextContent));

        Assert.NotEmpty(cut.FindAll(".app-chrome"));       // chrome
        Assert.NotEmpty(cut.FindAll(".breadcrumb"));        // breadcrumb
        Assert.NotEmpty(cut.FindAll(".card-identity-panel")); // identity
        Assert.NotEmpty(cut.FindAll(".tier-strip"));        // strip
        Assert.NotEmpty(cut.FindAll(".price-chart"));       // chart
        Assert.NotEmpty(cut.FindAll(".ledger-panel"));      // ledger
        Assert.NotEmpty(cut.FindAll(".census-pair"));       // census pair (container)
        Assert.NotEmpty(cut.FindAll(".census-panel"));      // census pair (population half)
        Assert.NotEmpty(cut.FindAll(".activity-panel"));    // census pair (grading-activity half)
        Assert.NotEmpty(cut.FindAll(".freshness-footer"));  // footer
    }

    private static RenderFragment CardPageFragment(long id) => builder =>
    {
        builder.OpenComponent<CardPage>(0);
        builder.AddAttribute(1, nameof(CardPage.Id), id);
        builder.CloseComponent();
    };

    private void RegisterClient(HttpMessageHandler handler) =>
        Services.AddScoped(_ => new CardApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

/// <summary>A handler whose response can be scripted per call -- branch on the request,
/// count calls, or gate a response behind a TaskCompletionSource to pause mid-flow and
/// assert on a transient state before releasing it.</summary>
internal sealed class ScriptedHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        respond(request);
}
