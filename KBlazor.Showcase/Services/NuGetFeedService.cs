using System.IO.Compression;
using System.Xml.Linq;

namespace KBlazor.Showcase.Services;

public class NuGetFeedService
{
    private readonly string _packagesPath;
    private List<PackageInfo>? _packages;
    private readonly object _lock = new();

    public NuGetFeedService(IWebHostEnvironment env)
    {
        _packagesPath = Path.Combine(env.WebRootPath, "packages");
        if (!Directory.Exists(_packagesPath))
            Directory.CreateDirectory(_packagesPath);
    }

    private List<PackageInfo> GetPackages()
    {
        if (_packages != null) return _packages;
        lock (_lock)
        {
            if (_packages != null) return _packages;
            _packages = ScanPackages();
            return _packages;
        }
    }

    public void Refresh()
    {
        lock (_lock) { _packages = null; }
    }

    private List<PackageInfo> ScanPackages()
    {
        var result = new List<PackageInfo>();
        foreach (var file in Directory.GetFiles(_packagesPath, "*.nupkg"))
        {
            try
            {
                using var zip = ZipFile.OpenRead(file);
                var nuspecEntry = zip.Entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
                if (nuspecEntry == null) continue;

                using var stream = nuspecEntry.Open();
                var doc = XDocument.Load(stream);
                var ns = doc.Root!.GetDefaultNamespace();
                var meta = doc.Root.Element(ns + "metadata")!;

                var id = meta.Element(ns + "id")?.Value ?? Path.GetFileNameWithoutExtension(file);
                var version = meta.Element(ns + "version")?.Value ?? "0.0.0";
                var description = meta.Element(ns + "description")?.Value ?? "";
                var authors = meta.Element(ns + "authors")?.Value ?? "";
                var projectUrl = meta.Element(ns + "projectUrl")?.Value;
                var tags = meta.Element(ns + "tags")?.Value;

                var depGroups = new List<DependencyGroupInfo>();
                var depsEl = meta.Element(ns + "dependencies");
                if (depsEl != null)
                {
                    foreach (var groupEl in depsEl.Elements(ns + "group"))
                    {
                        var tf = groupEl.Attribute("targetFramework")?.Value ?? "";
                        var deps = groupEl.Elements(ns + "dependency")
                            .Select(d => new PackageDependencyInfo(
                                d.Attribute("id")?.Value ?? "",
                                d.Attribute("version")?.Value ?? ""))
                            .ToList();
                        depGroups.Add(new DependencyGroupInfo(tf, deps));
                    }
                }

                result.Add(new PackageInfo(id, version, description, authors, projectUrl, tags, depGroups, Path.GetFileName(file)));
            }
            catch { /* skip malformed packages */ }
        }
        return result;
    }

    // Helper to create JSON-LD style objects with @id / @type keys
    private static Dictionary<string, object?> Ld(string? id = null, string? type = null)
    {
        var d = new Dictionary<string, object?>();
        if (id != null) d["@id"] = id;
        if (type != null) d["@type"] = type;
        return d;
    }

