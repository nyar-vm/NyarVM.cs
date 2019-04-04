using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Aws;

/// <summary>
/// AWS S3 对象存储实现，使用 AWS Signature v4
/// </summary>
public sealed class AwsBlobStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _service = "s3";

    /// <summary>
    /// 初始化 AWS S3 存储
    /// </summary>
    /// <param name="accessKey">Access Key</param>
    /// <param name="secretKey">Secret Key</param>
    /// <param name="region">区域，如 us-east-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AwsBlobStorage(string accessKey, string secretKey, string region = "us-east-1", HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    private string get_host(string bucket)
    {
        return $"{bucket}.s3.{_region}.amazonaws.com";
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var host = get_host(bucket);
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var content = new ByteArrayContent(data);

        if (contentType is not null)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        sign_request_v4(request, bucket, key, data);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok(response.Headers.ETag?.Tag);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"S3 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var host = get_host(bucket);
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request_v4(request, bucket, key, null);

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
        var host = get_host(bucket);
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request_v4(request, bucket, key, null);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"S3 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var host = get_host(bucket);
        var url = $"https://{host}/?list-type=2&max-keys={maxKeys}";

        if (prefix is not null)
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request_v4(request, bucket, string.Empty, null);

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancel), cancellationToken: cancel);

        var results = new List<BlobInfo>();

        if (doc.RootElement.TryGetProperty("Contents", out var contents))
        {
            foreach (var item in contents.EnumerateArray())
            {
                results.Add(new BlobInfo
                {
                    key = item.GetProperty("Key").get_string() ?? string.Empty,
                    size = item.GetProperty("Size").GetInt64(),
                    last_modified = item.GetProperty("LastModified").GetDateTime(),
                    etag = item.TryGetProperty("ETag", out var etag) ? etag.get_string() : null
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET")
    {
        var expiration = DateTimeOffset.UtcNow.Add(expiry);
        var host = get_host(bucket);
        var date = expiration.ToString("yyyyMMdd'T'HHmmss'Z'");
        var credential = $"{_access_key}/{expiration:yyyyMMdd}/{_region}/{_service}/aws4_request";

        var canonicalRequest = $"{method}\n/{key}\n\nhost:{host}\n\nhost\nUNSIGNED-PAYLOAD";
        var stringToSign = $"AWS4-HMAC-SHA256\n{date}\n{expiration:yyyyMMdd}/{_region}/{_service}/aws4_request\n{sha256_hex(canonicalRequest)}";
        var signingKey = hmac_sha256(hmac_sha256(hmac_sha256(hmac_sha256(
            Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), expiration.ToString("yyyyMMdd")),
            _region), _service), "aws4_request");
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        return $"https://{host}/{Uri.EscapeDataString(key)}"
               + $"?X-Amz-Algorithm=AWS4-HMAC-SHA256"
               + $"&X-Amz-Credential={Uri.EscapeDataString(credential)}"
               + $"&X-Amz-Date={date}"
               + $"&X-Amz-Expires={(int)expiry.TotalSeconds}"
               + $"&X-Amz-SignedHeaders=host"
               + $"&X-Amz-Signature={signature}";
    }

    private void sign_request_v4(HttpRequestMessage request, string bucket, string key, byte[]? body)
    {
        var host = get_host(bucket);
        var t = DateTimeOffset.UtcNow;
        var amzDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/aws4_request";

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Amz-Date", amzDate);

        var payloadHash = body is not null ? sha256_hex(body) : "UNSIGNED-PAYLOAD";
        request.Headers.Add("X-Amz-Content-Sha256", payloadHash);

        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = $"{request.Method}\n/{key}\n\n"
                               + $"host:{host}\n"
                               + $"x-amz-content-sha256:{payloadHash}\n"
                               + $"x-amz-date:{amzDate}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = hmac_sha256(hmac_sha256(hmac_sha256(hmac_sha256(
            Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), datestamp),
            _region), _service), "aws4_request");
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_access_key}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
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

    private static string hmac_sha256_hex(byte[] key, string dataToHash, string data)
    {
        using var hmac = new HMACSHA256(key);
        hmac.TransformFinalBlock(Encoding.UTF8.GetBytes(dataToHash), 0, Encoding.UTF8.GetByteCount(dataToHash));
        var signingKey = hmac.Hash!;
        return hmac_sha256_hex(signingKey, data);
    }

    private static string hmac_sha256_hex(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}