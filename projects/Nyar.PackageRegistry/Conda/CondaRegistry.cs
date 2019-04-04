using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nyar.PackageRegistry.Conda;

/// <summary>
///     conda 注册表适配器（Anaconda），对接 conda-forge 频道
/// </summary>
public class CondaRegistry : RegistryBase
{
    /// <summary>
    ///     conda 默认注册表端点
    /// </summary>
    public const string default_endpoint = "https://api.anaconda.org";

    /// <summary>
    ///     conda 默认频道
    /// </summary>
    public const string default_channel = "conda-forge";

    /// <summary>
    ///     创建 conda 注册表适配器
    /// </summary>
    /// <param name="endpoint">注册表端点，默认为 https://api.anaconda.org</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public CondaRegistry(string endpoint = default_endpoint, HttpClient? httpClient = null, ILogger? logger = null)
        : base("conda", endpoint, httpClient, logger)
    {
    }

    /// <summary>
    ///     conda 注册表的重试配置：3 次重试，标准退避
    /// </summary>
    protected override RetryConfig _retry_config { get; } = new()
    {
        max_retries = 3,
        initial_delay = TimeSpan.FromMilliseconds(500),
        backoff_multiplier = 2.0,
        max_delay = TimeSpan.FromSeconds(10)
    };

    /// <inheritdoc />
    public override async Task<Package> get_package(string packageName, string version)
    {
        var url = build_url($"package/{default_channel}/{Uri.EscapeDataString(packageName)}");
        var json = await get<CondaPackageResponse>(url);

        var resolvedVersion = version == "latest"
            ? json.latest_version ?? "0.0.0"
            : version;

        var tarball = json.files?.FirstOrDefault()?.download_url;

        var dependencyVersions = new Dictionary<string, string>();
        var dependencies = new List<string>();

        if (json.dependencies is not null)
            foreach (var (name, ver) in json.dependencies)
            {
                dependencyVersions[name] = ver;
                dependencies.Add($"{name}@{ver}");
            }

        return new Package
        {
            name = json.name ?? packageName,
            version = resolvedVersion,
            description = json.summary ?? json.description ?? string.Empty,
            homepage = json.homepage
                       ?? json.dev_url
                       ?? $"https://anaconda.org/{default_channel}/{packageName}",
            author = json.author ?? string.Empty,
            license = json.license ?? string.Empty,
            dependencies = dependencies,
            dependency_versions = dependencyVersions,
            dist_tarball = tarball
        };
    }

    /// <inheritdoc />
    public override async Task<List<Package>> search_packages(string query)
    {
        var url = build_url($"search?name={Uri.EscapeDataString(query)}&limit=20");
        var jsonList = await get<List<CondaSearchItem>>(url);

        var packages = new List<Package>();

        foreach (var item in jsonList)
            packages.Add(new Package
            {
                name = item.name ?? string.Empty,
                version = item.latest_version ?? "0.0.0",
                description = item.summary ?? string.Empty,
                homepage = item.homepage
                           ?? $"https://anaconda.org/{default_channel}/{item.name}",
                author = item.author ?? string.Empty,
                license = item.license ?? string.Empty
            });

        return packages;
    }

    /// <inheritdoc />
    public override async Task<PublishResult> publish_package(PublishOptions options, byte[] tarballData)
    {
        if (string.IsNullOrWhiteSpace(options.auth_token)) throw new RegistryException("发布到 conda 注册表需要认证令牌");

        var url = build_url($"upload/{default_channel}/{Uri.EscapeDataString(options.package_name)}/{options.version}");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(tarballData), "package", $"{options.package_name}-{options.version}.tar.bz2");

        var response = await send_authenticated(HttpMethod.Post, url, options.auth_token, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new PublishResult
            {
                success = false,
                package_name = options.package_name,
                version = options.version,
                message = "认证失败：无效的认证令牌"
            };

        if (response.StatusCode == HttpStatusCode.Conflict)
            return new PublishResult
            {
                success = false,
                package_name = options.package_name,
                version = options.version,
                message = $"版本 {options.version} 已存在于 conda 注册表中"
            };

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new PublishResult
            {
                success = false,
                package_name = options.package_name,
                version = options.version,
                message = $"发布失败：{(int)response.StatusCode} - {errorContent}"
            };
        }

