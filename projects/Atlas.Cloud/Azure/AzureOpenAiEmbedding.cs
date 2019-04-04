using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;
using Atlas.Cloud.OpenAI;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure OpenAI 向量嵌入服务实现，调用 Azure OpenAI Embeddings API
/// </summary>
public sealed class AzureOpenAiEmbedding : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 Azure OpenAI 向量嵌入服务
    /// </summary>
    /// <param name="resourceName">Azure 资源名称</param>
    /// <param name="deploymentId">部署标识</param>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureOpenAiEmbedding(string resourceName, string deploymentId, string apiKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = $"https://{resourceName}.openai.azure.com/openai/deployments/{deploymentId}/embeddings?api-version=2024-06-01";
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
            http_request.Headers.Add("api-key", _api_key);
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
