using CardStock.Application.Cards;
using CardStock.Application.Prices;
using CardStock.Domain.Signals;

namespace CardStock.Api.Cards;

public static class CardsEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder routes)
    {
        var cards = routes.MapGroup("/api/v1/cards");

        cards.MapGet("/{id:long}", async (
            long id,
            ICardIdentityReader identityReader,
            ICardPriceReader priceReader,
            ICardCensusReader censusReader,
            TimeProvider time,
            CancellationToken ct) =>
        {
            // Three readers, three connections, one wait (D-084.6).
            var identityTask = identityReader.GetAsync(id, ct);
            var pricesTask = priceReader.GetAsync(id, ct);
            var censusTask = censusReader.GetAsync(id, ct);
            await Task.WhenAll(identityTask, pricesTask, censusTask);

            var identity = identityTask.Result;
            if (identity is null || identity.NotACardAt is not null)
            {
                return NoSuchCard(identity);
            }

            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            var chips = ChipEngine.Evaluate(pricesTask.Result!, currentMonth);

            return Results.Ok(CardPageMapper.ToDto(
                identity, pricesTask.Result!, censusTask.Result, chips, currentMonth));
        });

        cards.MapGet("/{id:long}/sales", async (
            long id,
            ICardIdentityReader identityReader,
            ICardSalesReader salesReader,
            CancellationToken ct) =>
        {
            var identity = await identityReader.GetAsync(id, ct);
            if (identity is null || identity.NotACardAt is not null)
            {
                return NoSuchCard(identity);
            }

            var sales = await salesReader.GetAsync(id, ct);
            return Results.Ok(sales.Select(CardPageMapper.ToDto).ToArray());
        });

        cards.MapGet("/{id:long}/image", async (
            long id,
            ICardIdentityReader identityReader,
            IConfiguration configuration,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var identity = await identityReader.GetAsync(id, ct);
            if (identity is null || identity.NotACardAt is not null || identity.ImageHash is null)
            {
                return Results.NotFound();
            }

            // Defense in depth: the hash is stored, not user-supplied, but a
            // path-traversal-shaped value must never reach Path.Combine below.
            var hash = identity.ImageHash;
            if (!hash.All(char.IsAsciiLetterOrDigit))
            {
                return Results.NotFound();
            }

            var directory = configuration["ImageStore:Directory"]
                ?? throw new InvalidOperationException("ImageStore:Directory is not configured.");
            var path = Path.Combine(directory, hash, "1600.jpg");
            if (!File.Exists(path))
            {
                // The site owes us this image and hasn't been asked for it yet
                // (spec 13.1). The disk is the fact, not the database row.
                return Results.NotFound();
            }

            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(path, "image/jpeg");
        });

        return routes;
    }

    private static IResult NoSuchCard(CardIdentity? identity) =>
        Results.Problem(
            title: "No such card",
            statusCode: StatusCodes.Status404NotFound,
            extensions: new Dictionary<string, object?>
            {
                ["reason"] = identity is null ? "unknown" : "not_a_card",
            });
}
