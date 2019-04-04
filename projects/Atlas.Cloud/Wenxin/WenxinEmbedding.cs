using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Wenxin;

/// <summary>
/// 文心向量嵌入服务实现，调用文心 Embedding API
/// </summary>
public sealed class WenxinEmbedding : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _secret_key;

    /// <summary>
    /// 初始化文心向量嵌入服务
    /// </summary>
    /// <param name="apiKey">API Key（client_id）</param>
    /// <param name="secretKey">Secret Key（client_secret）</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public WenxinEmbedding(string apiKey, string secretKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _secret_key = secretKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<EmbeddingResponse> embed(EmbeddingRequest request, CancellationToken ct = default)
    {
        try
        {
            var token_url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={_api_key}&client_secret={_secret_key}";
            using var token_request = new HttpRequestMessage(HttpMethod.Post, token_url);
            var token_response = await _http.SendAsync(token_request, ct);
            var token_body = await token_response.Content.ReadAsStringAsync(ct);

            using var token_doc = JsonDocument.Parse(token_body);
            var access_token = token_doc.RootElement.TryGetProperty("access_token", out var token_el)
                ? token_el.get_string() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(access_token))
            {
                return EmbeddingResponse.fail("获取 access_token 失败");
            }

            var url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/embeddings/embedding-v1?access_token={access_token}";

            var body = new
            {
                input = request.input
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

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

            return EmbeddingResponse.ok(request.model, embeddings, total_tokens);
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
