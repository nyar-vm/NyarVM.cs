using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure 翻译服务实现，使用 Azure Translator REST API 和 Ocp-Apim-Subscription-Key 认证
/// </summary>
public sealed class AzureTranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly string _subscription_key;
    private readonly string _region;

    /// <summary>
    /// 初始化 Azure 翻译服务
    /// </summary>
    /// <param name="subscriptionKey">Azure 认知服务订阅密钥</param>
    /// <param name="region">Azure 区域，如 global</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureTranslationService(string subscriptionKey, string region = "global", HttpClient? httpClient = null)
    {
        _subscription_key = subscriptionKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<TranslationResult> translate(TranslationRequest request, CancellationToken ct = default)
    {
        var fromParam = request.source_lang == "auto" ? "" : $"&from={Uri.EscapeDataString(request.source_lang)}";
        var url = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0{fromParam}&to={Uri.EscapeDataString(request.target_lang)}";

        var payload = new[] { new Dictionary<string, string> { ["text"] = request.text } };
        var requestBody = JsonSerializer.Serialize(payload);

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _subscription_key);
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Region", _region);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return TranslationResult.fail($"Azure 翻译失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.GetArrayLength() == 0)
        {
            return TranslationResult.fail("Azure 翻译返回空结果");
        }

        var firstResult = root[0];

        var translatedText = firstResult.TryGetProperty("translations", out var translations)
                             && translations.GetArrayLength() > 0
                             && translations[0].TryGetProperty("text", out var text)
            ? text.get_string() ?? string.Empty
            : string.Empty;

        var detectedLang = firstResult.TryGetProperty("detectedLanguage", out var detected)
                           && detected.TryGetProperty("language", out var lang)
            ? lang.get_string() ?? request.source_lang
            : request.source_lang;

        return TranslationResult.ok(translatedText, detectedLang, request.target_lang);
    }
}
