using System.Net;
using System.Threading.RateLimiting;
using CardStock.Api.Cards;
using CardStock.Api.Catalog;
using CardStock.Api.Security;
using CardStock.Application.Cards;
using CardStock.Application.Catalog;
using CardStock.Application.Prices;
using CardStock.Infrastructure.Cards;
using CardStock.Infrastructure.Catalog;
using CardStock.Infrastructure.Persistence;
using CardStock.Infrastructure.Prices;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// AddDbContextFactory also registers CardStockDbContext itself as a scoped
// service that resolves via the factory, so /healthz/data below is unchanged.
builder.Services.AddDbContextFactory<CardStockDbContext>(options =>
    options.UseCardStock(builder.Configuration.GetConnectionString("CardStock")
        ?? throw new InvalidOperationException("ConnectionStrings:CardStock is not configured.")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICardIdentityReader, CardIdentityReader>();
builder.Services.AddScoped<ICardPriceReader, CardPriceReader>();
builder.Services.AddScoped<ICardCensusReader, CardCensusReader>();
builder.Services.AddScoped<ICardSalesReader, CardSalesReader>();
builder.Services.AddScoped<ISetPageReader, SetPageReader>();
builder.Services.AddScoped<ICharacterPageReader, CharacterPageReader>();
builder.Services.AddScoped<IBrowseReader, BrowseReader>();

// Corpus-wide latest-PSA-10 measured 1,427 ms cold on the Pi (2026-08-15) --
// far too slow per page load, so it computes once behind a short TTL and
// serves from memory. Singleton: corpus state, not user session state.
var aggregateTtl = TimeSpan.FromMinutes(
    builder.Configuration.GetValue("Catalog:AggregateCacheMinutes", 5));
builder.Services.AddSingleton<ICatalogAggregates>(sp => new CatalogAggregateCache(
    sp.GetRequiredService<IDbContextFactory<CardStockDbContext>>(),
    sp.GetRequiredService<TimeProvider>(), aggregateTtl));

// Timeout outlives the worker's own 60s upstream cap, so the worker always
// answers first (D-076) and we never guess at a status it hasn't yet given.
builder.Services.AddHttpClient("worker-intake", client =>
{
    var baseUrl = builder.Configuration["Worker:IntakeBaseUrl"]
        ?? throw new InvalidOperationException("Worker:IntakeBaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(65);
});

// Per-IP is D-084.1's pre-auth adaptation of D-062's per-account intent: there is
// no login yet to key on. The tunnel delivers every public request from cloudflared on
// loopback, with the real visitor in CF-Connecting-IP (Cloudflare overwrites client-
// supplied values at its edge). Trust exactly the loopback proxy and nothing else:
// direct LAN/VPN connections arrive from non-proxy addresses and keep their own socket
// IP, and a forged header from one is ignored (D-132 §7).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardedForHeaderName = "CF-Connecting-IP";
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});

var expressPerHour = builder.Configuration.GetValue("RateLimits:ExpressPerHour", 300);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("express-refresh", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress ?? IPAddress.None,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = expressPerHour,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
            }));
});

builder.Services.Configure<SecurityHeaderOptions>(builder.Configuration.GetSection("Security"));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSecurityHeaders();
app.UseRateLimiter();

// No Database.Migrate() here, and none in the Worker either. Migrations are a
// deliberate act run by a human from a dev machine (ADR-0001), which also means
// two units cannot race one history table at boot.

app.MapGet("/healthz", () => Results.Ok("ok"));

// Proves the deployed application can read the crawler's schema with the grants
// it actually holds -- the one thing no local test can confirm, because the
// throwaway test databases are owned by cardstock_tester and carry none of the
// production grants.
app.MapGet("/healthz/data", async (CardStockDbContext db) => Results.Ok(new
{
    cards = await db.ScraperCards.LongCountAsync(),
    sets = await db.ScraperSets.LongCountAsync(),
    populations = await db.ScraperPopulations.LongCountAsync(),
    sales = await db.ScraperSales.LongCountAsync(),
}));

app.MapCardEndpoints();
app.MapCatalogEndpoints();
app.MapRefreshEndpoint();

// The WASM app's own static assets and its host page fallback. Kept after the
// API routes so a request for a mapped API path is never shadowed by these.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
