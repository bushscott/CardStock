using CardStock.Application.Catalog;

namespace CardStock.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1");

        api.MapGet("/sets/{id:long}", async (
            long id, ISetPageReader reader, CancellationToken ct) =>
        {
            var snapshot = await reader.GetAsync(id, ct);
            return snapshot is null ? NotFound() : Results.Ok(CatalogMappers.ToDto(snapshot));
        });

        return routes;
    }

    private static IResult NotFound() => Results.Problem(
        title: "No such entry",
        statusCode: StatusCodes.Status404NotFound,
        extensions: new Dictionary<string, object?> { ["reason"] = "unknown" });
}
