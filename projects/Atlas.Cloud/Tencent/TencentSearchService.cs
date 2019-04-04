using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云 CSS (Cloud Search) 搜索服务实现，使用 TC3-HMAC-SHA256 签名
/// </summary>
public sealed class TencentSearchService : ISearchService
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _region;

    private const string _endpoint = "css.tencentcloudapi.com";
    private const string _service = "css";

    /// <summary>
    /// 初始化腾讯云 CSS 搜索服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 ap-guangzhou</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentSearchService(string secretId, string secretKey, string region = "ap-guangzhou", HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<SearchResult> search(SearchRequest request, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["IndexName"] = request.index_name,
            ["Query"] = new Dictionary<string, object>
            {
                ["QueryText"] = request.query
            },
            ["From"] = request.offset,
            ["Size"] = request.limit
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_endpoint}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        httpRequest.Headers.Add("X-TC-Action", "SearchDocuments");
        httpRequest.Headers.Add("X-TC-Version", "2022-04-21");
        httpRequest.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-TC-Region", _region);

        var auth = sign_v3(requestBody, timestamp);
        httpRequest.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SearchResult.fail($"腾讯云 CSS 搜索失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Response", out var resp))
        {
            if (resp.TryGetProperty("Error", out var error))
            {
                var message = error.TryGetProperty("Message", out var msg)
                    ? msg.get_string() ?? "未知错误"
                    : "未知错误";
                return SearchResult.fail($"腾讯云 CSS 搜索错误: {message}");
            }

            var total = resp.TryGetProperty("TotalCount", out var totalElement)
                ? totalElement.GetInt32()
                : 0;

            var hits = new List<SearchHit>();

            if (resp.TryGetProperty("Documents", out var documents))
            {
                foreach (var item in documents.EnumerateArray())
                {
                    var id = item.TryGetProperty("DocId", out var idElement)
                        ? idElement.get_string() ?? string.Empty
                        : string.Empty;
                    var score = item.TryGetProperty("Score", out var scoreElement)
                        ? scoreElement.GetDouble()
                        : 0.0;
                    byte[]? source = null;

                    if (item.TryGetProperty("Content", out var content))
                    {
                        source = Encoding.UTF8.GetBytes(content.GetRawText());
                    }

                    hits.Add(new SearchHit { id = id, score = score, source = source });
                }
            }

            return SearchResult.ok(total, hits);
        }

        return SearchResult.fail("腾讯云 CSS 搜索响应缺少 Response 字段");
    }

    /// <inheritdoc />
    public async Task index(string indexName, string documentId, byte[] document, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["IndexName"] = indexName,
            ["DocumentId"] = documentId,
            ["Content"] = JsonSerializer.Deserialize<Dictionary<string, object>>(document) ?? new Dictionary<string, object>()
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_endpoint}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        httpRequest.Headers.Add("X-TC-Action", "IndexDocument");
        httpRequest.Headers.Add("X-TC-Version", "2022-04-21");
        httpRequest.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-TC-Region", _region);

        var auth = sign_v3(requestBody, timestamp);
        httpRequest.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_index(string indexName, string documentId, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["IndexName"] = indexName,
            ["DocumentId"] = documentId
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_endpoint}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        httpRequest.Headers.Add("X-TC-Action", "DeleteDocument");
        httpRequest.Headers.Add("X-TC-Version", "2022-04-21");
        httpRequest.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-TC-Region", _region);

        var auth = sign_v3(requestBody, timestamp);
        httpRequest.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    private string sign_v3(string payload, long timestamp)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var canonicalRequest = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{_endpoint}\n\ncontent-type;host\n{sha256_hex(payload)}";
        var credentialScope = $"{date}/{_service}/tc3_request";
        var stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
        var secretService = hmac_sha256(secretDate, _service);
        var secretSigning = hmac_sha256(secretService, "tc3_request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"TC3-HMAC-SHA256 Credential={_secret_id}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
