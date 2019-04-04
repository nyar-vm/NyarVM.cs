using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Oracle;

/// <summary>
/// Oracle Cloud 对象存储实现，使用 Object Storage REST API HMAC-SHA256 签名（兼容 S3）
/// </summary>
public sealed class OracleObjectStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _tenancy_id;
    private readonly string _user_id;
    private readonly string _fingerprint;
    private readonly string _private_key;
    private readonly string _region;
    private readonly string _endpoint;
    private const string _service = "s3";

    /// <summary>
    /// 初始化 Oracle Cloud 对象存储
    /// </summary>
    /// <param name="tenancyId">租户 OCID</param>
    /// <param name="userId">用户 OCID</param>
    /// <param name="fingerprint">API 密钥指纹</param>
    /// <param name="privateKey">PEM 格式私钥</param>
    /// <param name="region">区域，如 us-ashburn-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public OracleObjectStorage(
        string tenancyId, string userId, string fingerprint, string privateKey,
        string region = "us-ashburn-1", HttpClient? httpClient = null)
    {
        _tenancy_id = tenancyId;
        _user_id = userId;
        _fingerprint = fingerprint;
        _private_key = privateKey;
        _region = region;
        _endpoint = $"objectstorage.{region}.oraclecloud.com";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/n/{_tenancy_id}/b/{bucket}/o/{Uri.EscapeDataString(key)}";
        var content = new ByteArrayContent(data);

        if (contentType is not null)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        sign_request(request, bucket, key, data);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"Oracle 对象存储上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/n/{_tenancy_id}/b/{bucket}/o/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, bucket, key, null);

        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancel);
    }

    /// <inheritdoc />
    public async Task<BlobResult> delete_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/n/{_tenancy_id}/b/{bucket}/o/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request(request, bucket, key, null);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"Oracle 对象存储删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/n/{_tenancy_id}/b/{bucket}/o?limit={maxKeys}";

        if (prefix is not null)
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, bucket, string.Empty, null);

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
        var results = new List<BlobInfo>();

        if (doc.RootElement.TryGetProperty("objects", out var objects))
        {
            foreach (var item in objects.EnumerateArray())
            {
                results.Add(new BlobInfo
                {
                    key = item.TryGetProperty("name", out var n) ? n.get_string() ?? string.Empty : string.Empty,
                    size = item.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
                    last_modified = item.TryGetProperty("timeCreated", out var tc)
                        ? tc.GetDateTime()
                        : default,
                    etag = item.TryGetProperty("etag", out var e) ? e.get_string() : null
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET")
    {
        var t = DateTimeOffset.UtcNow;
        var amzDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credential = $"{_tenancy_id}/{_user_id}/{_fingerprint}/{datestamp}/{_region}/{_service}/aws4_request";

        var path = $"/n/{_tenancy_id}/b/{bucket}/o/{key}";
        var canonicalRequest = $"{method}\n{path}\n\nhost:{_endpoint}\n\nhost\nUNSIGNED-PAYLOAD";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{datestamp}/{_region}/{_service}/aws4_request\n{sha256_hex(canonicalRequest)}";

        var signingKey = derive_signing_key(datestamp);
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        return $"https://{_endpoint}{path}"
               + $"?X-Amz-Algorithm=AWS4-HMAC-SHA256"
               + $"&X-Amz-Credential={Uri.EscapeDataString(credential)}"
               + $"&X-Amz-Date={amzDate}"
               + $"&X-Amz-Expires={(int)expiry.TotalSeconds}"
               + $"&X-Amz-SignedHeaders=host"
               + $"&X-Amz-Signature={signature}";
    }

    private void sign_request(
        HttpRequestMessage request, string bucket, string key, byte[]? body)
    {
        var t = DateTimeOffset.UtcNow;
        var amzDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/aws4_request";

        request.Headers.Add("Host", _endpoint);
        request.Headers.Add("X-Amz-Date", amzDate);

        var payloadHash = body is not null ? sha256_hex(body) : "UNSIGNED-PAYLOAD";
        request.Headers.Add("X-Amz-Content-Sha256", payloadHash);

        var path = $"/n/{_tenancy_id}/b/{bucket}/o/{key}";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";

        var canonicalRequest = $"{request.Method}\n{path}\n\n"
                               + $"host:{_endpoint}\n"
                               + $"x-amz-content-sha256:{payloadHash}\n"
                               + $"x-amz-date:{amzDate}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = derive_signing_key(datestamp);
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_tenancy_id}/{_user_id}/{_fingerprint}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private byte[] derive_signing_key(string datestamp)
    {
        return hmac_sha256(
            hmac_sha256(
                hmac_sha256(
                    hmac_sha256(
                        Encoding.UTF8.GetBytes($"AWS4{_private_key}"),
                        datestamp),
                    _region),
                _service),
            "aws4_request");
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string sha256_hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string hmac_sha256_hex(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
