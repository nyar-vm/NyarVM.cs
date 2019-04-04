using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Minimax;

/// <summary>
/// Minimax 聊天补全服务实现，调用 Minimax ChatCompletion V2 API
/// </summary>
public sealed class MinimaxChatCompletion : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _group_id;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 Minimax 聊天补全服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="groupId">群组标识</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public MinimaxChatCompletion(string apiKey, string groupId, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _group_id = groupId;
        _base_url = $"https://api.minimax.chat/v1/text/chatcompletion_v2?GroupId={groupId}";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ChatResponse> complete(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new
            {
                model = request.model,
                messages = request.messages.Select(m => new { role = m.role, content = m.content }),
                temperature = request.temperature,
                max_tokens = request.max_tokens
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("Authorization", $"Bearer {_api_key}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, cancellationToken);
            var response_body = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var id_el) ? id_el.get_string() ?? string.Empty : string.Empty;
            var model = root.TryGetProperty("model", out var model_el) ? model_el.get_string() ?? string.Empty : string.Empty;

            var content = string.Empty;
            var finish_reason = string.Empty;

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first_choice = choices[0];
                if (first_choice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content_el))
                {
                    content = content_el.get_string() ?? string.Empty;
                }

                if (first_choice.TryGetProperty("finish_reason", out var reason_el))
                {
                    finish_reason = reason_el.get_string() ?? string.Empty;
                }
            }

            var total_tokens = 0;
            if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var tokens_el))
            {
                total_tokens = tokens_el.GetInt32();
            }

            return new ChatResponse
            {
                id = id,
                model = model,
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