    public object GetServiceIndex(string baseUrl)
    {
        return new Dictionary<string, object>
        {
            ["version"] = "3.0.0",
            ["resources"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/search",
                    ["@type"] = "SearchQueryService",
                    ["comment"] = "Search packages"
                },
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/search",
                    ["@type"] = "SearchQueryService/3.0.0-beta",
                    ["comment"] = ""
                },
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/registration/",
                    ["@type"] = "RegistrationsBaseUrl",
                    ["comment"] = "Package registration"
                },
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/registration/",
                    ["@type"] = "RegistrationsBaseUrl/3.0.0-beta",
                    ["comment"] = ""
                },
                new Dictionary<string, object>
                {
                    ["@id"] = $"{baseUrl}/v3/flatcontainer/",
                    ["@type"] = "PackageBaseAddress/3.0.0",
                    ["comment"] = "Package content"
                },
            }
        };
    }

    public object? GetRegistrationIndex(string baseUrl, string id)
    {
        var packages = GetPackages()
            .Where(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Version)
            .ToList();

        if (packages.Count == 0) return null;

        var idLower = id.ToLowerInvariant();

        var items = packages.Select(p =>
        {
            var vLower = p.Version.ToLowerInvariant();
            var entryUrl = $"{baseUrl}/v3/registration/{idLower}/{vLower}.json";
            var contentUrl = $"{baseUrl}/v3/flatcontainer/{idLower}/{vLower}/{idLower}.{vLower}.nupkg";

            var depGroups = p.Dependencies.Select(dg =>
            {
                var dgDict = Ld(
                    $"{entryUrl}#dependencygroup/{dg.TargetFramework.ToLowerInvariant()}",
                    "PackageDependencyGroup");
                dgDict["targetFramework"] = dg.TargetFramework;
                dgDict["dependencies"] = dg.Dependencies.Select(d =>
                {
                    var dd = Ld(
                        $"{entryUrl}#dependencygroup/{dg.TargetFramework.ToLowerInvariant()}/{d.Id.ToLowerInvariant()}",
                        "PackageDependency");
                    dd["id"] = d.Id;
                    dd["range"] = d.VersionRange;
                    return dd;
                }).ToArray();
                return dgDict;
            }).ToArray();

            var catalogEntry = Ld(entryUrl);
            catalogEntry["id"] = p.Id;
            catalogEntry["version"] = p.Version;
            catalogEntry["description"] = p.Description;
            catalogEntry["authors"] = p.Authors;
            catalogEntry["projectUrl"] = p.ProjectUrl ?? "";
            catalogEntry["listed"] = true;
            catalogEntry["dependencyGroups"] = depGroups;
            catalogEntry["packageContent"] = contentUrl;

            var item = Ld(entryUrl);
            item["catalogEntry"] = catalogEntry;
            item["packageContent"] = contentUrl;
            return item;
        }).ToArray();

        var page = Ld($"{baseUrl}/v3/registration/{idLower}/index.json#page/0");
        page["count"] = items.Length;
        page["items"] = items;
        page["lower"] = packages.First().Version;
        page["upper"] = packages.Last().Version;

        var root = Ld($"{baseUrl}/v3/registration/{idLower}/index.json");
        root["count"] = 1;
        root["items"] = new[] { page };
        return root;
    }

    public object? GetFlatContainerVersions(string id)
    {
        var versions = GetPackages()
            .Where(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Version.ToLowerInvariant())
            .OrderBy(v => v)
            .ToList();

        if (versions.Count == 0) return null;
        return new { versions };
    }

    public string? GetPackageFilePath(string id, string version)
    {
        var pkg = GetPackages().FirstOrDefault(p =>
            p.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
            p.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

        if (pkg == null) return null;
        return Path.Combine(_packagesPath, pkg.NupkgFileName);
    }

    public object GetSearchResults(string baseUrl, string? query, int skip, int take)
    {
        var packages = GetPackages().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            packages = packages.Where(p =>
                p.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Tags?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var grouped = packages
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Skip(skip)
            .Take(take)
            .ToList();

        var data = grouped.Select(g =>
        {
            var latest = g.OrderByDescending(p => p.Version).First();
            var idLower = latest.Id.ToLowerInvariant();
            var regUrl = $"{baseUrl}/v3/registration/{idLower}/index.json";

            return new Dictionary<string, object?>
            {
                ["@id"] = regUrl,
                ["@type"] = "Package",
                ["registration"] = regUrl,
                ["id"] = latest.Id,
                ["version"] = latest.Version,
                ["description"] = latest.Description,
                ["authors"] = new[] { latest.Authors },
                ["projectUrl"] = latest.ProjectUrl ?? "",
                ["tags"] = latest.Tags?.Split(';', ',', ' ')
                    .Select(t => t.Trim()).Where(t => t.Length > 0).ToArray()
                    ?? Array.Empty<string>(),
                ["totalDownloads"] = 0,
                ["versions"] = g.Select(p => new Dictionary<string, object>
                {
                    ["version"] = p.Version,
                    ["@id"] = $"{baseUrl}/v3/registration/{idLower}/{p.Version.ToLowerInvariant()}.json"
                }).ToArray()
            };
        }).ToList();

        return new { totalHits = grouped.Count, data };
    }
}

public record PackageInfo(
    string Id, string Version, string Description, string Authors,
    string? ProjectUrl, string? Tags,
    List<DependencyGroupInfo> Dependencies, string NupkgFileName);

public record DependencyGroupInfo(string TargetFramework, List<PackageDependencyInfo> Dependencies);
public record PackageDependencyInfo(string Id, string VersionRange);
