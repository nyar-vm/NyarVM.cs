using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Volcengine;

/// <summary>
/// 火山引擎语音服务实现，使用火山引擎语音 REST API 和 HMAC-SHA256 签名
/// </summary>
public sealed class VolcengineSpeechService : ISpeechService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _app_id;

    private const string _tts_endpoint = "https://opens.volcengineapi.com/v1/tts";
    private const string _asr_endpoint = "https://opens.volcengineapi.com/v1/asr";

    /// <summary>
    /// 初始化火山引擎语音服务
    /// </summary>
    /// <param name="accessKey">AccessKey</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="appId">语音应用 AppId</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public VolcengineSpeechService(string accessKey, string secretKey, string appId, HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _app_id = appId;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<byte[]> tts(TtsRequest request, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["app"] = new Dictionary<string, object>
            {
                ["appid"] = _app_id,
                ["cluster"] = "volcano_tts"
            },
            ["user"] = new Dictionary<string, object>
            {
                ["uid"] = "default"
            },
            ["audio"] = new Dictionary<string, object>
            {
                ["voice_type"] = request.voice,
                ["encoding"] = request.format,
                ["speed_ratio"] = request.speed,
                ["volume_ratio"] = 1.0,
                ["pitch_ratio"] = 1.0
            },
            ["request"] = new Dictionary<string, object>
            {
                ["reqid"] = Guid.NewGuid().ToString("N"),
                ["text"] = request.text,
                ["text_type"] = "plain",
                ["operation"] = "query"
            }
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _tts_endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        sign_request(httpRequest, requestBody);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            var audioBase64 = data.get_string() ?? string.Empty;
            return Convert.FromBase64String(audioBase64);
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SpeechRecognitionResult> asr(byte[] audio, string? format = null, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["app"] = new Dictionary<string, object>
            {
                ["appid"] = _app_id,
                ["cluster"] = "volcano_asr"
            },
            ["user"] = new Dictionary<string, object>
            {
                ["uid"] = "default"
            },
            ["audio"] = new Dictionary<string, object>
            {
                ["format"] = format ?? "wav",
                ["rate"] = 16000,
                ["bits"] = 16,
                ["channel"] = 1,
                ["data"] = Convert.ToBase64String(audio)
            },
            ["request"] = new Dictionary<string, object>
            {
                ["reqid"] = Guid.NewGuid().ToString("N"),
                ["sequence"] = 1,
                ["nbest"] = 1,
                ["show_utterances"] = false
            }
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _asr_endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        sign_request(httpRequest, requestBody);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SpeechRecognitionResult.fail($"火山引擎语音识别失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("code", out var code) && code.GetInt32() != 0)
        {
            var message = root.TryGetProperty("message", out var msg)
                ? msg.get_string() ?? "未知错误"
                : "未知错误";
            return SpeechRecognitionResult.fail($"火山引擎语音识别错误: {message}");
        }

        if (root.TryGetProperty("result", out var result))
        {
            var text = result.TryGetProperty("text", out var textElement)
                ? textElement.get_string() ?? string.Empty
                : string.Empty;

            return SpeechRecognitionResult.ok(text, 1.0, "zh");
        }

        return SpeechRecognitionResult.fail("火山引擎语音识别响应缺少 result 字段");
    }

    private void sign_request(HttpRequestMessage request, string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");

        request.Headers.Add("X-Date", timestamp.ToString());
        request.Headers.Add("X-Access-Key", _access_key);
        request.Headers.Add("X-Nonce", nonce);

        var stringToSign = $"{request.Method.Method}\n{request.RequestUri?.AbsolutePath ?? "/"}\n{timestamp}\n{nonce}\n{sha256_hex(body)}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret_key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        request.Headers.Add("Authorization", $"HMAC-SHA256 Credential={_access_key}, Signature={signature}");
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
