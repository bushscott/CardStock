using Microsoft.AspNetCore.RateLimiting;

namespace CardStock.Api.Cards;

public static class RefreshEndpoint
{
    public static IEndpointRouteBuilder MapRefreshEndpoint(this IEndpointRouteBuilder routes)
    {
        var cards = routes.MapGroup("/api/v1/cards");

        cards.MapPost("/{id:long}/refresh", async (
            long id, IHttpClientFactory clients, CancellationToken ct) =>
        {
            var client = clients.CreateClient("worker-intake");
            try
            {
                using var response = await client.PostAsync($"/cards/{id}/express-visit", content: null, ct);
                return Results.StatusCode((int)response.StatusCode);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                // The worker is unreachable or the 65s cap fired: to the badge machine
                // this is indistinguishable from a 502, so say 502.
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }
        }).RequireRateLimiting("express-refresh");

        return routes;
    }
}
