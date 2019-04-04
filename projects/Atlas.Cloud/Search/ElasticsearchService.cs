using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Search;

/// <summary>
/// Elasticsearch / OpenSearch 搜索服务实现，使用 REST API 和可选 Basic Auth 认证
/// </summary>
public sealed class ElasticsearchService : ISearchService
{
    private readonly HttpClient _http;
    private readonly string[] _urls;

    /// <summary>
    /// 初始化 Elasticsearch 搜索服务
    /// </summary>
    /// <param name="urls">Elasticsearch 节点 URL 列表</param>
    /// <param name="username">用户名，为 null 时不使用认证</param>
    /// <param name="password">密码，为 null 时不使用认证</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public ElasticsearchService(string[] urls, string? username = null, string? password = null, HttpClient? httpClient = null)
    {
        _urls = urls;
        _http = httpClient ?? new HttpClient();

        if (username is not null && password is not null)
        {
            var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }
    }

    /// <inheritdoc />
    public async Task<SearchResult> search(SearchRequest request, CancellationToken ct = default)
    {
        var url = $"{get_base_url()}/{Uri.EscapeDataString(request.index_name)}/_search";

        var payload = new Dictionary<string, object>
        {
            ["query"] = new Dictionary<string, object>
            {
                ["match"] = new Dictionary<string, string>
                {
                    ["_all"] = request.query
                }
            },
            ["from"] = request.offset,
            ["size"] = request.limit
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
            return SearchResult.fail($"Elasticsearch 搜索失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var total = root.TryGetProperty("hits", out var hits)
                    && hits.TryGetProperty("total", out var totalElement)
            ? totalElement.TryGetProperty("value", out var value)
                ? value.GetInt32()
                : totalElement.GetInt32()
            : 0;

        var resultHits = new List<SearchHit>();

        if (root.TryGetProperty("hits", out var hitsNode)
            && hitsNode.TryGetProperty("hits", out var hitsArray))
        {
            foreach (var item in hitsArray.EnumerateArray())
            {
                var id = item.TryGetProperty("_id", out var idElement)
                    ? idElement.get_string() ?? string.Empty
                    : string.Empty;
                var score = item.TryGetProperty("_score", out var scoreElement)
                    ? scoreElement.GetDouble()
                    : 0.0;
                byte[]? source = null;

                if (item.TryGetProperty("_source", out var sourceElement))
                {
                    source = Encoding.UTF8.GetBytes(sourceElement.GetRawText());
                }

                resultHits.Add(new SearchHit { id = id, score = score, source = source });
            }
        }

        return SearchResult.ok(total, resultHits);
    }

    /// <inheritdoc />
    public async Task index(string indexName, string documentId, byte[] document, CancellationToken ct = default)
    {
        var url = $"{get_base_url()}/{Uri.EscapeDataString(indexName)}/_doc/{Uri.EscapeDataString(documentId)}";

        var content = new ByteArrayContent(document);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_index(string indexName, string documentId, CancellationToken ct = default)
    {
        var url = $"{get_base_url()}/{Uri.EscapeDataString(indexName)}/_doc/{Uri.EscapeDataString(documentId)}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }

    private string get_base_url()
    {
        if (_urls.Length == 0)
        {
            return "http://localhost:9200";
        }

        var index = Random.Shared.Next(_urls.Length);
        return _urls[index].TrimEnd('/');
    }
}
