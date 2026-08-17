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

        api.MapGet("/characters/{slug}", async (
            string slug, ICharacterPageReader reader, CancellationToken ct) =>
        {
            var snapshot = await reader.GetAsync(slug, ct);
            return snapshot is null ? NotFound() : Results.Ok(CatalogMappers.ToDto(snapshot));
        });

        // The card-image endpoint's shape (CardsEndpoints.cs): disk is the fact.
        // The id is an int route constraint, so no traversal-shaped value can
        // reach Path.Combine.
        api.MapGet("/species/{id:int}/icon", (
            int id, IConfiguration configuration, HttpContext httpContext) =>
        {
            var directory = configuration["SpeciesIcons:Directory"]
                ?? throw new InvalidOperationException("SpeciesIcons:Directory is not configured.");
            var path = Path.Combine(directory, $"{id}.png");
            if (!File.Exists(path))
            {
                return Results.NotFound();
            }

            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(path, "image/png");
        });

        api.MapGet("/browse/sets", async (IBrowseReader reader, CancellationToken ct) =>
            Results.Ok(CatalogMappers.ToDto(await reader.GetSetsAsync(ct))));

        api.MapGet("/browse/species", async (IBrowseReader reader, CancellationToken ct) =>
            Results.Ok(CatalogMappers.ToDto(await reader.GetSpeciesAsync(ct))));

        return routes;
    }

    private static IResult NotFound() => Results.Problem(
        title: "No such entry",
        statusCode: StatusCodes.Status404NotFound,
        extensions: new Dictionary<string, object?> { ["reason"] = "unknown" });
}
