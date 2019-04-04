using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nyar.PackageRegistry;

/// <summary>
///     注册表抽象基类，提供 HTTP 客户端管理、重试策略、日志支持和通用 JSON 序列化
/// </summary>
public abstract class RegistryBase : IRegistry
{
    /// <summary>
    ///     JSON 序列化选项，使用 CamelCase 命名策略和不区分大小写的属性匹配
    /// </summary>
    protected static readonly JsonSerializerOptions _json_options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string _endpoint;

    /// <summary>
    ///     创建注册表适配器实例
    /// </summary>
    /// <param name="name">注册表名称标识</param>
    /// <param name="defaultEndpoint">默认 API 端点地址</param>
    /// <param name="httpClient">HTTP 客户端实例，若不提供则自动创建</param>
    /// <param name="logger">日志记录器，若不提供则使用空记录器</param>
    protected RegistryBase(string name, string defaultEndpoint, HttpClient? httpClient = null, ILogger? logger = null)
    {
        this.name = name;
        _endpoint = defaultEndpoint.TrimEnd('/');
        _http_client = httpClient ?? create_default_http_client();
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    ///     获取重试配置，子类可重写以自定义策略
    /// </summary>
    protected virtual RetryConfig _retry_config => RetryConfig.@default;

    /// <summary>
    ///     获取内部 HTTP 客户端实例
    /// </summary>
    protected HttpClient _http_client { get; }

    /// <summary>
    ///     获取日志记录器
    /// </summary>
    protected ILogger _logger { get; }

    /// <inheritdoc />
    public string name { get; }

    /// <inheritdoc />
    public string endpoint
    {
        get => _endpoint;
        set => _endpoint = value.TrimEnd('/');
    }

    /// <inheritdoc />
    public abstract Task<Package> get_package(string packageName, string version);

    /// <inheritdoc />
    public abstract Task<List<Package>> search_packages(string query);

    /// <inheritdoc />
    public abstract Task<PublishResult> publish_package(PublishOptions options, byte[] tarballData);

    /// <inheritdoc />
    public abstract Task<string> download_package(Package package, string targetDirectory);

    /// <inheritdoc />
    public abstract Task<List<string>> get_package_versions(string packageName);

    /// <inheritdoc />
    public abstract Task<TokenVerifyResult> verify_token(string token);

    /// <summary>
    ///     释放 HTTP 客户端资源
    /// </summary>
    public void Dispose()
    {
        _http_client.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     构建完整的 API URL
    /// </summary>
    /// <param name="path">相对于端点的路径</param>
    protected string build_url(string path)
    {
        return $"{_endpoint}/{path.TrimStart('/')}";
    }

    /// <summary>
    ///     发送 GET 请求并将响应反序列化为指定类型，支持重试和取消
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="url">请求 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<T> get<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await send_with_retry(() =>
            new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(_json_options, cancellationToken);

        if (result is null)
        {
            _logger.LogError("反序列化 JSON 响应失败: {Url}", url);
            throw new RegistryException($"反序列化响应失败: {url}");
        }

        return result;
    }

    /// <summary>
    ///     发送 GET 请求并返回原始响应消息，支持重试
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> get_http_response(string url,
        CancellationToken cancellationToken = default)
    {
        return await send_with_retry(() =>
            new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
    }

    /// <summary>
    ///     发送带 Bearer 认证头的 GET 请求，支持重试
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="token">认证令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> get_authenticated(string url, string token,
        CancellationToken cancellationToken = default)
    {
        return await send_with_retry(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            return request;
        }, cancellationToken);
    }

    /// <summary>
    ///     发送带自定义认证头和 HTTP 方法的请求，支持重试
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    /// <param name="url">请求 URL</param>
    /// <param name="token">认证令牌</param>
    /// <param name="content">请求体内容，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> send_authenticated(
        HttpMethod method,
        string url,
        string token,
        HttpContent? content = null,
        CancellationToken cancellationToken = default)
    {
        return await send_with_retry(() =>
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            if (content is not null) request.Content = content;

            return request;
        }, cancellationToken);
    }

    /// <summary>
    ///     发送自定义请求并获取原始响应，支持重试和取消
    /// </summary>
    /// <param name="requestFactory">请求工厂函数</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> send_with_retry(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        var config = _retry_config;
        var lastException = (Exception?)null;

