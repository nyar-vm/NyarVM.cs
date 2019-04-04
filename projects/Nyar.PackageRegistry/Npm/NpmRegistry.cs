using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nyar.PackageRegistry.Npm;

/// <summary>
///     npm 注册表适配器，对接 npm registry API
/// </summary>
public class NpmRegistry : RegistryBase
{
    /// <summary>
    ///     npm 默认注册表端点
    /// </summary>
    public const string default_endpoint = "https://registry.npmjs.org";

    /// <summary>
    ///     创建 npm 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://registry.npmjs.org</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public NpmRegistry(string endpoint = default_endpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("npm", endpoint, httpClient, logger)
    {
    }

    /// <summary>
    ///     npm 注册表的重试配置：5 次重试，指数退避，应对速率限制
    /// </summary>
    protected override RetryConfig _retry_config { get; } = new()
    {
        max_retries = 5,
        initial_delay = TimeSpan.FromMilliseconds(500),
        backoff_multiplier = 2.0,
        max_delay = TimeSpan.FromSeconds(15)
    };

    /// <inheritdoc />
    public override async Task<Package> get_package(string packageName, string version)
    {
        if (version == "latest")
        {
            var url = build_url($"{packageName}/latest");
            var versionData = await get<NpmVersionData>(url);
            return convert_to_package(packageName, versionData);
        }

        var packageUrl = build_url($"{packageName}/{version}");
        var data = await get<NpmVersionData>(packageUrl);
        return convert_to_package(packageName, data);
    }

    /// <inheritdoc />
    public override async Task<List<Package>> search_packages(string query)
    {
        var url = build_url($"-/v1/search?text={Uri.EscapeDataString(query)}&size=20");
        var response = await get<NpmSearchResponse>(url);

        var result = new List<Package>();

        if (response.objects is not null)
            foreach (var obj in response.objects)
                if (obj.package is not null)
                    result.Add(new Package
                    {
                        name = obj.package.name,
                        version = obj.package.version,
                        description = obj.package.description ?? string.Empty,
                        author = obj.package.author?.name ?? string.Empty
                    });

        return result;
    }

    /// <inheritdoc />
    public override async Task<PublishResult> publish_package(PublishOptions options, byte[] tarballData)
    {
        var url = build_url(options.package_name);

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new ByteArrayContent(tarballData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        if (!string.IsNullOrWhiteSpace(options.auth_token))
            request.Headers.Add("Authorization", $"Bearer {options.auth_token}");

        var response = await _http_client.SendAsync(request);

        return response.IsSuccessStatusCode
            ? new PublishResult
            {
                success = true,
                package_name = options.package_name,
                version = options.version,
                message = "发布成功",
                published_url = url
            }
            : new PublishResult
            {
                success = false,
                package_name = options.package_name,
                version = options.version,
                message = $"发布失败，HTTP {(int)response.StatusCode}"
            };
    }

    /// <inheritdoc />
    public override async Task<string> download_package(Package package, string targetDirectory)
    {
        string tarballUrl;

        if (!string.IsNullOrWhiteSpace(package.dist_tarball))
        {
            tarballUrl = package.dist_tarball;
        }
        else
        {
            var url = build_url($"{package.name}/{package.version}");
            var data = await get<NpmVersionData>(url);

            if (string.IsNullOrWhiteSpace(data.dist?.tarball))
                throw new RegistryException($"获取 {package.name}@{package.version} 的下载地址失败", 404);

            tarballUrl = data.dist.tarball;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"legion-npm-{package.name}-{package.version}.tgz");

        try
        {
            var response = await _http_client.GetAsync(tarballUrl);
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
        var url = build_url(packageName);
        var data = await get<NpmPackageMetadata>(url);

        return data.versions?.Keys.ToList() ?? [];
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> verify_token(string token)
    {
        var url = build_url("-/whoami");

        try
        {
            var response = await get_authenticated(url, token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var whoami = JsonSerializer.Deserialize<NpmWhoAmIResponse>(content, _json_options);

                return TokenVerifyResult.success(
                    whoami?.username ?? "unknown");
            }

            return TokenVerifyResult.failure($"令牌验证失败，HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.failure($"网络请求失败: {ex.Message}");
        }
    }

    #region 私有方法

    private static Package convert_to_package(string packageName, NpmVersionData data)
    {
        var deps = new List<string>();
        var depVersions = new Dictionary<string, string>();

        if (data.dependencies is not null)
            foreach (var dep in data.dependencies)
            {
                deps.Add($"{dep.Key}@{dep.Value}");
                depVersions[dep.Key] = dep.Value;
            }

        return new Package
        {
            name = data.name ?? packageName,
            version = data.version ?? string.Empty,
            description = data.description ?? string.Empty,
            author = data.author?.name ?? string.Empty,
            license = data.license ?? string.Empty,
            dist_tarball = data.dist?.tarball,
            dist_integrity = data.dist?.integrity,
            dependencies = deps,
            dependency_versions = depVersions,
            peer_dependencies = data.peer_dependencies
        };
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

    private class NpmPackageMetadata
    {
        [JsonPropertyName("name")] public string? name { get; set; }

        [JsonPropertyName("versions")] public Dictionary<string, NpmVersionData>? versions { get; set; }

        [JsonPropertyName("dist-tags")] public Dictionary<string, string>? dist_tags { get; set; }
    }

    private class NpmVersionData
    {
        [JsonPropertyName("name")] public string? name { get; set; }

        [JsonPropertyName("version")] public string? version { get; set; }

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("author")] public NpmAuthor? author { get; set; }

        [JsonPropertyName("license")] public string? license { get; set; }

        [JsonPropertyName("dist")] public NpmDist? dist { get; set; }

        [JsonPropertyName("dependencies")] public Dictionary<string, string>? dependencies { get; set; }

        [JsonPropertyName("peerDependencies")] public Dictionary<string, string>? peer_dependencies { get; set; }
    }

    private class NpmAuthor
    {
        [JsonPropertyName("name")] public string name { get; } = string.Empty;
    }

    private class NpmDist
    {
        [JsonPropertyName("tarball")] public string? tarball { get; set; }

        [JsonPropertyName("integrity")] public string? integrity { get; set; }

        [JsonPropertyName("shasum")] public string? shasum { get; set; }
    }

    private class NpmSearchResponse
    {
        [JsonPropertyName("objects")] public List<NpmSearchObject>? objects { get; set; }
    }

    private class NpmSearchObject
    {
        [JsonPropertyName("package")] public NpmSearchPackage? package { get; set; }
    }

    private class NpmSearchPackage
    {
        [JsonPropertyName("name")] public string name { get; } = string.Empty;

        [JsonPropertyName("version")] public string version { get; } = string.Empty;

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("author")] public NpmAuthor? author { get; set; }
    }

    private class NpmWhoAmIResponse
    {
        [JsonPropertyName("username")] public string? username { get; set; }
    }

    #endregion
}