using CardStock.Application.Cards;
using CardStock.Application.Prices;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CardStock.Api.Tests;

public sealed class TestApp : WebApplicationFactory<Program>
{
    public CardIdentity? Identity { get; set; }
    public CardPriceSnapshot? Prices { get; set; }
    public CardCensus Census { get; set; } = CardCensus.From([], []);
    public IReadOnlyList<LedgerSale> Sales { get; set; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:CardStock",
            "Host=localhost;Database=never_used;Username=x;Password=x");
        builder.ConfigureServices(services =>
        {
            services.AddScoped<ICardIdentityReader>(_ => new StubIdentity(this));
            services.AddScoped<ICardPriceReader>(_ => new StubPrices(this));
            services.AddScoped<ICardCensusReader>(_ => new StubCensus(this));
            services.AddScoped<ICardSalesReader>(_ => new StubSales(this));
        });
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
}
