using CardStock.Api.Cards;
using CardStock.Application.Cards;
using CardStock.Application.Prices;
using CardStock.Infrastructure.Cards;
using CardStock.Infrastructure.Persistence;
using CardStock.Infrastructure.Prices;
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

var app = builder.Build();

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

app.Run();

public partial class Program { }
