using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云智能语音服务实现，使用阿里云语音 REST API 和 HMAC-SHA1 签名
/// </summary>
public sealed class AlibabaSpeechService : ISpeechService
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;
    private readonly string _app_key;

    private const string _tts_endpoint = "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/tts";
    private const string _asr_endpoint = "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/asr";

    /// <summary>
    /// 初始化阿里云语音服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="appKey">智能语音应用 AppKey</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaSpeechService(string accessKeyId, string accessKeySecret, string appKey, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _app_key = appKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<byte[]> tts(TtsRequest request, CancellationToken ct = default)
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = _access_key_id,
            ["Action"] = "CreateTtsTask",
            ["AppKey"] = _app_key,
            ["Format"] = request.format,
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureVersion"] = "1.0",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["Text"] = request.text,
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["Version"] = "2019-02-28",
            ["Voice"] = request.voice,
            ["SpeechRate"] = ((int)(request.speed * 100)).ToString()
        };

        var canonicalQuery = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var stringToSign = $"GET&{Uri.EscapeDataString("/")}&{Uri.EscapeDataString(canonicalQuery)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret + "&"));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var url = $"{_tts_endpoint}?{canonicalQuery}&Signature={Uri.EscapeDataString(signature)}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("Authorization", $"ACS {_access_key_id}:{signature}");

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SpeechRecognitionResult> asr(byte[] audio, string? format = null, CancellationToken ct = default)
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = _access_key_id,
            ["Action"] = "CreateAsrTask",
            ["AppKey"] = _app_key,
            ["Format"] = format ?? "wav",
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureVersion"] = "1.0",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["Version"] = "2019-02-28"
        };

        var canonicalQuery = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var stringToSign = $"GET&{Uri.EscapeDataString("/")}&{Uri.EscapeDataString(canonicalQuery)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret + "&"));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var url = $"{_asr_endpoint}?{canonicalQuery}&Signature={Uri.EscapeDataString(signature)}";

        var content = new ByteArrayContent(audio);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        httpRequest.Headers.Add("Authorization", $"ACS {_access_key_id}:{signature}");

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SpeechRecognitionResult.fail($"阿里云语音识别失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Result", out var result))
        {
            var text = result.TryGetProperty("Sentence", out var sentence)
                ? sentence.get_string() ?? string.Empty
                : string.Empty;
            var confidence = result.TryGetProperty("Confidence", out var conf)
                ? conf.GetDouble()
                : 0.0;
            var language = result.TryGetProperty("Language", out var lang)
                ? lang.get_string() ?? string.Empty
                : string.Empty;

            return SpeechRecognitionResult.ok(text, confidence, language);
        }

        return SpeechRecognitionResult.fail("阿里云语音识别响应缺少 Result 字段");
    }
}
