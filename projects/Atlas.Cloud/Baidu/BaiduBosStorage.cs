using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Baidu;

/// <summary>
/// 百度云对象存储 BOS 实现，使用 BOS REST API HMAC-SHA256 签名
/// </summary>
public sealed class BaiduBosStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _secret_access_key;
    private readonly string _region;
    private readonly string _endpoint;
    private const string _service = "bce";

    /// <summary>
    /// 初始化百度云 BOS 存储
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="secretAccessKey">AccessKey Secret</param>
    /// <param name="region">区域，如 bj</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public BaiduBosStorage(
        string accessKeyId, string secretAccessKey,
        string region = "bj", HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _secret_access_key = secretAccessKey;
        _region = region;
        _endpoint = $"{region}.bcebos.com";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var url = $"https://{bucket}.{_endpoint}/{Uri.EscapeDataString(key)}";
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
        return BlobResult.fail($"BOS 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = $"https://{bucket}.{_endpoint}/{Uri.EscapeDataString(key)}";
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
        var url = $"https://{bucket}.{_endpoint}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request(request, bucket, key, null);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"BOS 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var queryString = $"maxKeys={maxKeys}";

        if (prefix is not null)
        {
            queryString += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var url = $"https://{bucket}.{_endpoint}/?{queryString}";
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
        var expiration = DateTimeOffset.UtcNow.Add(expiry);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var expirationStr = expiration.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var canonicalResource = $"/{bucket}/{key}";
        var stringToSign = $"{method}\n\n\n{timestamp}\n{canonicalResource}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret_access_key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        return $"https://{bucket}.{_endpoint}/{Uri.EscapeDataString(key)}"
               + $"?sign={Uri.EscapeDataString(signature)}"
               + $"&timestamp={Uri.EscapeDataString(timestamp)}"
               + $"&expiration={Uri.EscapeDataString(expirationStr)}"
               + $"&accessKeyId={_access_key_id}";
    }

    private void sign_request(
        HttpRequestMessage request, string bucket, string key, byte[]? body,
        string? queryString = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var host = $"{bucket}.{_endpoint}";

        request.Headers.Add("Host", host);
        request.Headers.Add("x-bce-date", timestamp);

        var canonicalUri = $"/{key}";
        var canonicalQueryString = queryString ?? string.Empty;
        var contentLength = body?.Length ?? 0;
        var contentLengthStr = contentLength > 0 ? contentLength.ToString() : string.Empty;

        var signedHeaders = "host;x-bce-date";
        var canonicalRequest = $"{request.Method}\n{canonicalUri}\n{canonicalQueryString}\n"
                               + $"host:{host}\n"
                               + $"x-bce-date:{timestamp}\n\n"
                               + $"{signedHeaders}\n";

        var signingKey = hmac_sha256(Encoding.UTF8.GetBytes(_secret_access_key), canonicalRequest);
        var signature = Convert.ToBase64String(signingKey);

        request.Headers.Add("Authorization",
            $"bce-auth-v1/{_access_key_id}/{timestamp}/3600/{signedHeaders}/{signature}");
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
