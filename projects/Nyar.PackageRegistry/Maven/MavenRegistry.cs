using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nyar.PackageRegistry.Maven;

/// <summary>
///     Maven Central 注册表适配器，对接 Maven Central REST API
/// </summary>
public class MavenRegistry : RegistryBase
{
    /// <summary>
    ///     Maven 搜索端点
    /// </summary>
    public const string default_endpoint = "https://search.maven.org";

    /// <summary>
    ///     Maven 仓库下载端点
    /// </summary>
    public const string default_repository_url = "https://repo1.maven.org/maven2";

    private string _repository_url;

    /// <summary>
    ///     创建 Maven 注册表适配器
    /// </summary>
    /// <param name="endpoint">搜索端点，默认为 https://search.maven.org</param>
    /// <param name="repositoryUrl">仓库下载地址，默认为 https://repo1.maven.org/maven2</param>
    /// <param name="publishUrl">发布地址，可选</param>
    /// <param name="httpClient">可选的 HTTP 客户端</param>
    /// <param name="logger">可选的日志记录器</param>
    public MavenRegistry(
        string endpoint = default_endpoint,
        string repositoryUrl = default_repository_url,
        string? publishUrl = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
        : base("maven", endpoint, httpClient, logger)
    {
        _repository_url = repositoryUrl.TrimEnd('/');
    }

    /// <summary>
    ///     仓库下载地址
    /// </summary>
    public string repository_url
    {
        get => _repository_url;
        set => _repository_url = value.TrimEnd('/');
    }

    /// <summary>
    ///     发布地址（Sonatype OSSRH 等）
    /// </summary>
    public string? publish_url { get; set; }

    /// <summary>
    ///     Maven 注册表的重试配置：4 次重试，Solr 搜索慢时使用较长退避
    /// </summary>
    protected override RetryConfig _retry_config { get; } = new()
    {
        max_retries = 4,
        initial_delay = TimeSpan.FromMilliseconds(600),
        backoff_multiplier = 2.0,
        max_delay = TimeSpan.FromSeconds(12)
    };

    /// <inheritdoc />
    public override async Task<Package> get_package(string packageName, string version)
    {
        parse_maven_coordinates(packageName, out var groupId, out var artifactId);

        var searchUrl = build_url(
            $"solrsearch/select?q=g:{Uri.EscapeDataString(groupId)}+AND+a:{Uri.EscapeDataString(artifactId)}&core=gav&rows=20&wt=json");

        var searchJson = await get<MavenSearchResponse>(searchUrl);
        var docs = searchJson.response?.docs;

        if (docs is null || docs.Count == 0)
            throw new RegistryException($"包 '{packageName}' 在 Maven Central 中未找到", 404);

        var resolvedVersion = version == "latest"
            ? docs[0].version ?? "0.0.0"
            : version;

        var groupPath = groupId.Replace('.', '/');
        var pomUrl = $"{repository_url}/{groupPath}/{artifactId}/{resolvedVersion}/{artifactId}-{resolvedVersion}.pom";

        var dependencyVersions = new Dictionary<string, string>();
        var dependencies = new List<string>();

        try
        {
            var pomResponse = await _http_client.GetAsync(pomUrl);

            if (pomResponse.IsSuccessStatusCode)
            {
                var pomContent = await pomResponse.Content.ReadAsStringAsync();
                parse_pom_dependencies(pomContent, dependencies, dependencyVersions);
            }
        }
        catch
        {
            _logger.LogWarning("解析 POM 文件失败: {PomUrl}", pomUrl);
        }

        return new Package
        {
            name = $"{groupId}:{artifactId}",
            version = resolvedVersion,
            description = string.Empty,
            homepage = string.Empty,
            author = groupId,
            license = string.Empty,
            dependencies = dependencies,
            dependency_versions = dependencyVersions,
            dist_tarball =
                $"{repository_url}/{groupPath}/{artifactId}/{resolvedVersion}/{artifactId}-{resolvedVersion}.jar"
        };
    }

    /// <inheritdoc />
    public override async Task<List<Package>> search_packages(string query)
    {
        var url = build_url($"solrsearch/select?q={Uri.EscapeDataString(query)}&rows=20&wt=json");
        var json = await get<MavenSearchResponse>(url);

        var docs = json.response?.docs;
        var packages = new List<Package>();

        if (docs is not null)
            foreach (var doc in docs)
            {
                var g = doc.group_id;
                var a = doc.artifact_id;

                if (g is null || a is null) continue;

                packages.Add(new Package
                {
                    name = $"{g}:{a}",
                    version = doc.version ?? "0.0.0",
                    description = string.Empty,
                    homepage = string.Empty,
                    author = g,
                    license = string.Empty
                });
            }

        return packages;
    }

    /// <inheritdoc />
    public override async Task<PublishResult> publish_package(PublishOptions options, byte[] tarballData)
    {
        if (string.IsNullOrWhiteSpace(options.auth_token)) throw new RegistryException("发布到 Maven 仓库需要认证令牌");

        var publishEndpoint = publish_url ?? "https://s01.oss.sonatype.org/service/local/staging/deploy/maven2";

        parse_maven_coordinates(options.package_name, out var groupId, out var artifactId);
        var groupPath = groupId.Replace('.', '/');
        var url = $"{publishEndpoint}/{groupPath}/{artifactId}/{options.version}/{artifactId}-{options.version}.jar";

        var content = new ByteArrayContent(tarballData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/java-archive");

        var response = await send_authenticated(HttpMethod.Put, url, options.auth_token, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new PublishResult
            {
                success = false,
                package_name = options.package_name,
                version = options.version,
                message = "认证失败：无效的认证令牌"
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
            message = $"成功发布 {options.package_name}:{options.version} 到 Maven 仓库"
        };
    }

    /// <inheritdoc />
    public override async Task<string> download_package(Package package, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(package.dist_tarball))
            throw new RegistryException($"包 '{package.name}' 没有可用的下载地址");

        Directory.CreateDirectory(targetDirectory);

        var fileName = Path.GetFileName(new Uri(package.dist_tarball).AbsolutePath);
        var targetFile = Path.Combine(targetDirectory, fileName);

        using var response = await _http_client.GetAsync(package.dist_tarball);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(targetFile);
        await stream.CopyToAsync(fileStream);

        verify_download_integrity(targetFile, package.dist_integrity);

        return targetDirectory;
    }

    /// <inheritdoc />
    public override async Task<List<string>> get_package_versions(string packageName)
    {
        parse_maven_coordinates(packageName, out var groupId, out var artifactId);

        var url = build_url(
            $"solrsearch/select?q=g:{Uri.EscapeDataString(groupId)}+AND+a:{Uri.EscapeDataString(artifactId)}&core=gav&rows=200&wt=json");

        try
        {
            var json = await get<MavenSearchResponse>(url);
            var docs = json.response?.docs;

            return docs?.Select(d => d.version ?? string.Empty)
                .Where(v => !string.IsNullOrEmpty(v))
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
        var url = build_url("solrsearch/select?q=g:com&rows=0");

        try
        {
            var response = await get_authenticated(url, token);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return TokenVerifyResult.failure("认证令牌无效或已过期");

            if (response.IsSuccessStatusCode) return TokenVerifyResult.success("maven-user");

            return TokenVerifyResult.failure($"验证失败：{(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return TokenVerifyResult.failure($"验证请求失败：{ex.Message}");
        }
    }

    #region 私有方法

    /// <summary>
    ///     解析 Maven 坐标 groupId:artifactId 格式
    /// </summary>
    public static void parse_maven_coordinates(string packageName, out string groupId, out string artifactId)
    {
        var colonIndex = packageName.IndexOf(':');

        if (colonIndex > 0 && colonIndex < packageName.Length - 1)
        {
            groupId = packageName[..colonIndex];
            artifactId = packageName[(colonIndex + 1)..];
        }
        else
        {
            groupId = packageName;
            artifactId = packageName;
        }
    }

    private static void parse_pom_dependencies(string pomContent, List<string> dependencies,
        Dictionary<string, string> dependencyVersions)
    {
        var depsStart = pomContent.IndexOf("<dependencies>", StringComparison.Ordinal);

        if (depsStart < 0) return;

        var depsEnd = pomContent.IndexOf("</dependencies>", StringComparison.Ordinal);

        if (depsEnd < 0) return;

        var depsSection = pomContent[depsStart..(depsEnd + "</dependencies>".Length)];
        var searchStart = 0;

        while (true)
        {
            var depStart = depsSection.IndexOf("<dependency>", searchStart, StringComparison.Ordinal);

            if (depStart < 0) break;

            var depEnd = depsSection.IndexOf("</dependency>", depStart, StringComparison.Ordinal);

            if (depEnd < 0) break;

            var depXml = depsSection[depStart..(depEnd + "</dependency>".Length)];
            searchStart = depEnd + 1;

            var depGroup = extract_xml_element(depXml, "groupId");
            var depArtifact = extract_xml_element(depXml, "artifactId");
            var depVersion = extract_xml_element(depXml, "version");

            if (depGroup is not null && depArtifact is not null)
            {
                var coord = $"{depGroup}:{depArtifact}";
                var ver = depVersion ?? "0.0.0";

                if (!ver.StartsWith('$'))
                {
                    dependencyVersions[coord] = ver;
                    dependencies.Add($"{coord}:{ver}");
                }
            }
        }
    }

    private static string? extract_xml_element(string xml, string elementName)
    {
        var openTag = $"<{elementName}>";
        var closeTag = $"</{elementName}>";

        var start = xml.IndexOf(openTag, StringComparison.Ordinal);

        if (start < 0) return null;

        start += openTag.Length;
        var end = xml.IndexOf(closeTag, start, StringComparison.Ordinal);

        if (end < 0) return null;

        return xml[start..end].Trim();
    }

    #endregion

    #region JSON 数据模型

    private class MavenSearchResponse
    {
        [JsonPropertyName("response")] public MavenResponseBody? response { get; set; }
    }

    private class MavenResponseBody
    {
        [JsonPropertyName("docs")] public List<MavenDoc>? docs { get; set; }
    }

    private class MavenDoc
    {
        [JsonPropertyName("g")] public string? group_id { get; set; }

        [JsonPropertyName("a")] public string? artifact_id { get; set; }

        [JsonPropertyName("v")] public string? version { get; set; }
    }

    #endregion
}