using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;
using Atlas.Cloud.OpenAI;

namespace Atlas.Cloud.Tongyi;

/// <summary>
/// 通义千问向量嵌入服务实现，调用通义 text-embedding API（OpenAI 兼容）
/// </summary>
public sealed class TongyiEmbedding : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化通义千问向量嵌入服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TongyiEmbedding(string apiKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = "https://dashscope.aliyuncs.com/compatible-mode/v1/embeddings";
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

            return OpenAiEmbedding.parse_openai_response(response_body);
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
}
