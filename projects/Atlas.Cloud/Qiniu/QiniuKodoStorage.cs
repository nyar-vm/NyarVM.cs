using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Qiniu;

/// <summary>
/// 七牛云对象存储 Kodo 实现，使用 Kodo REST API Qiniu Token 签名（HMAC-SHA1）
/// </summary>
public sealed class QiniuKodoStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private readonly string _endpoint;

    /// <summary>
    /// 初始化七牛云 Kodo 存储
    /// </summary>
    /// <param name="accessKey">AccessKey</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 z0</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public QiniuKodoStorage(
        string accessKey, string secretKey,
        string region = "z0", HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _endpoint = $"kodo-{region}.qiniuapi.com";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/buckets/{bucket}/objects/{Uri.EscapeDataString(key)}/upload";
        var content = new ByteArrayContent(data);

        if (contentType is not null)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        sign_request(request, $"buckets/{bucket}/objects/{key}/upload");

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"Kodo 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/buckets/{bucket}/objects/{Uri.EscapeDataString(key)}/download";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, $"buckets/{bucket}/objects/{key}/download");

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
        var url = $"https://{_endpoint}/buckets/{bucket}/objects/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request(request, $"buckets/{bucket}/objects/{key}");

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"Kodo 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/buckets/{bucket}/objects?limit={maxKeys}";

        if (prefix is not null)
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, $"buckets/{bucket}/objects");

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);
        var results = new List<BlobInfo>();

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                results.Add(new BlobInfo
                {
                    key = item.TryGetProperty("key", out var k) ? k.get_string() ?? string.Empty : string.Empty,
                    size = item.TryGetProperty("fsize", out var s) ? s.GetInt64() : 0,
                    last_modified = item.TryGetProperty("putTime", out var pt)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(pt.GetInt64() / 10000).DateTime
                        : default,
                    etag = item.TryGetProperty("hash", out var h) ? h.get_string() : null
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET")
    {
        var deadline = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var downloadUrl = $"https://{_endpoint}/buckets/{bucket}/objects/{key}/download";
        var stringToSign = $"{downloadUrl}\n{deadline}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_secret_key));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var token = $"{_access_key}:{signature}:{deadline}";

        return $"{downloadUrl}?token={Uri.EscapeDataString(token)}";
    }

    private void sign_request(HttpRequestMessage request, string pathAndQuery)
    {
        var stringToSign = $"{request.Method.Method} {pathAndQuery}\n";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_secret_key));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var token = $"Qiniu {_access_key}:{signature}";

        request.Headers.Add("Authorization", token);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
