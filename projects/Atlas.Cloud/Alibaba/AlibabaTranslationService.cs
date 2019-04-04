using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云翻译服务实现，使用阿里云机器翻译 API 和 HMAC-SHA1 签名
/// </summary>
public sealed class AlibabaTranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;

    private const string _endpoint = "mt.aliyuncs.com";

    /// <summary>
    /// 初始化阿里云翻译服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaTranslationService(string accessKeyId, string accessKeySecret, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<TranslationResult> translate(TranslationRequest request, CancellationToken ct = default)
    {
        var sourceLang = request.source_lang == "auto" ? "auto" : request.source_lang;

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = _access_key_id,
            ["Action"] = "TranslateGeneral",
            ["Format"] = "JSON",
            ["FormatType"] = "text",
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureVersion"] = "1.0",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["SourceLanguage"] = sourceLang,
            ["SourceText"] = request.text,
            ["TargetLanguage"] = request.target_lang,
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["Version"] = "2018-10-12"
        };

        var canonicalQuery = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var stringToSign = $"GET&{Uri.EscapeDataString("/")}&{Uri.EscapeDataString(canonicalQuery)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret + "&"));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var url = $"https://{_endpoint}/?{canonicalQuery}&Signature={Uri.EscapeDataString(signature)}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return TranslationResult.fail($"阿里云翻译失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Code", out var code) && code.get_string() != "200")
        {
            var message = root.TryGetProperty("Message", out var msg)
                ? msg.get_string() ?? "未知错误"
                : "未知错误";
            return TranslationResult.fail($"阿里云翻译错误: {message}");
        }

        var translatedText = root.TryGetProperty("Data", out var data)
                             && data.TryGetProperty("Translated", out var translated)
            ? translated.get_string() ?? string.Empty
            : string.Empty;

        var detectedLang = root.TryGetProperty("Data", out var dataNode)
                           && dataNode.TryGetProperty("SourceLanguage", out var srcLang)
            ? srcLang.get_string() ?? request.source_lang
            : request.source_lang;

        return TranslationResult.ok(translatedText, detectedLang, request.target_lang);
    }
}
