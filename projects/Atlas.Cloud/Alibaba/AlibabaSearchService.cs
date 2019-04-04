using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云 OpenSearch 搜索服务实现，使用阿里云 OpenSearch API 和 HMAC-SHA1 签名
/// </summary>
public sealed class AlibabaSearchService : ISearchService
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;
    private readonly string _app_name;

    private const string _endpoint = "opensearch.aliyuncs.com";

    /// <summary>
    /// 初始化阿里云 OpenSearch 搜索服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="appName">OpenSearch 应用名称</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaSearchService(string accessKeyId, string accessKeySecret, string appName, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _app_name = appName;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<SearchResult> search(SearchRequest request, CancellationToken ct = default)
    {
        var path = $"/v3/openapi/apps/{Uri.EscapeDataString(_app_name)}/search";
        var url = $"https://{_endpoint}{path}";

        var payload = new Dictionary<string, object>
        {
            ["query"] = request.query,
            ["from"] = request.offset,
            ["size"] = request.limit
        };

        var requestBody = JsonSerializer.Serialize(payload);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        sign_request(httpRequest, "POST", path, requestBody);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SearchResult.fail($"阿里云 OpenSearch 搜索失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var total = root.TryGetProperty("result", out var result)
                    && result.TryGetProperty("total", out var totalElement)
            ? totalElement.GetInt32()
            : 0;

        var hits = new List<SearchHit>();

        if (root.TryGetProperty("result", out var resultNode)
            && resultNode.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idElement)
                    ? idElement.get_string() ?? string.Empty
                    : string.Empty;
                var score = item.TryGetProperty("score", out var scoreElement)
                    ? scoreElement.GetDouble()
                    : 0.0;
                byte[]? source = null;

                if (item.TryGetProperty("fields", out var fields))
                {
                    source = Encoding.UTF8.GetBytes(fields.GetRawText());
                }

                hits.Add(new SearchHit { id = id, score = score, source = source });
            }
        }

        return SearchResult.ok(total, hits);
    }

    /// <inheritdoc />
    public async Task index(string indexName, string documentId, byte[] document, CancellationToken ct = default)
    {
        var path = $"/v3/openapi/apps/{Uri.EscapeDataString(_app_name)}/actions/kv";
        var url = $"https://{_endpoint}{path}";

        var payload = new Dictionary<string, object>
        {
            ["table_name"] = indexName,
            ["doc_id"] = documentId,
            ["fields"] = JsonSerializer.Deserialize<Dictionary<string, object>>(document) ?? new Dictionary<string, object>()
        };

        var requestBody = JsonSerializer.Serialize(payload);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        sign_request(httpRequest, "POST", path, requestBody);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_index(string indexName, string documentId, CancellationToken ct = default)
    {
        var path = $"/v3/openapi/apps/{Uri.EscapeDataString(_app_name)}/actions/kv/delete";
        var url = $"https://{_endpoint}{path}";

        var payload = new Dictionary<string, object>
        {
            ["table_name"] = indexName,
            ["doc_id"] = documentId
        };

        var requestBody = JsonSerializer.Serialize(payload);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        sign_request(httpRequest, "POST", path, requestBody);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    private void sign_request(HttpRequestMessage request, string method, string path, string body)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var nonce = Guid.NewGuid().ToString("N");

        request.Headers.Add("X-Opensearch-Nonce", nonce);
        request.Headers.Add("Date", timestamp);

        var contentMd5 = md5_hex(body);
        var contentType = "application/json";

        var stringToSign = $"{method}\n{contentMd5}\n{contentType}\n{timestamp}\n{path}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        request.Headers.Add("Authorization", $"OPENSEARCH {_access_key_id}:{signature}");
    }

    private static string md5_hex(string data)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
