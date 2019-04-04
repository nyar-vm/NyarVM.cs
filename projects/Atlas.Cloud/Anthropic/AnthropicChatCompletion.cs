using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Anthropic;

/// <summary>
/// Anthropic 聊天补全服务实现，调用 Anthropic Messages API
/// </summary>
public sealed class AnthropicChatCompletion : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 Anthropic 聊天补全服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基地址，默认为 Anthropic 官方端点</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AnthropicChatCompletion(string apiKey, string? baseUrl = null, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = baseUrl ?? "https://api.anthropic.com/v1/messages";
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
                max_tokens = request.max_tokens,
                messages = request.messages.Select(m => new { role = m.role, content = m.content }),
                temperature = request.temperature
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("x-api-key", _api_key);
            http_request.Headers.Add("anthropic-version", "2023-06-01");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, cancellationToken);
            var response_body = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var id_el) ? id_el.get_string() ?? string.Empty : string.Empty;
            var model = root.TryGetProperty("model", out var model_el) ? model_el.get_string() ?? string.Empty : string.Empty;
            var finish_reason = root.TryGetProperty("stop_reason", out var reason_el) ? reason_el.get_string() ?? string.Empty : string.Empty;

            var total_tokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                var input_tokens = usage.TryGetProperty("input_tokens", out var input_el) ? input_el.GetInt32() : 0;
                var output_tokens = usage.TryGetProperty("output_tokens", out var output_el) ? output_el.GetInt32() : 0;
                total_tokens = input_tokens + output_tokens;
            }

            var content = string.Empty;
            if (root.TryGetProperty("content", out var content_arr) && content_arr.GetArrayLength() > 0)
            {
                var first_block = content_arr[0];
                if (first_block.TryGetProperty("text", out var text_el))
                {
                    content = text_el.get_string() ?? string.Empty;
                }
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
