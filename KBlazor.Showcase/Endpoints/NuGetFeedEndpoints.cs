using KBlazor.Showcase.Services;

namespace KBlazor.Showcase.Endpoints;

public static class NuGetFeedEndpoints
{
    private static string BaseUrl(HttpRequest req)
    {
        var scheme = req.Scheme;
        var host = req.Host.ToString();
        return $"{scheme}://{host}";
    }

    public static WebApplication MapNuGetFeedEndpoints(this WebApplication app)
    {
        // Service index
        app.MapGet("/v3/index.json", (HttpRequest req, NuGetFeedService svc) =>
        {
            return Results.Json(svc.GetServiceIndex(BaseUrl(req)));
        });

        // Registration index
        app.MapGet("/v3/registration/{id}/index.json", (string id, HttpRequest req, NuGetFeedService svc) =>
        {
            var result = svc.GetRegistrationIndex(BaseUrl(req), id);
            return result is null ? Results.NotFound() : Results.Json(result);
        });

        // Flat container — version list
        app.MapGet("/v3/flatcontainer/{id}/index.json", (string id, NuGetFeedService svc) =>
        {
            var result = svc.GetFlatContainerVersions(id);
            return result is null ? Results.NotFound() : Results.Json(result);
        });

        // Flat container — package download
        app.MapGet("/v3/flatcontainer/{id}/{version}/{filename}", (string id, string version, string filename, NuGetFeedService svc) =>
        {
            var path = svc.GetPackageFilePath(id, version);
            if (path is null || !File.Exists(path))
                return Results.NotFound();

            return Results.File(path, "application/octet-stream", Path.GetFileName(path));
        });

        // Search
        app.MapGet("/v3/search", (string? q, int? skip, int? take, HttpRequest req, NuGetFeedService svc) =>
        {
            var result = svc.GetSearchResults(BaseUrl(req), q, skip ?? 0, take ?? 20);
            return Results.Json(result);
        });

        return app;
    }
}
