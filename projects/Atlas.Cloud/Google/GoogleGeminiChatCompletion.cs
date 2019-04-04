using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Google;

/// <summary>
/// Google Gemini 聊天补全服务实现，调用 Google Gemini REST API
/// </summary>
public sealed class GoogleGeminiChatCompletion : IChatCompletionService
{
    private readonly HttpClient _http;
    private readonly string _api_key;

    /// <summary>
    /// 初始化 Google Gemini 聊天补全服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public GoogleGeminiChatCompletion(string apiKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ChatResponse> complete(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{request.model}:generateContent?key={_api_key}";

            var body = new
            {
                contents = request.messages.Select(m => new
                {
                    role = m.role == "assistant" ? "model" : "user",
                    parts = new[] { new { text = m.content } }
                }),
                generationConfig = new
                {
                    temperature = request.temperature,
                    maxOutputTokens = request.max_tokens
                }
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, cancellationToken);
            var response_body = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var content = string.Empty;
            var finish_reason = string.Empty;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first_candidate = candidates[0];
                if (first_candidate.TryGetProperty("content", out var cand_content) &&
                    cand_content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    if (parts[0].TryGetProperty("text", out var text_el))
                    {
                        content = text_el.get_string() ?? string.Empty;
                    }
                }

                if (first_candidate.TryGetProperty("finishReason", out var reason_el))
                {
                    finish_reason = reason_el.get_string() ?? string.Empty;
                }
            }

            var total_tokens = 0;
            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                if (usage.TryGetProperty("totalTokenCount", out var tokens_el))
                {
                    total_tokens = tokens_el.GetInt32();
                }
            }

            return new ChatResponse
            {
                id = string.Empty,
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
