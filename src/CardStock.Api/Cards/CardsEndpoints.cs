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
                return Results.Problem(
                    title: "No such card",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?>
                    {
                        ["reason"] = identity is null ? "unknown" : "not_a_card",
                    });
            }

            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            var chips = ChipEngine.Evaluate(pricesTask.Result!, currentMonth);

            return Results.Ok(CardPageMapper.ToDto(
                identity, pricesTask.Result!, censusTask.Result, chips, currentMonth));
        });

        return routes;
    }
}
