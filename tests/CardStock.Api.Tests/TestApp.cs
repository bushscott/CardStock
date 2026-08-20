using System.Net;
using CardStock.Application.Cards;
using CardStock.Application.Catalog;
using CardStock.Application.Prices;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace CardStock.Api.Tests;

public sealed class TestApp : WebApplicationFactory<Program>
{
    public CardIdentity? Identity { get; set; }
    public CardPriceSnapshot? Prices { get; set; }
    public CardCensus Census { get; set; } = CardCensus.From([]);
    public IReadOnlyList<LedgerSale> Sales { get; set; } = [];

    /// <summary>Replaces the "worker-intake" named client's primary handler, so tests
    /// never make a real HTTP call.</summary>
    public HttpMessageHandler? WorkerIntakeHandler { get; set; }

    /// <summary>Fixes the endpoint's clock when set — the signals panel's volume and
    /// churn rows count days from it.</summary>
    public DateTimeOffset? UtcNow { get; set; }

    /// <summary>Overrides RateLimits:ExpressPerHour for a single test's host.</summary>
    public int? ExpressPerHour { get; set; }

    /// <summary>Fakes the socket-level client address when set — TestServer
    /// connections otherwise have none. Runs before the app's own pipeline,
    /// so forwarded-headers trust checks see it as the connection IP.</summary>
    public IPAddress? RemoteIp { get; set; }

    /// <summary>Overrides Security:HstsMaxAgeSeconds (0 = HSTS off, the default).</summary>
    public int? HstsMaxAgeSeconds { get; set; }

    public bool HstsIncludeSubdomains { get; set; }

    /// <summary>When set, becomes the host's webroot — drop an index.html here to
    /// exercise the CSP hash computation.</summary>
    public string? WebRoot { get; set; }

    /// <summary>Overrides the host environment (the factory defaults to Development,
    /// where static web assets shadow a physical WebRoot override).</summary>
    public string? EnvironmentName { get; set; }

    /// <summary>Overrides AllowedHosts — dev/tests default to "*".</summary>
    public string? AllowedHosts { get; set; }

    public SetPageSnapshot? SetSnapshot { get; set; }

    public CharacterPageSnapshot? CharacterSnapshot { get; set; }

    public IReadOnlyList<SetTile> BrowseSets { get; set; } = [];

    public IReadOnlyList<SpeciesTile> BrowseSpecies { get; set; } = [];

    /// <summary>A fresh temp directory per test instance, wired as ImageStore:Directory.</summary>
    public string ImageDirectory { get; } =
        Directory.CreateTempSubdirectory("cardstock-image-tests-").FullName;

    public string SpeciesIconDirectory { get; } =
        Directory.CreateTempSubdirectory("cardstock-icon-tests-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:CardStock",
            "Host=localhost;Database=never_used;Username=x;Password=x");
        builder.UseSetting("ImageStore:Directory", ImageDirectory);
        builder.UseSetting("SpeciesIcons:Directory", SpeciesIconDirectory);
        builder.UseSetting("Worker:IntakeBaseUrl", "http://127.0.0.1:5155");
        if (ExpressPerHour is not null)
        {
            builder.UseSetting("RateLimits:ExpressPerHour", ExpressPerHour.Value.ToString());
        }

        if (HstsMaxAgeSeconds is not null)
        {
            builder.UseSetting("Security:HstsMaxAgeSeconds", HstsMaxAgeSeconds.Value.ToString());
            builder.UseSetting("Security:HstsIncludeSubdomains", HstsIncludeSubdomains.ToString());
        }

        if (WebRoot is not null)
        {
            builder.UseSetting(WebHostDefaults.WebRootKey, WebRoot);
        }

        if (EnvironmentName is not null)
        {
            builder.UseEnvironment(EnvironmentName);
        }

        if (AllowedHosts is not null)
        {
            builder.UseSetting("AllowedHosts", AllowedHosts);
        }

        builder.ConfigureServices(services =>
        {
            services.AddScoped<ICardIdentityReader>(_ => new StubIdentity(this));
            services.AddScoped<ICardPriceReader>(_ => new StubPrices(this));
            services.AddScoped<ICardCensusReader>(_ => new StubCensus(this));
            services.AddScoped<ICardSalesReader>(_ => new StubSales(this));
            services.AddScoped<ISetPageReader>(_ => new StubSetPage(this));
            services.AddScoped<ICharacterPageReader>(_ => new StubCharacter(this));
            services.AddScoped<IBrowseReader>(_ => new StubBrowse(this));
            if (UtcNow is not null)
            {
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow.Value));
            }

            if (RemoteIp is not null)
            {
                services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(RemoteIp));
            }

            var handler = WorkerIntakeHandler;
            if (handler is not null)
            {
                services.Configure<HttpClientFactoryOptions>("worker-intake", options =>
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = handler));
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                Directory.Delete(ImageDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; the OS reclaims the temp dir eventually regardless.
            }

            try
            {
                Directory.Delete(SpeciesIconDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; the OS reclaims the temp dir eventually regardless.
            }
        }
    }

    private sealed class StubIdentity(TestApp app) : ICardIdentityReader
    {
        public Task<CardIdentity?> GetAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(app.Identity);
    }

    private sealed class StubPrices(TestApp app) : ICardPriceReader
    {
        public Task<CardPriceSnapshot?> GetAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(app.Prices);
    }

    private sealed class StubCensus(TestApp app) : ICardCensusReader
    {
        public Task<CardCensus> GetAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(app.Census);
    }

    private sealed class StubSales(TestApp app) : ICardSalesReader
    {
        public Task<IReadOnlyList<LedgerSale>> GetAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(app.Sales);
    }

    private sealed class StubSetPage(TestApp app) : ISetPageReader
    {
        public Task<SetPageSnapshot?> GetAsync(long setId, CancellationToken ct = default) =>
            Task.FromResult(app.SetSnapshot?.SetId == setId ? app.SetSnapshot : null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubCharacter(TestApp app) : ICharacterPageReader
    {
        public Task<CharacterPageSnapshot?> GetAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(app.CharacterSnapshot?.Slug == slug ? app.CharacterSnapshot : null);
    }

    private sealed class StubBrowse(TestApp app) : IBrowseReader
    {
        public Task<IReadOnlyList<SetTile>> GetSetsAsync(CancellationToken ct = default) =>
            Task.FromResult(app.BrowseSets);

        public Task<IReadOnlyList<SpeciesTile>> GetSpeciesAsync(CancellationToken ct = default) =>
            Task.FromResult(app.BrowseSpecies);
    }

    private sealed class RemoteIpStartupFilter(IPAddress ip) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = ip;
                    return nextMiddleware();
                });
                next(app);
            };
    }
}
