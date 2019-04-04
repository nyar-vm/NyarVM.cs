using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Valhalla.Server.Storage;

/// <summary>
///     S3 兼容存储后端 — 基于 HTTP REST API 实现，不依赖 AWS SDK
///     支持 AWS S3、MinIO、Cloudflare R2 等兼容存储
/// </summary>
public class S3Storage : IStorage
{
    private readonly string _access_key;
    private readonly string _bucket_name;
    private readonly string _endpoint;
    private readonly HttpClient _http_client;
    private readonly string _region;
    private readonly string _secret_key;

    /// <summary>
    ///     创建 S3 存储后端（公开读写模式，无签名）
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="endpoint">S3 端点地址</param>
    /// <param name="region">区域</param>
    public S3Storage(string bucketName, string endpoint, string region)
        : this(bucketName, endpoint, region, string.Empty, string.Empty)
    {
    }

    /// <summary>
    ///     创建 S3 存储后端（带签名认证）
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="endpoint">S3 端点地址</param>
    /// <param name="region">区域</param>
    /// <param name="accessKey">Access Key</param>
    /// <param name="secretKey">Secret Key</param>
    public S3Storage(string bucketName, string endpoint, string region, string accessKey, string secretKey)
    {
        _bucket_name = bucketName;
        _endpoint = endpoint.TrimEnd('/');
        _region = region;
        _access_key = accessKey;
        _secret_key = secretKey;

        _http_client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    /// <inheritdoc />
    public async Task<string?> read_string(string path, CancellationToken ct = default)
    {
        var response = await send_request(HttpMethod.Get, path, ct: ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <inheritdoc />
    public async Task<byte[]?> read_bytes(string path, CancellationToken ct = default)
    {
        var response = await send_request(HttpMethod.Get, path, ct: ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<StorageResult> write_string(string path, string content, CancellationToken ct = default)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(content);
            var response = await send_request(HttpMethod.Put, path, data, "text/plain; charset=utf-8", ct);
            response.EnsureSuccessStatusCode();
            return StorageResult.succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<StorageResult> write_bytes(string path, byte[] data, CancellationToken ct = default)
    {
        try
        {
            var response = await send_request(HttpMethod.Put, path, data, "application/octet-stream", ct);
            response.EnsureSuccessStatusCode();
            return StorageResult.succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> exists(string path, CancellationToken ct = default)
    {
        var response = await send_request(HttpMethod.Head, path, ct: ct);
        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <inheritdoc />
    public async Task<StorageResult> delete(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await send_request(HttpMethod.Delete, path, ct: ct);

            if (response.StatusCode == HttpStatusCode.NotFound) return StorageResult.succeed();

            response.EnsureSuccessStatusCode();
            return StorageResult.succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> list(string prefix, CancellationToken ct = default)
    {
        var result = new List<string>();
        string? continuationToken = null;

        do
        {
            var query = $"list-type=2&prefix={Uri.EscapeDataString(prefix)}&max-keys=1000";

            if (continuationToken is not null)
                query += $"&continuation-token={Uri.EscapeDataString(continuationToken)}";

            var response = await send_request(HttpMethod.Get, $"?{query}", ct: ct);

            if (!response.IsSuccessStatusCode) break;

            var xmlContent = await response.Content.ReadAsStringAsync(ct);
            var doc = XDocument.Parse(xmlContent);

            XNamespace ns = "http://s3.amazonaws.com/doc/2006-03-01/";

            foreach (var content in doc.Root?.elements(ns + "Contents") ?? [])
            {
                var keyElement = content.Element(ns + "Key");
                if (keyElement is not null) result.Add(keyElement.Value);
            }

            var isTruncated = doc.Root?.Element(ns + "IsTruncated")?.Value == "true";
            continuationToken = isTruncated
                ? doc.Root?.Element(ns + "NextContinuationToken")?.Value
                : null;
        } while (continuationToken is not null);

        return result;
    }

    #region S3 签名

    /// <summary>
    ///     发送 S3 请求（自动签名）
    /// </summary>
    private async Task<HttpResponseMessage> send_request(
        HttpMethod method,
        string path,
        byte[]? data = null,
        string? contentType = null,
        CancellationToken ct = default)
    {
        var normalizedPath = path.Replace('\\', '/').TrimStart('/');
        var url = $"{_endpoint}/{_bucket_name}/{normalizedPath}";

        using var request = new HttpRequestMessage(method, url);

        if (data is not null) request.Content = new ByteArrayContent(data);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var date = timestamp.Substring(0, 8);

        request.Headers.Add("Host", new Uri(url).Host);
        request.Headers.Add("x-amz-date", timestamp);
        request.Headers.Add("x-amz-content-sha256", compute_sha256_hex(data ?? []));

        if (contentType is not null && data is not null)
            request.Content!.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        if (!string.IsNullOrEmpty(_access_key) && !string.IsNullOrEmpty(_secret_key))
            sign_request(request, method, normalizedPath, timestamp, date, data);

        return await _http_client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>
    ///     AWS Signature Version 4 签名
    /// </summary>
    private void sign_request(
        HttpRequestMessage request,
        HttpMethod method,
        string path,
        string timestamp,
        string date,
        byte[]? data)
    {
        var service = "s3";
        var credentialScope = $"{date}/{_region}/{service}/aws4_request";

        var signedHeaders = new List<string> { "host", "x-amz-content-sha256", "x-amz-date" };
        var canonicalHeaders = new StringBuilder();
        canonicalHeaders.Append($"host:{request.Headers.Host}\n");
        canonicalHeaders.Append($"x-amz-content-sha256:{request.Headers.GetValues("x-amz-content-sha256").First()}\n");
        canonicalHeaders.Append($"x-amz-date:{timestamp}\n");

        var signedHeadersStr = string.Join(";", signedHeaders);

        var payloadHash = request.Headers.GetValues("x-amz-content-sha256").First();

        var canonicalRequest =
            $"{method}\n/{_bucket_name}/{path}\n\n{canonicalHeaders}\n{signedHeadersStr}\n{payloadHash}";

        var stringToSign =
            $"AWS4-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{compute_sha256_hex(Encoding.UTF8.GetBytes(canonicalRequest))}";

        var signingKey = derive_signing_key(date, _region, service, _secret_key);
        var signature = compute_hmac_hex(signingKey, Encoding.UTF8.GetBytes(stringToSign));

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "AWS4-HMAC-SHA256",
            $"Credential={_access_key}/{credentialScope}, SignedHeaders={signedHeadersStr}, Signature={signature}");
    }

    /// <summary>
    ///     派生签名密钥
    /// </summary>
    private static byte[] derive_signing_key(string date, string region, string service, string secretKey)
    {
        var kDate = compute_hmac(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), Encoding.UTF8.GetBytes(date));
        var kRegion = compute_hmac(kDate, Encoding.UTF8.GetBytes(region));
        var kService = compute_hmac(kRegion, Encoding.UTF8.GetBytes(service));
        var kSigning = compute_hmac(kService, Encoding.UTF8.GetBytes("aws4_request"));
        return kSigning;
    }

    private static byte[] compute_hmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string compute_hmac_hex(byte[] key, byte[] data)
    {
        var hash = compute_hmac(key, data);
        return Convert.ToHexStringLower(hash);
    }

    private static string compute_sha256_hex(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexStringLower(hash);
    }

    #endregion
}