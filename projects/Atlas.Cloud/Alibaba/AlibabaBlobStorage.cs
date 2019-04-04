using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云对象存储 OSS 实现，使用 REST API HMAC-SHA1 签名
/// </summary>
public sealed class AlibabaBlobStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;
    private readonly string _endpoint;
    private readonly string _region;

    /// <summary>
    /// 初始化阿里云 OSS 存储
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="endpoint">OSS 端点，如 oss-cn-hangzhou.aliyuncs.com</param>
    /// <param name="region">区域</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaBlobStorage(
        string accessKeyId, string accessKeySecret,
        string endpoint, string region = "cn-hangzhou",
        HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _endpoint = endpoint;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var host = $"{bucket}.{_endpoint}";
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var content = new ByteArrayContent(data);

        if (contentType is not null)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        sign_request(request, bucket, key);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok(response.Headers.ETag?.Tag);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"OSS 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var host = $"{bucket}.{_endpoint}";
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, bucket, key);

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
        var host = $"{bucket}.{_endpoint}";
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request(request, bucket, key);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"OSS 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var host = $"{bucket}.{_endpoint}";
        var url = $"https://{host}/?list-type=2&max-keys={maxKeys}";

        if (prefix is not null)
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        sign_request(request, bucket, string.Empty);

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
                    key = item.GetProperty("Key").GetString() ?? string.Empty,
                    size = item.GetProperty("Size").GetInt64(),
                    last_modified = item.GetProperty("LastModified").GetDateTime(),
                    etag = item.TryGetProperty("ETag", out var etag) ? etag.GetString() : null
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET")
    {
        var expiration = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var host = $"{bucket}.{_endpoint}";
        var resourcePath = $"/{key}";

        var stringToSign = $"{method}\n\n\n{expiration}\n{resourcePath}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var encodedSignature = Uri.EscapeDataString(signature);

        return $"https://{host}/{Uri.EscapeDataString(key)}"
               + $"?OSSAccessKeyId={_access_key_id}"
               + $"&Expires={expiration}"
               + $"&Signature={encodedSignature}";
    }

    private void sign_request(HttpRequestMessage request, string bucket, string key)
    {
        var date = DateTime.UtcNow.ToString("R");
        request.Headers.Add("Date", date);
        request.Headers.Add("Host", $"{bucket}.{_endpoint}");

        var resourcePath = string.IsNullOrEmpty(key)
            ? $"/{bucket}/"
            : $"/{bucket}/{key}";

        var stringToSign = $"{request.Method}\n\n\n{date}\n{resourcePath}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        request.Headers.Add("Authorization", $"OSS {_access_key_id}:{signature}");
    }
}