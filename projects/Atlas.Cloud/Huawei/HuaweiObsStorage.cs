using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Huawei;

/// <summary>
/// 华为云对象存储 OBS 实现，使用 OBS REST API HMAC-SHA256 签名（类似 AWS V4，service = obs）
/// </summary>
public sealed class HuaweiObsStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _secret_access_key;
    private readonly string _region;
    private readonly string _endpoint;
    private const string _service = "obs";

    /// <summary>
    /// 初始化华为云 OBS 存储
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="secretAccessKey">AccessKey Secret</param>
    /// <param name="region">区域，如 cn-north-1</param>
    /// <param name="endpoint">OBS 端点，为 null 时自动拼接</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public HuaweiObsStorage(
        string accessKeyId, string secretAccessKey,
        string region = "cn-north-1", string? endpoint = null,
        HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _secret_access_key = secretAccessKey;
        _region = region;
        _endpoint = endpoint ?? $"obs.{region}.myhuaweicloud.com";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/{bucket}/{Uri.EscapeDataString(key)}";
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
            return BlobResult.ok(response.Headers.ETag?.Tag);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"OBS 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/{bucket}/{Uri.EscapeDataString(key)}";
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
        var url = $"https://{_endpoint}/{bucket}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request(request, bucket, key, null);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"OBS 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var queryString = $"list-type=2&max-keys={maxKeys}";

        if (prefix is not null)
        {
            queryString += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var url = $"https://{_endpoint}/{bucket}?{queryString}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, bucket, string.Empty, null, queryString);

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancel);
        var doc = XDocument.Parse(xml);
        var results = new List<BlobInfo>();

        foreach (var elem in doc.Descendants("Contents"))
        {
            results.Add(new BlobInfo
            {
                key = elem.Element("Key")?.Value ?? string.Empty,
                size = long.TryParse(elem.Element("Size")?.Value, out var s) ? s : 0,
                last_modified = DateTime.TryParse(elem.Element("LastModified")?.Value, out var dt) ? dt : default,
                etag = elem.Element("ETag")?.Value
            });
        }

        return results;
    }

    /// <inheritdoc />
    public string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET")
    {
        var t = DateTimeOffset.UtcNow;
        var obsDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credential = $"{_access_key_id}/{datestamp}/{_region}/{_service}/request";

        var canonicalRequest = $"{method}\n/{bucket}/{key}\n\nhost:{_endpoint}\n\nhost\nUNSIGNED-PAYLOAD";
        var stringToSign = $"OBS4-HMAC-SHA256\n{obsDate}\n{datestamp}/{_region}/{_service}/request\n{sha256_hex(canonicalRequest)}";
        var signingKey = derive_signing_key(datestamp);
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        return $"https://{_endpoint}/{bucket}/{Uri.EscapeDataString(key)}"
               + $"?X-Obs-Algorithm=OBS4-HMAC-SHA256"
               + $"&X-Obs-Credential={Uri.EscapeDataString(credential)}"
               + $"&X-Obs-Date={obsDate}"
               + $"&X-Obs-Expires={(int)expiry.TotalSeconds}"
               + $"&X-Obs-SignedHeaders=host"
               + $"&X-Obs-Signature={signature}";
    }

    private void sign_request(
        HttpRequestMessage request, string bucket, string key, byte[]? body,
        string? queryString = null)
    {
        var t = DateTimeOffset.UtcNow;
        var obsDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/request";

        request.Headers.Add("Host", _endpoint);
        request.Headers.Add("X-Obs-Date", obsDate);

        var payloadHash = body is not null ? sha256_hex(body) : sha256_hex(string.Empty);
        request.Headers.Add("X-Obs-Content-Sha256", payloadHash);

        var signedHeaders = "host;x-obs-content-sha256;x-obs-date";
        var canonicalUri = $"/{bucket}";

        if (!string.IsNullOrEmpty(key))
        {
            canonicalUri += $"/{key}";
        }

        var canonicalQueryString = queryString ?? string.Empty;

        var canonicalRequest = $"{request.Method}\n{canonicalUri}\n{canonicalQueryString}\n"
                               + $"host:{_endpoint}\n"
                               + $"x-obs-content-sha256:{payloadHash}\n"
                               + $"x-obs-date:{obsDate}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"OBS4-HMAC-SHA256\n{obsDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = derive_signing_key(datestamp);
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"OBS4-HMAC-SHA256 Credential={_access_key_id}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private byte[] derive_signing_key(string datestamp)
    {
        return hmac_sha256(
            hmac_sha256(
                hmac_sha256(
                    hmac_sha256(
                        Encoding.UTF8.GetBytes($"OBS4{_secret_access_key}"),
                        datestamp),
                    _region),
                _service),
            "request");
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
