using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Google;

/// <summary>
/// Google Cloud Storage 实现，使用 REST API OAuth2 Bearer 认证
/// </summary>
public sealed class GoogleCloudStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _access_token;
    private const string _endpoint = "storage.googleapis.com";

    /// <summary>
    /// 初始化 Google Cloud Storage
    /// </summary>
    /// <param name="accessToken">OAuth2 访问令牌</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public GoogleCloudStorage(string accessToken, HttpClient? httpClient = null)
    {
        _access_token = accessToken;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/upload/storage/v1/b/{bucket}/o?uploadType=media&name={Uri.EscapeDataString(key)}";
        var content = new ByteArrayContent(data);

        if (contentType is not null)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Add("Authorization", $"Bearer {_access_token}");

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancel);
            using var doc = JsonDocument.Parse(responseBody);
            var etag = doc.RootElement.TryGetProperty("etag", out var e) ? e.get_string() : null;
            return BlobResult.ok(etag);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"GCS 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/storage/v1/b/{bucket}/o/{Uri.EscapeDataString(key)}?alt=media";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {_access_token}");

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
        var url = $"https://{_endpoint}/storage/v1/b/{bucket}/o/{Uri.EscapeDataString(key)}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Authorization", $"Bearer {_access_token}");

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"GCS 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/storage/v1/b/{bucket}/o?maxResults={maxKeys}";

        if (prefix is not null)
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {_access_token}");

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);
        var results = new List<BlobInfo>();

        if (doc.RootElement.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                results.Add(new BlobInfo
                {
                    key = item.TryGetProperty("name", out var n) ? n.get_string() ?? string.Empty : string.Empty,
                    size = item.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
                    last_modified = item.TryGetProperty("updated", out var u)
                        ? u.GetDateTime()
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
        var expiration = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        return $"https://{_endpoint}/storage/v1/b/{bucket}/o/{Uri.EscapeDataString(key)}?alt=media"
               + $"&access_token={Uri.EscapeDataString(_access_token)}"
               + $"&expires={expiration}";
    }
}
