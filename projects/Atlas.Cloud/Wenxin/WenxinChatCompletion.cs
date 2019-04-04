using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Wenxin;

/// <summary>
/// 文心一言聊天补全服务实现，调用 ERNIE Bot API
/// </summary>
public sealed class WenxinChatCompletion : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _secret_key;

    /// <summary>
    /// 初始化文心一言聊天补全服务
    /// </summary>
    /// <param name="apiKey">API Key（client_id）</param>
    /// <param name="secretKey">Secret Key（client_secret）</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public WenxinChatCompletion(string apiKey, string secretKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _secret_key = secretKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ChatResponse> complete(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token_url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={_api_key}&client_secret={_secret_key}";
            using var token_request = new HttpRequestMessage(HttpMethod.Post, token_url);
            var token_response = await _http.SendAsync(token_request, cancellationToken);
            var token_body = await token_response.Content.ReadAsStringAsync(cancellationToken);

            using var token_doc = JsonDocument.Parse(token_body);
            var access_token = token_doc.RootElement.TryGetProperty("access_token", out var token_el)
                ? token_el.get_string() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrEmpty(access_token))
            {
                return new ChatResponse();
            }

            var url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/{request.model}?access_token={access_token}";

            var body = new
            {
                messages = request.messages.Select(m => new { role = m.role, content = m.content }),
                temperature = request.temperature
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, cancellationToken);
            var response_body = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var id_el) ? id_el.get_string() ?? string.Empty : string.Empty;
            var content = root.TryGetProperty("result", out var result_el) ? result_el.get_string() ?? string.Empty : string.Empty;
            var finish_reason = root.TryGetProperty("finish_reason", out var reason_el) ? reason_el.get_string() ?? string.Empty : string.Empty;

            var total_tokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                var prompt_tokens = usage.TryGetProperty("prompt_tokens", out var prompt_el) ? prompt_el.GetInt32() : 0;
                var completion_tokens = usage.TryGetProperty("completion_tokens", out var comp_el) ? comp_el.GetInt32() : 0;
                total_tokens = prompt_tokens + completion_tokens;
            }

            return new ChatResponse
            {
                id = id,
                model = request.model,
                message = new ChatMessage { role = "assistant", content = content },
                finish_reason = finish_reason,
                usage_total_tokens = total_tokens
            };
        }
        catch (HttpRequestException)
        {
            return new ChatResponse();
        }
        catch (JsonException)
        {
            return new ChatResponse();
        }
    }
}
