using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Hermes.Storage.Cos;

/// <summary>
///     腾讯云 COS 连接选项
/// </summary>
public sealed class CosOptions
{
    public string Region { get; init; } = "ap-guangzhou";
    public string Bucket { get; init; } = "";
    public string AppId { get; init; } = "";
    public string SecretId { get; init; } = "";
    public string SecretKey { get; init; } = "";
    public string? Endpoint { get; init; }
    public bool UseHttps { get; init; } = true;
}

/// <summary>
///     腾讯云 COS 对象存储 — 自研 HttpClient + HMAC-SHA1/HMAC-SHA256 签名，零外部依赖
/// </summary>
public sealed class CosStorage : IObjectStorage, IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly CosOptions _options;
    private bool _disposed;

    public CosStorage(CosOptions options)
    {
        _options = options;

        var scheme = options.UseHttps ? "https" : "http";
        var endpoint = options.Endpoint ?? $"cos.{options.Region}.myqcloud.com";
        var bucketAppId = options.Bucket.Contains('-') ? options.Bucket : $"{options.Bucket}-{options.AppId}";
        _baseUrl = $"{scheme}://{bucketAppId}.{endpoint}";

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(300)
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _httpClient.Dispose();
    }

    #region COS 签名

    private void SignRequest(HttpRequestMessage request, string method, string url, string[]? headersToSign = null)
    {
        var now = DateTime.UtcNow;
        var timestamp = ToTimestamp(now);
        var expiryTimestamp = ToTimestamp(now.AddHours(1));
        var keyTime = $"{timestamp};{expiryTimestamp}";

        var signKey = HmacSha1Hex(_options.SecretKey, keyTime);

        var host = _baseUrl.Substring(_baseUrl.IndexOf("://") + 3);
        request.Headers.Host = host;

        var signedHeaderList = headersToSign ?? ["host"];
        var headerPairs = new List<string>();
        foreach (var h in signedHeaderList)
        {
            var value = h.ToLowerInvariant() switch
            {
                "host" => host,
                "content-type" => request.Content?.Headers.ContentType?.ToString() ?? "",
                _ => ""
            };
            headerPairs.Add($"{h}={value}");
        }

        var httpString = $"{method.ToLowerInvariant()}\n{url}\n\n{string.Join(";", headerPairs)}\n";
        var stringToSign = $"sha1\n{keyTime}\n{Sha1Hex(httpString)}\n";
        var signature = HmacSha1Hex(signKey, stringToSign);

        var authorization =
            $"q-sign-algorithm=sha1&q-ak={_options.SecretId}&q-sign-time={keyTime}&q-key-time={keyTime}&q-header-list={string.Join(";", signedHeaderList)}&q-url-param-list=&q-signature={signature}";
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
    }

    #endregion

    #region XML 解析

    private static List<ObjectInfo> ParseListBucketResult(string xml)
    {
        var result = new List<ObjectInfo>();
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            foreach (var content in doc.Descendants(ns + "Contents"))
                result.Add(new ObjectInfo
                {
                    Key = content.Element(ns + "Key")?.Value ?? "",
                    Size = long.TryParse(content.Element(ns + "Size")?.Value, out var size) ? size : 0,
                    LastModified = DateTimeOffset.TryParse(content.Element(ns + "LastModified")?.Value,
                        CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
                        ? dt
                        : null,
                    ETag = content.Element(ns + "ETag")?.Value?.Trim('"')
                });
        }
        catch
        {
        }

        return result;
    }

    #endregion

    #region IObjectStorage

    public async Task PutAsync(string key, Stream data, string? contentType = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateKey(key);

        var url = $"/{key.TrimStart('/')}";
        var content = new StreamContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? DetectContentType(key));

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{url}")
        {
            Content = content
        };

        SignRequest(request, "PUT", url, ["content-type", "host"]);

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response);
    }

    public async Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateKey(key);

        var url = $"/{key.TrimStart('/')}";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}{url}");

        SignRequest(request, "GET", url, ["host"]);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateKey(key);

        var url = $"/{key.TrimStart('/')}";
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}{url}");

        SignRequest(request, "DELETE", url, ["host"]);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        await EnsureSuccessAsync(response);
        return true;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateKey(key);

        var url = $"/{key.TrimStart('/')}";
        var request = new HttpRequestMessage(HttpMethod.Head, $"{_baseUrl}{url}");

        SignRequest(request, "HEAD", url, ["host"]);

        var response = await _httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<ObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateKey(key);

        var url = $"/{key.TrimStart('/')}";
        var request = new HttpRequestMessage(HttpMethod.Head, $"{_baseUrl}{url}");

        SignRequest(request, "HEAD", url, ["host"]);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode) return null;

        return new ObjectMetadata
        {
            Key = key,
            Size = response.Content.Headers.ContentLength ?? 0,
            ContentType = response.Content.Headers.ContentType?.ToString(),
            LastModified = response.Content.Headers.LastModified,
            ETag = response.Headers.ETag?.Tag?.Trim('"')
        };
    }

    public async Task<IReadOnlyList<ObjectInfo>> ListAsync(string? prefix = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var url = $"/?prefix={Uri.EscapeDataString(prefix ?? "")}&max-keys=1000";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}{url}");

        SignRequest(request, "GET", url, ["host"]);

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response);

        var xml = await response.Content.ReadAsStringAsync(ct);
        return ParseListBucketResult(xml);
    }

    public async Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateKey(key);

        var url = $"/{key.TrimStart('/')}";
        var expirySeconds = (long)expiry.TotalSeconds;
        var now = DateTime.UtcNow;
        var keyTime = $"{ToTimestamp(now)};{ToTimestamp(now.AddSeconds(expirySeconds))}";

        var signKey = HmacSha1Hex(_options.SecretKey, keyTime);

        var host = _baseUrl.Substring(_baseUrl.IndexOf("://") + 3);
        var httpString = $"get\n{url}\n\nhost={host}\n";
        var stringToSign = $"sha1\n{keyTime}\n{Sha1Hex(httpString)}\n";
        var signature = HmacSha1Hex(signKey, stringToSign);

        var queryParams =
            $"q-sign-algorithm=sha1&q-ak={_options.SecretId}&q-sign-time={keyTime}&q-key-time={keyTime}&q-header-list=host&q-url-param-list=&q-signature={signature}";

        return $"{_baseUrl}{url}?{queryParams}";
    }

    #endregion

    #region 加密工具

    private static string HmacSha1Hex(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sha1Hex(string data)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA1.HashData(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static long ToTimestamp(DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    #endregion

    #region 辅助方法

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("对象键不能为空", nameof(key));
    }

    private static string DetectContentType(string key)
    {
        var ext = Path.GetExtension(key).ToLowerInvariant();
        return ext switch
        {
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"COS 请求失败：{(int)response.StatusCode} {response.StatusCode} — {body}");
    }

    #endregion
}