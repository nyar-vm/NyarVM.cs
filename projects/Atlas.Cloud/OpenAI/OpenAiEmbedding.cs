using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.OpenAI;

/// <summary>
/// OpenAI 向量嵌入服务实现，调用 OpenAI Embeddings API
/// </summary>
public sealed class OpenAiEmbedding : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 OpenAI 向量嵌入服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基地址，默认为 OpenAI 官方端点</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public OpenAiEmbedding(string apiKey, string? baseUrl = null, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = baseUrl ?? "https://api.openai.com/v1/embeddings";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<EmbeddingResponse> embed(EmbeddingRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = request.model,
                ["input"] = request.input
            };

            if (request.dimensions.HasValue)
            {
                body["dimensions"] = request.dimensions.Value;
            }

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("Authorization", $"Bearer {_api_key}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            return parse_openai_response(response_body);
        }
        catch (HttpRequestException ex)
        {
            return EmbeddingResponse.fail(ex.Message);
        }
        catch (JsonException ex)
        {
            return EmbeddingResponse.fail(ex.Message);
        }
    }

    internal static EmbeddingResponse parse_openai_response(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var model = root.TryGetProperty("model", out var model_el) ? model_el.get_string() ?? string.Empty : string.Empty;

        var total_tokens = 0;
        if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var tokens_el))
        {
            total_tokens = tokens_el.GetInt32();
        }

        var embeddings = new List<float[]>();
        if (root.TryGetProperty("data", out var data))
        {
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("embedding", out var emb))
                {
                    var vec = emb.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    embeddings.Add(vec);
                }
            }
        }

        return EmbeddingResponse.ok(model, embeddings, total_tokens);
    }
}