        for (var attempt = 0; attempt <= config.max_retries; attempt++)
            try
            {
                using var request = requestFactory();
                var response = await _http_client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode) return response;

                var shouldRetry = config.should_retry?.Invoke(response)
                                  ?? is_default_retryable(response);

                if (!shouldRetry || attempt >= config.max_retries) response.EnsureSuccessStatusCode();

                _logger.LogWarning(
                    "请求 {Url} 返回 {StatusCode}，第 {Attempt}/{MaxRetries} 次重试",
                    request.RequestUri, (int)response.StatusCode, attempt + 1, config.max_retries);

                var delay = TimeSpan.FromMilliseconds(
                    config.initial_delay.TotalMilliseconds * Math.Pow(config.backoff_multiplier, attempt));

                if (delay > config.max_delay) delay = config.max_delay;

                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;

                if (attempt >= config.max_retries)
                {
                    _logger.LogError(ex, "请求已达最大重试次数 {MaxRetries}", config.max_retries);
                    throw;
                }

                _logger.LogWarning(ex, "请求异常，第 {Attempt}/{MaxRetries} 次重试", attempt + 1, config.max_retries);

                var delay = TimeSpan.FromMilliseconds(
                    config.initial_delay.TotalMilliseconds * Math.Pow(config.backoff_multiplier, attempt));

                if (delay > config.max_delay) delay = config.max_delay;

                await Task.Delay(delay, cancellationToken);
            }

        throw lastException ?? new RegistryException("请求失败，已达最大重试次数");
    }

    private static HttpClient create_default_http_client()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Nyar.PackageManager.Registry/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private static bool is_default_retryable(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        return statusCode is 429 or >= 500 and < 600;
    }

    /// <summary>
    ///     验证下载文件的完整性（SRI 格式：sha256-xxx 或 sha512-xxx）
    ///     如果 Package.DistIntegrity 为空则跳过校验
    /// </summary>
    /// <param name="filePath">待校验的文件路径</param>
    /// <param name="expectedIntegrity">期望的完整性值（SRI 格式），为空则跳过</param>
    /// <exception cref="RegistryException">校验失败时抛出</exception>
    protected static void verify_download_integrity(string filePath, string? expectedIntegrity)
    {
        if (string.IsNullOrEmpty(expectedIntegrity)) return;

        if (!File.Exists(filePath)) throw new RegistryException($"完整性校验失败：文件不存在 {filePath}");

        var separatorIndex = expectedIntegrity.IndexOf('-');
        if (separatorIndex < 0) throw new RegistryException($"完整性校验失败：无效的 SRI 格式 '{expectedIntegrity}'");

        var algorithm = expectedIntegrity[..separatorIndex];
        var expectedHash = expectedIntegrity[(separatorIndex + 1)..];

        byte[] actualHashBytes;
        using (var stream = File.OpenRead(filePath))
        {
            actualHashBytes = algorithm switch
            {
                "sha256" => SHA256.HashData(stream),
                "sha384" => SHA384.HashData(stream),
                "sha512" => SHA512.HashData(stream),
                _ => throw new RegistryException($"完整性校验失败：不支持的哈希算法 '{algorithm}'")
            };
        }

        var actualHash = Convert.ToBase64String(actualHashBytes);

        if (actualHash != expectedHash)
            throw new RegistryException(
                $"完整性校验失败：{algorithm} 不匹配" +
                $"（期望 {expectedHash[..16]}...，实际 {actualHash[..16]}...）");

        _ = algorithm;
    }

    /// <summary>
    ///     计算文件的 SRI 完整性值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="algorithm">哈希算法，默认 sha512</param>
    /// <returns>SRI 格式的完整性值（如 sha512-xxxx）</returns>
    public static string compute_file_integrity(string filePath, string algorithm = "sha512")
    {
        if (!File.Exists(filePath)) throw new RegistryException($"计算完整性失败：文件不存在 {filePath}");

        byte[] hashBytes;
        using (var stream = File.OpenRead(filePath))
        {
            hashBytes = algorithm switch
            {
                "sha256" => SHA256.HashData(stream),
                "sha384" => SHA384.HashData(stream),
                "sha512" => SHA512.HashData(stream),
                _ => throw new RegistryException($"不支持的哈希算法 '{algorithm}'")
            };
        }

        return $"{algorithm}-{Convert.ToBase64String(hashBytes)}";
    }
}