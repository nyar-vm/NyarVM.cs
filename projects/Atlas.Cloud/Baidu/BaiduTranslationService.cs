using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Baidu;

/// <summary>
/// 百度翻译服务实现，使用百度翻译 API 和 MD5 签名
/// </summary>
public sealed class BaiduTranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly string _app_id;
    private readonly string _secret_key;

    private const string _endpoint = "https://fanyi-api.baidu.com/api/trans/vip/translate";

    /// <summary>
    /// 初始化百度翻译服务
    /// </summary>
    /// <param name="appId">百度翻译应用 AppId</param>
    /// <param name="secretKey">百度翻译密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public BaiduTranslationService(string appId, string secretKey, HttpClient? httpClient = null)
    {
        _app_id = appId;
        _secret_key = secretKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<TranslationResult> translate(TranslationRequest request, CancellationToken ct = default)
    {
        var salt = Guid.NewGuid().ToString("N")[..16];
        var signInput = $"{_app_id}{request.text}{salt}{_secret_key}";
        var sign = md5_hex(signInput);

        var sourceLang = request.source_lang == "auto" ? "auto" : request.source_lang;

        var url = $"{_endpoint}?q={Uri.EscapeDataString(request.text)}&from={Uri.EscapeDataString(sourceLang)}&to={Uri.EscapeDataString(request.target_lang)}&appid={_app_id}&salt={salt}&sign={sign}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return TranslationResult.fail($"百度翻译失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("error_code", out var errorCode))
        {
            var errorMsg = root.TryGetProperty("error_msg", out var errorMsgElement)
                ? errorMsgElement.get_string() ?? "未知错误"
                : "未知错误";
            return TranslationResult.fail($"百度翻译错误: {errorCode.get_string()} - {errorMsg}");
        }

        if (root.TryGetProperty("trans_result", out var transResult)
            && transResult.GetArrayLength() > 0)
        {
            var translatedParts = new List<string>();

            foreach (var item in transResult.EnumerateArray())
            {
                if (item.TryGetProperty("dst", out var dst))
                {
                    translatedParts.Add(dst.get_string() ?? string.Empty);
                }
            }

            var translatedText = string.Join("\n", translatedParts);
            var detectedLang = root.TryGetProperty("from", out var from)
                ? from.get_string() ?? request.source_lang
                : request.source_lang;
            var targetLang = root.TryGetProperty("to", out var to)
                ? to.get_string() ?? request.target_lang
                : request.target_lang;

            return TranslationResult.ok(translatedText, detectedLang, targetLang);
        }

        return TranslationResult.fail("百度翻译响应缺少 trans_result 字段");
    }

    private static string md5_hex(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
