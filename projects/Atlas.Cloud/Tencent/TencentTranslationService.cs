using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云翻译服务实现，使用腾讯云翻译 API 和 TC3-HMAC-SHA256 签名
/// </summary>
public sealed class TencentTranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _region;

    private const string _endpoint = "tmt.tencentcloudapi.com";
    private const string _service = "tmt";

    /// <summary>
    /// 初始化腾讯云翻译服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 ap-beijing</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentTranslationService(string secretId, string secretKey, string region = "ap-beijing", HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<TranslationResult> translate(TranslationRequest request, CancellationToken ct = default)
    {
        var sourceLang = request.source_lang == "auto" ? "auto" : request.source_lang;

        var payload = new Dictionary<string, object>
        {
            ["SourceText"] = request.text,
            ["Source"] = sourceLang,
            ["Target"] = request.target_lang,
            ["ProjectId"] = 0
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_endpoint}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        httpRequest.Headers.Add("X-TC-Action", "TextTranslate");
        httpRequest.Headers.Add("X-TC-Version", "2018-03-21");
        httpRequest.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-TC-Region", _region);

        var auth = sign_v3(requestBody, timestamp);
        httpRequest.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return TranslationResult.fail($"腾讯云翻译失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Response", out var resp))
        {
            if (resp.TryGetProperty("Error", out var error))
            {
                var message = error.TryGetProperty("Message", out var msg)
                    ? msg.get_string() ?? "未知错误"
                    : "未知错误";
                return TranslationResult.fail($"腾讯云翻译错误: {message}");
            }

            var translatedText = resp.TryGetProperty("TargetText", out var targetText)
                ? targetText.get_string() ?? string.Empty
                : string.Empty;
            var source = resp.TryGetProperty("Source", out var src)
                ? src.get_string() ?? request.source_lang
                : request.source_lang;
            var target = resp.TryGetProperty("Target", out var tgt)
                ? tgt.get_string() ?? request.target_lang
                : request.target_lang;

            return TranslationResult.ok(translatedText, source, target);
        }

        return TranslationResult.fail("腾讯云翻译响应缺少 Response 字段");
    }

    private string sign_v3(string payload, long timestamp)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var canonicalRequest = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{_endpoint}\n\ncontent-type;host\n{sha256_hex(payload)}";
        var credentialScope = $"{date}/{_service}/tc3_request";
        var stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
        var secretService = hmac_sha256(secretDate, _service);
        var secretSigning = hmac_sha256(secretService, "tc3_request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"TC3-HMAC-SHA256 Credential={_secret_id}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
