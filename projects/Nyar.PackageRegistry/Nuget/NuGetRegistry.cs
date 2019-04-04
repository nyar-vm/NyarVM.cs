using System.IO.Compression;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nyar.PackageRegistry.Nuget;

/// <summary>
///     NuGet 注册表适配器，对接 NuGet v3 API
/// </summary>
public class NuGetRegistry : RegistryBase
{
    /// <summary>
    ///     NuGet 默认注册表端点
    /// </summary>
    public const string default_endpoint = "https://api.nuget.org/v3";

    /// <summary>
    ///     服务索引缓存
    /// </summary>
    private NuGetServiceIndex? _service_index;

    /// <summary>
    ///     创建 NuGet 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://api.nuget.org/v3</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public NuGetRegistry(string endpoint = default_endpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("nuget", endpoint, httpClient, logger)
    {
    }

    /// <summary>
    ///     NuGet 注册表的重试配置：3 次重试，较长退避，适应大包注册表
    /// </summary>
    protected override RetryConfig _retry_config { get; } = new()
    {
        max_retries = 3,
        initial_delay = TimeSpan.FromSeconds(1),
        backoff_multiplier = 2.0,
        max_delay = TimeSpan.FromSeconds(15)
    };

    /// <inheritdoc />
    public override async Task<Package> get_package(string packageName, string version)
    {
        var index = await get_service_index();
        var regUrl = index.get_registration_base_url() ?? endpoint;

        var packageUrl = $"{regUrl}/{packageName.ToLowerInvariant()}/index.json";
        var packageData = await get<NuGetRegistrationIndex>(packageUrl);

        var items = packageData.items ?? [];

        if (version == "latest")
        {
            var latestItem = items
                .SelectMany(p => p.items ?? [])
                .MaxBy(l => parse_nu_get_version(l.catalog_entry?.version));

            if (latestItem?.catalog_entry is null) throw new RegistryException($"包 {packageName} 没有可用版本", 404);

            return convert_to_package(latestItem.catalog_entry);
        }

        var match = items
            .SelectMany(p => p.items ?? [])
            .FirstOrDefault(l =>
                string.Equals(l.catalog_entry?.version, version, StringComparison.OrdinalIgnoreCase));

        if (match?.catalog_entry is null) throw new RegistryException($"包 {packageName}@{version} 不存在", 404);

        return convert_to_package(match.catalog_entry);
    }

    /// <inheritdoc />
    public override async Task<List<Package>> search_packages(string query)
    {
        var index = await get_service_index();
        var searchUrl = index.get_search_query_url() ?? $"{endpoint}/query";

        var url = $"{searchUrl}?q={Uri.EscapeDataString(query)}&take=20&prerelease=false";
        var response = await get<NuGetSearchResponse>(url);

        var result = new List<Package>();

        if (response.data is not null)
            foreach (var item in response.data)
                result.Add(new Package
                {
                    name = item.id ?? string.Empty,
                    version = item.version ?? string.Empty,
                    description = item.description ?? string.Empty,
                    author = string.Join(", ", item.authors ?? [])
                });

        return result;
    }

    /// <inheritdoc />
    public override Task<PublishResult> publish_package(PublishOptions options, byte[] tarballData)
    {
        return Task.FromResult(new PublishResult
        {
            success = false,
            package_name = options.package_name,
            version = options.version,
            message = "NuGet 发布请使用 dotnet nuget push 或 nuget push 命令"
        });
    }

    /// <inheritdoc />
    public override async Task<string> download_package(Package package, string targetDirectory)
    {
        var index = await get_service_index();
        var flatUrl = index.get_package_base_address_url()
                      ?? $"{endpoint}/flatcontainer";

        var downloadUrl =
            $"{flatUrl}/{package.name.ToLowerInvariant()}/{package.version}/{package.name.ToLowerInvariant()}.{package.version}.nupkg";

        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-nuget-{package.name}-{package.version}.nupkg");

        try
        {
            var response = await _http_client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(tempFile))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            verify_download_integrity(tempFile, package.dist_integrity);

            await using var nupkgStream = File.OpenRead(tempFile);
            await extract_nupkg(nupkgStream, targetDirectory);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        return targetDirectory;
    }

