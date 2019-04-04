using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nyar.PackageRegistry.Jsr;

/// <summary>
///     JSR 注册表适配器，对接 jsr.io API
/// </summary>
public class JsrRegistry : RegistryBase
{
    /// <summary>
    ///     JSR 默认注册表端点
    /// </summary>
    public const string default_endpoint = "https://jsr.io";

    /// <summary>
    ///     JSR npm 兼容层端点
    /// </summary>
    private const string _npm_compat_endpoint = "https://npm.jsr.io";

    /// <summary>
    ///     创建 JSR 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://jsr.io</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public JsrRegistry(string endpoint = default_endpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("jsr", endpoint, httpClient, logger)
    {
    }

    /// <summary>
    ///     JSR 注册表的重试配置：3 次重试，较快退避
    /// </summary>
    protected override RetryConfig _retry_config { get; } = new()
    {
        max_retries = 3,
        initial_delay = TimeSpan.FromMilliseconds(300),
        backoff_multiplier = 2.0,
        max_delay = TimeSpan.FromSeconds(8)
    };

    /// <inheritdoc />
    public override async Task<Package> get_package(string packageName, string version)
    {
        var (scope, name) = parse_package_name(packageName);

        if (version == "latest")
        {
            var metaUrl = build_url($"api/scopes/{scope}/packages/{name}/versions");
            var versions = await get<JsrVersionListResponse>(metaUrl);

            var latestVersion = versions.versions?
                .OrderByDescending(v => parse_version_for_comparison(v.version))
                .FirstOrDefault();

            if (latestVersion is null) throw new RegistryException($"包 {packageName} 没有可用版本", 404);

            return await get_package_version(scope, name, latestVersion.version);
        }

        return await get_package_version(scope, name, version);
    }

    /// <inheritdoc />
    public override async Task<List<Package>> search_packages(string query)
    {
        var url = build_url($"api/packages?query={Uri.EscapeDataString(query)}&limit=20");
        var response = await get<JsrSearchResponse>(url);

        var result = new List<Package>();

        if (response.items is not null)
            foreach (var item in response.items)
                result.Add(new Package
                {
                    name = $"@{item.scope}/{item.name}",
                    version = item.latest_stable_version ?? item.latest_version ?? "0.0.0",
                    description = item.description ?? string.Empty
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
            message = "JSR 发布请使用 deno publish 或 jsr publish 命令"
        });
    }

    /// <inheritdoc />
    public override async Task<string> download_package(Package package, string targetDirectory)
    {
        var (scope, name) = parse_package_name(package.name);
        var npmCompatUrl = $"{_npm_compat_endpoint}/~/packages/@{scope}/{name}/{package.version}/";

        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-jsr-{scope}-{name}-{package.version}.tgz");

        try
        {
            var response = await _http_client.GetAsync(npmCompatUrl);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(tempFile))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            verify_download_integrity(tempFile, package.dist_integrity);

            await using var tarballStream = File.OpenRead(tempFile);
            await extract_tarball(tarballStream, targetDirectory);
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
        var (scope, name) = parse_package_name(packageName);
        var url = build_url($"api/scopes/{scope}/packages/{name}/versions");

        try
        {
            var response = await get<JsrVersionListResponse>(url);
            return response.versions?
                .Select(v => v.version)
                .Where(v => v is not null)
                .Select(v => v!)
                .ToList() ?? [];
        }
        catch (RegistryException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> verify_token(string token)
    {
        var url = build_url("api/user");

        try
        {
            var response = await get_authenticated(url, token);

            if (response.IsSuccessStatusCode)
            {
                var userData = await response.Content.ReadFromJsonAsync<JsrUserResponse>(_json_options);
                return TokenVerifyResult.success(userData?.user?.name ?? "unknown");
            }

            return TokenVerifyResult.failure($"令牌验证失败，HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.failure($"网络请求失败: {ex.Message}");
        }
    }

    #region 私有方法

    private static (string Scope, string Name) parse_package_name(string packageName)
    {
        var clean = packageName.StartsWith('@') ? packageName[1..] : packageName;
        var slashIndex = clean.IndexOf('/');

        if (slashIndex > 0) return (clean[..slashIndex], clean[(slashIndex + 1)..]);

        return ("std", clean);
    }

    private async Task<Package> get_package_version(string scope, string name, string version)
    {
        var url = build_url($"api/scopes/{scope}/packages/{name}/versions/{version}");
        var versionData = await get<JsrVersionData>(url);

        return new Package
        {
            name = $"@{scope}/{name}",
            version = versionData.version ?? version,
            description = versionData.description ?? string.Empty,
            dist_tarball = $"{_npm_compat_endpoint}/~/packages/@{scope}/{name}/{version}/",
            dist_integrity = versionData.checksum is not null ? $"sha256-{versionData.checksum}" : null
        };
    }

    private static Version? parse_version_for_comparison(string version)
    {
        return Version.TryParse(version.Split('-')[0], out var v) ? v : null;
    }

    private static async Task extract_tarball(Stream tarballStream, string targetDirectory)
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            await using (var fileStream = File.Create(tempFile))
            {
                await tarballStream.CopyToAsync(fileStream);
            }

            Directory.CreateDirectory(targetDirectory);

            using var archive = await ZipFile.OpenReadAsync(tempFile);

            foreach (var entry in archive.Entries)
            {
                var relativePath = entry.FullName;

                if (relativePath.StartsWith("package/", StringComparison.OrdinalIgnoreCase))
                    relativePath = relativePath["package/".Length..];

                if (string.IsNullOrEmpty(relativePath)) continue;

                var destPath = Path.Combine(targetDirectory, relativePath);
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

    #region JSON 数据模型

    private class JsrSearchResponse
    {
        [JsonPropertyName("items")] public List<JsrSearchItem>? items { get; set; }
    }

    private class JsrSearchItem
    {
        [JsonPropertyName("scope")] public string scope { get; } = string.Empty;

        [JsonPropertyName("name")] public string name { get; } = string.Empty;

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("latestVersion")] public string? latest_version { get; set; }

        [JsonPropertyName("latestStableVersion")]
        public string? latest_stable_version { get; set; }
    }

    private class JsrVersionListResponse
    {
        [JsonPropertyName("versions")] public List<JsrVersionEntry>? versions { get; set; }
    }

    private class JsrVersionEntry
    {
        [JsonPropertyName("version")] public string version { get; } = string.Empty;
    }

    private class JsrVersionData
    {
        [JsonPropertyName("version")] public string? version { get; set; }

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("checksum")] public string? checksum { get; set; }
    }

    private class JsrUserResponse
    {
        [JsonPropertyName("user")] public JsrUser? user { get; set; }
    }

    private class JsrUser
    {
        [JsonPropertyName("name")] public string name { get; } = string.Empty;
    }

    #endregion
}