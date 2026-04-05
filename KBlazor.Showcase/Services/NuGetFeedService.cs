using System.IO.Compression;
using System.Xml.Linq;

namespace KBlazor.Showcase.Services;

/// <summary>
/// Reads .nupkg files from wwwroot/packages and serves NuGet v3 protocol responses.
/// </summary>
public class NuGetFeedService
{
    private readonly string _packagesPath;

    public NuGetFeedService(IWebHostEnvironment env)
    {
        _packagesPath = Path.Combine(env.WebRootPath, "packages");
    }

    // ── Service Index (/v3/index.json) ──────────────────────────────────
    public object GetServiceIndex(string baseUrl)
    {
        return new Dictionary<string, object>
        {
            ["version"] = "3.0.0",
            ["resources"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/registration/",
                    ["@type"] = "RegistrationsBaseUrl/3.6.0",
                    ["comment"] = "Package registration base URL"
                },
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/flat/",
                    ["@type"] = "PackageBaseAddress/3.0.0",
                    ["comment"] = "Package content base URL"
                },
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/search",
                    ["@type"] = "SearchQueryService/3.5.0",
                    ["comment"] = "Search query service"
                }
            }
        };
    }

    // ── Registration Index (/v3/registration/{id}/index.json) ───────────
    public object? GetRegistrationIndex(string baseUrl, string packageId)
    {
        var versions = GetPackageVersions(packageId);
        if (versions.Count == 0) return null;

        var items = versions.Select(v =>
        {
            var meta = ReadNuspec(packageId, v);
            return new Dictionary<string, object>
            {
                ["@id"] = $"{baseUrl}/v3/registration/{packageId.ToLowerInvariant()}/{v}.json",
                ["catalogEntry"] = new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/registration/{packageId.ToLowerInvariant()}/{v}.json",
                    ["id"] = meta.Id,
                    ["version"] = v,
                    ["description"] = meta.Description,
                    ["authors"] = meta.Authors,
                    ["listed"] = true
                },
                ["packageContent"] = $"{baseUrl}/v3/flat/{packageId.ToLowerInvariant()}/{v}/{packageId.ToLowerInvariant()}.{v}.nupkg"
            };
        }).ToArray();

        return new Dictionary<string, object>
        {
            ["count"] = 1,
            ["items"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/registration/{packageId.ToLowerInvariant()}/index.json",
                    ["count"] = items.Length,
                    ["items"] = items,
                    ["lower"] = versions.First(),
                    ["upper"] = versions.Last()
                }
            }
        };
    }

    // ── Flat Container Versions (/v3/flat/{id}/index.json) ──────────────
    public object? GetFlatContainerVersions(string packageId)
    {
        var versions = GetPackageVersions(packageId);
        if (versions.Count == 0) return null;
        return new { versions };
    }

    // ── Package File Path (for download) ────────────────────────────────
    public string? GetPackageFilePath(string packageId, string version)
    {
        var file = Path.Combine(_packagesPath, $"{packageId}.{version}.nupkg");
        return File.Exists(file) ? file : null;
    }

    // ── Search (/v3/search?q=...) ───────────────────────────────────────
    public object GetSearchResults(string baseUrl, string? query)
    {
        var allPackages = GetAllPackageIds();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? allPackages
            : allPackages.Where(id => id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        var data = filtered.Select(id =>
        {
            var versions = GetPackageVersions(id);
            var latest = versions.LastOrDefault() ?? "0.0.0";
            var meta = ReadNuspec(id, latest);
            return new Dictionary<string, object>
            {
                ["@id"] = $"{baseUrl}/v3/registration/{id.ToLowerInvariant()}/index.json",
                ["id"] = meta.Id,
                ["version"] = latest,
                ["description"] = meta.Description,
                ["authors"] = new[] { meta.Authors },
                ["versions"] = versions.Select(v => new Dictionary<string, object>
                {
                    ["version"] = v,
                    ["@id"] = $"{baseUrl}/v3/registration/{id.ToLowerInvariant()}/{v}.json"
                }).ToArray()
            };
        }).ToArray();

        return new { totalHits = data.Length, data };
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private List<string> GetAllPackageIds()
    {
        if (!Directory.Exists(_packagesPath)) return new();
        return Directory.GetFiles(_packagesPath, "*.nupkg")
            .Select(f => ExtractPackageId(Path.GetFileNameWithoutExtension(f)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<string> GetPackageVersions(string packageId)
    {
        if (!Directory.Exists(_packagesPath)) return new();
        return Directory.GetFiles(_packagesPath, $"{packageId}.*.nupkg")
            .Select(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                return name[(packageId.Length + 1)..];
            })
            .OrderBy(v => v)
            .ToList();
    }

    private (string Id, string Description, string Authors) ReadNuspec(string packageId, string version)
    {
        var nupkgPath = Path.Combine(_packagesPath, $"{packageId}.{version}.nupkg");
        if (!File.Exists(nupkgPath))
            return (packageId, string.Empty, string.Empty);

        try
        {
            using var zip = ZipFile.OpenRead(nupkgPath);
            var nuspecEntry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry == null) return (packageId, string.Empty, string.Empty);

            using var stream = nuspecEntry.Open();
            var doc = XDocument.Load(stream);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            var metadata = doc.Root?.Element(ns + "metadata");

            return (
                Id: metadata?.Element(ns + "id")?.Value ?? packageId,
                Description: metadata?.Element(ns + "description")?.Value ?? string.Empty,
                Authors: metadata?.Element(ns + "authors")?.Value ?? string.Empty
            );
        }
        catch
        {
            return (packageId, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Extracts the package ID from a filename like "KBlazor.1.0.0" by splitting at the first
    /// segment that starts with a digit.
    /// </summary>
    private static string ExtractPackageId(string fileNameWithoutExt)
    {
        var parts = fileNameWithoutExt.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0 && char.IsDigit(parts[i][0]))
            {
                return string.Join('.', parts[..i]);
            }
        }
        return fileNameWithoutExt;
    }
}
