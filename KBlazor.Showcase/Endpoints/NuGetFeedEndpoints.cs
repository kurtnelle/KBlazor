using KBlazor.Showcase.Services;

namespace KBlazor.Showcase.Endpoints;

public static class NuGetFeedEndpoints
{
    /// <summary>
    /// Maps the NuGet v3 feed endpoints. The feed is accessible at /v3/index.json
    /// and serves packages from wwwroot/packages.
    /// </summary>
    public static WebApplication MapNuGetFeedEndpoints(this WebApplication app)
    {
        // Service Index — the entry point for NuGet clients
        app.MapGet("/v3/index.json", (HttpRequest request, NuGetFeedService feed) =>
        {
            var baseUrl = GetBaseUrl(request);
            return Results.Json(feed.GetServiceIndex(baseUrl));
        });

        // Registration Index — package metadata
        app.MapGet("/v3/registration/{id}/index.json", (string id, HttpRequest request, NuGetFeedService feed) =>
        {
            var baseUrl = GetBaseUrl(request);
            var result = feed.GetRegistrationIndex(baseUrl, id);
            return result is null ? Results.NotFound() : Results.Json(result);
        });

        // Flat Container — version list
        app.MapGet("/v3/flat/{id}/index.json", (string id, NuGetFeedService feed) =>
        {
            var result = feed.GetFlatContainerVersions(id);
            return result is null ? Results.NotFound() : Results.Json(result);
        });

        // Flat Container — package download
        app.MapGet("/v3/flat/{id}/{version}/{filename}", (string id, string version, string filename, NuGetFeedService feed) =>
        {
            var filePath = feed.GetPackageFilePath(id, version);
            return filePath is null
                ? Results.NotFound()
                : Results.File(filePath, "application/octet-stream", filename);
        });

        // Search
        app.MapGet("/v3/search", (string? q, HttpRequest request, NuGetFeedService feed) =>
        {
            var baseUrl = GetBaseUrl(request);
            return Results.Json(feed.GetSearchResults(baseUrl, q));
        });

        return app;
    }

    private static string GetBaseUrl(HttpRequest request)
    {
        // Use the Host header so it works correctly behind nuget.kblazor.com CNAME
        return $"{request.Scheme}://{request.Host}";
    }
}