    /// <inheritdoc />
    public override async Task<List<string>> get_package_versions(string packageName)
    {
        var index = await get_service_index();
        var regUrl = index.get_registration_base_url() ?? endpoint;

        var packageUrl = $"{regUrl}/{packageName.ToLowerInvariant()}/index.json";

        try
        {
            var packageData = await get<NuGetRegistrationIndex>(packageUrl);
            var items = packageData.items ?? [];

            return
            [
                .. items
                    .SelectMany(p => p.items ?? [])
                    .Select(l => l.catalog_entry?.version ?? string.Empty)
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
            ];
        }
        catch (RegistryException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> verify_token(string token)
    {
        var index = await get_service_index();
        var searchUrl = index.get_search_query_url() ?? $"{endpoint}/query";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{searchUrl}?q=test&take=1");
            request.Headers.Add("X-NuGet-ApiKey", token);

            var response = await _http_client.SendAsync(request);

            return response.IsSuccessStatusCode
                ? TokenVerifyResult.success("authenticated")
                : TokenVerifyResult.failure($"令牌验证失败，HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.failure($"网络请求失败: {ex.Message}");
        }
    }

    #region 私有方法

    private async Task<NuGetServiceIndex> get_service_index()
    {
        if (_service_index is not null) return _service_index;

        var url = $"{endpoint}/index.json";
        _service_index = await get<NuGetServiceIndex>(url);
        return _service_index;
    }

    private static Package convert_to_package(NuGetCatalogEntry entry)
    {
        return new Package
        {
            name = entry.id ?? string.Empty,
            version = entry.version ?? string.Empty,
            description = entry.description ?? string.Empty,
            author = string.Join(", ", entry.authors ?? []),
            license = entry.license_expression ?? entry.license_url ?? string.Empty,
            dist_tarball = entry.package_content
        };
    }

    private static Version? parse_nu_get_version(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;

        var clean = version.Split('-')[0];
        return Version.TryParse(clean, out var v) ? v : null;
    }

    private static async Task extract_nupkg(Stream nupkgStream, string targetDirectory)
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            await using (var fileStream = File.Create(tempFile))
            {
                await nupkgStream.CopyToAsync(fileStream);
            }

            Directory.CreateDirectory(targetDirectory);

            using var archive = await ZipFile.OpenReadAsync(tempFile);

            foreach (var entry in archive.Entries)
            {
                var destPath = Path.Combine(targetDirectory, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith('/'))
                {
                    Directory.CreateDirectory(destPath);
                    continue;
                }

                await entry.ExtractToFileAsync(destPath, true);
            }
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    #endregion

    #region NuGet v3 JSON 数据模型

    private class NuGetServiceIndex
    {
        [JsonPropertyName("version")] public string? version { get; set; }

        [JsonPropertyName("resources")] public List<NuGetResource>? resources { get; set; }

        public string? get_search_query_url()
        {
            return resources?
                .FirstOrDefault(r => r.type == "SearchQueryService")
                ?.id;
        }

        public string? get_registration_base_url()
        {
            return resources?
                .FirstOrDefault(r => r.type?
                    .StartsWith("RegistrationsBaseUrl", StringComparison.OrdinalIgnoreCase) == true)
                ?.id;
        }

        public string? get_package_base_address_url()
        {
            return resources?
                .FirstOrDefault(r => r.type?
                    .StartsWith("PackageBaseAddress", StringComparison.OrdinalIgnoreCase) == true)
                ?.id;
        }
    }

    private class NuGetResource
    {
        [JsonPropertyName("@id")] public string? id { get; set; }

        [JsonPropertyName("@type")] public string? type { get; set; }
    }

    private class NuGetSearchResponse
    {
        [JsonPropertyName("totalHits")] public int total_hits { get; set; }

        [JsonPropertyName("data")] public List<NuGetSearchItem>? data { get; set; }
    }

    private class NuGetSearchItem
    {
        [JsonPropertyName("id")] public string? id { get; set; }

        [JsonPropertyName("version")] public string? version { get; set; }

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("authors")] public List<string>? authors { get; set; }
    }

    private class NuGetRegistrationIndex
    {
        [JsonPropertyName("items")] public List<NuGetRegistrationPage>? items { get; set; }
    }

    private class NuGetRegistrationPage
    {
        [JsonPropertyName("items")] public List<NuGetRegistrationLeaf>? items { get; set; }
    }

    private class NuGetRegistrationLeaf
    {
        [JsonPropertyName("catalogEntry")] public NuGetCatalogEntry? catalog_entry { get; set; }
    }

    private class NuGetCatalogEntry
    {
        [JsonPropertyName("id")] public string? id { get; set; }

        [JsonPropertyName("version")] public string? version { get; set; }

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("authors")] public List<string>? authors { get; set; }

        [JsonPropertyName("licenseExpression")]
        public string? license_expression { get; set; }

        [JsonPropertyName("licenseUrl")] public string? license_url { get; set; }

        [JsonPropertyName("packageContent")] public string? package_content { get; set; }
    }

    #endregion
}