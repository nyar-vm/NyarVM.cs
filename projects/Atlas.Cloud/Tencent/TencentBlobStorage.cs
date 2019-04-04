using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云对象存储 COS 实现，使用 COS REST API HMAC-SHA1 签名
/// </summary>
public sealed class TencentBlobStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _endpoint_template = "{0}.cos.{1}.myqcloud.com";

    /// <summary>
    /// 初始化腾讯云 COS 存储
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 ap-guangzhou</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentBlobStorage(string secretId, string secretKey, string region = "ap-guangzhou", HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    private string get_host(string bucket) => string.Format(_endpoint_template, bucket, _region);

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
        sign_request(request, bucket, key);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok(response.Headers.ETag?.Tag);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"COS 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var host = get_host(bucket);
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
        var host = get_host(bucket);
        var url = $"https://{host}/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        sign_request(request, bucket, key);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"COS 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var host = get_host(bucket);
        var url = $"https://{host}/?max-keys={maxKeys}";

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
        var expiration = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var host = get_host(bucket);
        var httpString = $"{method.ToLowerInvariant()}\n/{key}\n\nhost={host}\n";
        var sha1String = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ";" + expiration;

        var signKey = hmac_sha1(_secret_key, sha1String);
        var stringToSign = $"sha1\n{sha1String}\n{sha1_hash(httpString)}\n";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(signKey));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var qParams = Uri.EscapeDataString(
            $"q-sign-algorithm=sha1"
            + $"&q-ak={_secret_id}"
            + $"&q-sign-time={sha1String}"
            + $"&q-key-time={sha1String}"
            + $"&q-header-list=host&q-url-param-list="
            + $"&q-signature={signature}");

        return $"https://{host}/{Uri.EscapeDataString(key)}?{qParams}";
    }

    private void sign_request(HttpRequestMessage request, string bucket, string key)
    {
        var host = get_host(bucket);
        var now = DateTimeOffset.UtcNow;
        var startTime = now.ToUnixTimeSeconds();
        var endTime = now.AddHours(1).ToUnixTimeSeconds();
        var keyTime = $"{startTime};{endTime}";

        request.Headers.Add("Host", host);

        var signedHeaders = "host";
        var headerList = $"host={host}";

        var httpMethod = request.Method.Method.ToLowerInvariant();
        var uriPath = $"/{key}";

        var httpString = $"{httpMethod}\n{uriPath}\n\n{headerList}\n";

        var signKey = hmac_sha1(_secret_key, keyTime);

        var httpStringSha1 = sha1_hash(httpString);
        var stringToSign = $"sha1\n{keyTime}\n{httpStringSha1}\n";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(signKey));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var auth = $"q-sign-algorithm=sha1"
                   + $"&q-ak={_secret_id}"
                   + $"&q-sign-time={keyTime}"
                   + $"&q-key-time={keyTime}"
                   + $"&q-header-list={signedHeaders}"
                   + $"&q-url-param-list=&q-signature={signature}";

        request.Headers.Add("Authorization", auth);
    }

    private static string hmac_sha1(string key, string data)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string sha1_hash(string data)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}