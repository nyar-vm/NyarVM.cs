using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Aws;

/// <summary>
/// AWS 翻译服务实现，使用 AWS Translate API 和 Signature V4 签名
/// </summary>
public sealed class AwsTranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _service = "translate";

    /// <summary>
    /// 初始化 AWS 翻译服务
    /// </summary>
    /// <param name="accessKey">AWS Access Key</param>
    /// <param name="secretKey">AWS Secret Key</param>
    /// <param name="region">AWS 区域，如 us-east-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AwsTranslationService(string accessKey, string secretKey, string region = "us-east-1", HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<TranslationResult> translate(TranslationRequest request, CancellationToken ct = default)
    {
        var host = $"translate.{_region}.amazonaws.com";
        var url = $"https://{host}/";

        var payload = new Dictionary<string, object>
        {
            ["Text"] = request.text,
            ["SourceLanguageCode"] = request.source_lang == "auto" ? "auto" : request.source_lang,
            ["TargetLanguageCode"] = request.target_lang
        };

        var requestBody = JsonSerializer.Serialize(payload);

        var content = new StringContent(requestBody, Encoding.UTF8, "application/x-amz-json-1.1");
        content.Headers.Add("X-Amz-Target", "AWSShineFrontendService_20170701.TranslateText");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        sign_request_v4(httpRequest, host, requestBody);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return TranslationResult.fail($"AWS 翻译失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var translatedText = root.TryGetProperty("TranslatedText", out var translated)
            ? translated.get_string() ?? string.Empty
            : string.Empty;
        var sourceLang = root.TryGetProperty("SourceLanguageCode", out var srcLang)
            ? srcLang.get_string() ?? request.source_lang
            : request.source_lang;
        var targetLang = root.TryGetProperty("TargetLanguageCode", out var tgtLang)
            ? tgtLang.get_string() ?? request.target_lang
            : request.target_lang;

        return TranslationResult.ok(translatedText, sourceLang, targetLang);
    }

    private void sign_request_v4(HttpRequestMessage request, string host, string body)
    {
        var t = DateTimeOffset.UtcNow;
        var amzDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/aws4_request";

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Amz-Date", amzDate);

        var payloadHash = sha256_hex(body);
        request.Headers.Add("X-Amz-Content-Sha256", payloadHash);

        var signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date;x-amz-target";
        var canonicalRequest = $"POST\n/\n\n"
                               + $"content-type:application/x-amz-json-1.1\n"
                               + $"host:{host}\n"
                               + $"x-amz-content-sha256:{payloadHash}\n"
                               + $"x-amz-date:{amzDate}\n"
                               + $"x-amz-target:AWSShineFrontendService_20170701.TranslateText\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = hmac_sha256(hmac_sha256(hmac_sha256(hmac_sha256(
            Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), datestamp),
            _region), _service), "aws4_request");
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_access_key}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
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

    private static string hmac_sha256_hex(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