        return new PublishResult
        {
            success = true,
            package_name = options.package_name,
            version = options.version,
            message = $"成功发布 {options.package_name}@{options.version} 到 conda 注册表",
            published_url = url
        };
    }

    /// <inheritdoc />
    public override async Task<string> download_package(Package package, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(package.dist_tarball))
            throw new RegistryException($"包 '{package.name}' 没有可用的下载地址");

        Directory.CreateDirectory(targetDirectory);

        var ext = package.dist_tarball.EndsWith(".tar.bz2") ? ".tar.bz2" : ".tar.gz";
        var tempFile = Path.Combine(Path.GetTempPath(), $"{package.name}@{package.version}{ext}");

        try
        {
            using var response = await _http_client.GetAsync(package.dist_tarball);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(tempFile);
            await stream.CopyToAsync(fileStream);

            verify_download_integrity(tempFile, package.dist_integrity);

            extract_tar_archive(tempFile, targetDirectory);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch
            {
            }
        }

        return targetDirectory;
    }

    /// <inheritdoc />
    public override async Task<List<string>> get_package_versions(string packageName)
    {
        var url = build_url($"package/{default_channel}/{Uri.EscapeDataString(packageName)}");

        try
        {
            var json = await get<CondaPackageResponse>(url);
            var fileVersionMap = new Dictionary<string, string?>();

            if (json.files is not null)
                foreach (var file in json.files)
                    if (file.version is not null)
                        fileVersionMap[file.version] = file.version;

            return [.. fileVersionMap.Keys];
        }
        catch (RegistryException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public override async Task<TokenVerifyResult> verify_token(string token)
    {
        var url = build_url("user");

        try
        {
            var response = await get_authenticated(url, token);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return TokenVerifyResult.failure("认证令牌无效或已过期");

            if (!response.IsSuccessStatusCode) return TokenVerifyResult.failure($"验证失败：{(int)response.StatusCode}");

            var userJson = await response.Content.ReadFromJsonAsync<CondaUserResponse>(_json_options);
            var username = userJson?.user?.login
                           ?? userJson?.login
                           ?? "未知用户";

            return TokenVerifyResult.success(username);
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.failure($"验证请求失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            return TokenVerifyResult.failure($"验证请求失败：{ex.Message}");
        }
    }

    #region TAR 解压

    /// <summary>
    ///     解压 .tar.gz / .tar.bz2 归档到目标目录
    /// </summary>
    private static void extract_tar_archive(string archivePath, string targetDirectory)
    {
        using var fileStream = File.OpenRead(archivePath);
        Stream dataStream;

        if (archivePath.EndsWith(".gz"))
            dataStream = new GZipStream(fileStream, CompressionMode.Decompress);
        else
            // .tar.bz2 需要 BZip2 支持，.NET BCL 不原生支持
            // 此处暂回退为直接读取原始流，后续可通过引入 NuGet 包来支持
            dataStream = fileStream;

        using var memoryStream = new MemoryStream();
        dataStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var buffer = new byte[4096];
        var position = 0L;
        var length = memoryStream.Length;

        while (position < length)
        {
            memoryStream.Position = position;
            memoryStream.ReadExactly(buffer, 0, 512);

            var name = Encoding.ASCII.GetString(buffer, 0, 100).TrimEnd('\0');
            var sizeStr = Encoding.ASCII.GetString(buffer, 124, 12).TrimEnd('\0', ' ');
            var fileSize = Convert.ToInt64(sizeStr, 8);

            position += 512;

            if (string.IsNullOrEmpty(name)) break;

            if (fileSize > 0)
            {
                var filePath = Path.Combine(targetDirectory, name);
                var fileDir = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(fileDir)) Directory.CreateDirectory(fileDir);

                var fileData = new byte[fileSize];
                memoryStream.Position = position;
                memoryStream.ReadExactly(fileData, 0, (int)fileSize);
                File.WriteAllBytes(filePath, fileData);
            }

            var paddedSize = (fileSize + 511) / 512 * 512;
            position += paddedSize;
        }
    }

    #endregion

    #region JSON 数据模型

    private class CondaPackageResponse
    {
        [JsonPropertyName("name")] public string? name { get; set; }

        [JsonPropertyName("latest_version")] public string? latest_version { get; set; }

        [JsonPropertyName("summary")] public string? summary { get; set; }

        [JsonPropertyName("description")] public string? description { get; set; }

        [JsonPropertyName("home")] public string? homepage { get; set; }

        [JsonPropertyName("dev_url")] public string? dev_url { get; set; }

        [JsonPropertyName("author")] public string? author { get; set; }

        [JsonPropertyName("license")] public string? license { get; set; }

        [JsonPropertyName("files")] public List<CondaFileItem>? files { get; set; }

        [JsonPropertyName("dependencies")] public Dictionary<string, string>? dependencies { get; set; }
    }

    private class CondaFileItem
    {
        [JsonPropertyName("download_url")] public string? download_url { get; set; }

        [JsonPropertyName("version")] public string? version { get; set; }
    }

    private class CondaSearchItem
    {
        [JsonPropertyName("name")] public string? name { get; set; }

        [JsonPropertyName("latest_version")] public string? latest_version { get; set; }

        [JsonPropertyName("summary")] public string? summary { get; set; }

        [JsonPropertyName("home")] public string? homepage { get; set; }

        [JsonPropertyName("author")] public string? author { get; set; }

        [JsonPropertyName("license")] public string? license { get; set; }
    }

    private class CondaUserResponse
    {
        [JsonPropertyName("user")] public CondaUserInfo? user { get; set; }

        [JsonPropertyName("login")] public string? login { get; set; }
    }

    private class CondaUserInfo
    {
        [JsonPropertyName("login")] public string login { get; } = string.Empty;
    }

    #endregion
}