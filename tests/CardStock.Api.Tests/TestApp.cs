using CardStock.Application.Cards;
using CardStock.Application.Prices;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;
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

    /// <summary>A fresh temp directory per test instance, wired as ImageStore:Directory.</summary>
    public string ImageDirectory { get; } =
        Directory.CreateTempSubdirectory("cardstock-image-tests-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:CardStock",
            "Host=localhost;Database=never_used;Username=x;Password=x");
        builder.UseSetting("ImageStore:Directory", ImageDirectory);
        builder.UseSetting("Worker:IntakeBaseUrl", "http://127.0.0.1:5155");
        if (ExpressPerHour is not null)
        {
            builder.UseSetting("RateLimits:ExpressPerHour", ExpressPerHour.Value.ToString());
        }

        builder.ConfigureServices(services =>
        {
            services.AddScoped<ICardIdentityReader>(_ => new StubIdentity(this));
            services.AddScoped<ICardPriceReader>(_ => new StubPrices(this));
            services.AddScoped<ICardCensusReader>(_ => new StubCensus(this));
            services.AddScoped<ICardSalesReader>(_ => new StubSales(this));
            if (UtcNow is not null)
            {
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow.Value));
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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
