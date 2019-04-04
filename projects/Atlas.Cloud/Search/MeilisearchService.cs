using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Search;

/// <summary>
/// Meilisearch 搜索服务实现，使用 Meilisearch REST API 和 Bearer Token 认证
/// </summary>
public sealed class MeilisearchService : ISearchService
{
    private readonly HttpClient _http;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 Meilisearch 搜索服务
    /// </summary>
    /// <param name="url">Meilisearch 服务地址</param>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public MeilisearchService(string url, string apiKey, HttpClient? httpClient = null)
    {
        _base_url = url.TrimEnd('/');
        _http = httpClient ?? new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    /// <inheritdoc />
    public async Task<SearchResult> search(SearchRequest request, CancellationToken ct = default)
    {
        var url = $"{_base_url}/indexes/{Uri.EscapeDataString(request.index_name)}/search";

        var payload = new Dictionary<string, object>
        {
            ["q"] = request.query,
            ["offset"] = request.offset,
            ["limit"] = request.limit
        };

        var requestBody = JsonSerializer.Serialize(payload);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SearchResult.fail($"Meilisearch 搜索失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var total = root.TryGetProperty("estimatedTotalHits", out var totalElement)
            ? totalElement.GetInt32()
            : root.TryGetProperty("totalHits", out var totalHits)
                ? totalHits.GetInt32()
                : 0;

        var hits = new List<SearchHit>();

        if (root.TryGetProperty("hits", out var hitsArray))
        {
            foreach (var item in hitsArray.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idElement)
                    ? idElement.ValueKind == JsonValueKind.Number
                        ? idElement.GetInt64().ToString()
                        : idElement.get_string() ?? string.Empty
                    : string.Empty;
                var score = item.TryGetProperty("_rankingScore", out var scoreElement)
                    ? scoreElement.GetDouble()
                    : 0.0;
                var source = Encoding.UTF8.GetBytes(item.GetRawText());

                hits.Add(new SearchHit { id = id, score = score, source = source });
            }
        }

        return SearchResult.ok(total, hits);
    }

    /// <inheritdoc />
    public async Task index(string indexName, string documentId, byte[] document, CancellationToken ct = default)
    {
        var url = $"{_base_url}/indexes/{Uri.EscapeDataString(indexName)}/documents/{Uri.EscapeDataString(documentId)}";

        var content = new ByteArrayContent(document);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_index(string indexName, string documentId, CancellationToken ct = default)
    {
        var url = $"{_base_url}/indexes/{Uri.EscapeDataString(indexName)}/documents/{Uri.EscapeDataString(documentId)}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }
}
