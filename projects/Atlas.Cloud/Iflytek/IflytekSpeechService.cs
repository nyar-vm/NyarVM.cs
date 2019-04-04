using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Iflytek;

/// <summary>
/// 讯飞语音服务实现，使用讯飞语音 REST API 和 Bearer JWT 认证
/// </summary>
public sealed class IflytekSpeechService : ISpeechService
{
    private readonly HttpClient _http;
    private readonly string _app_id;
    private readonly string _api_key;
    private readonly string _api_secret;

    private const string _tts_endpoint = "https://tts-api.xfyun.cn/v2/tts";
    private const string _asr_endpoint = "https://iat-api.xfyun.cn/v2/iat";

    /// <summary>
    /// 初始化讯飞语音服务
    /// </summary>
    /// <param name="appId">讯飞应用 AppId</param>
    /// <param name="apiKey">API Key</param>
    /// <param name="apiSecret">API Secret</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public IflytekSpeechService(string appId, string apiKey, string apiSecret, HttpClient? httpClient = null)
    {
        _app_id = appId;
        _api_key = apiKey;
        _api_secret = apiSecret;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<byte[]> tts(TtsRequest request, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["header"] = new Dictionary<string, string>
            {
                ["app_id"] = _app_id,
                ["status"] = "2"
            },
            ["parameter"] = new Dictionary<string, object>
            {
                ["tts"] = new Dictionary<string, object>
                {
                    ["vcn"] = request.voice,
                    ["speed"] = (int)(request.speed * 100),
                    ["volume"] = 50,
                    ["pitch"] = 50,
                    ["audio"] = new Dictionary<string, string>
                    {
                        ["encoding"] = request.format,
                        ["sample_rate"] = "16000"
                    }
                }
            },
            ["payload"] = new Dictionary<string, object>
            {
                ["text"] = new Dictionary<string, object>
                {
                    ["encoding"] = "utf8",
                    ["text"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.text)),
                    ["status"] = "2"
                }
            }
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var token = generate_jwt();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _tts_endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SpeechRecognitionResult> asr(byte[] audio, string? format = null, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["header"] = new Dictionary<string, string>
            {
                ["app_id"] = _app_id,
                ["status"] = "2"
            },
            ["parameter"] = new Dictionary<string, object>
            {
                ["iat"] = new Dictionary<string, object>
                {
                    ["domain"] = "iat",
                    ["language"] = "zh_cn",
                    ["accent"] = "mandarin",
                    ["vad_eos"] = 2000,
                    ["dwa"] = "wpgs",
                    ["result"] = new Dictionary<string, object>
                    {
                        ["encoding"] = "utf8",
                        ["compress"] = "raw",
                        ["format"] = "json"
                    }
                }
            },
            ["payload"] = new Dictionary<string, object>
            {
                ["audio"] = new Dictionary<string, object>
                {
                    ["encoding"] = format ?? "raw",
                    ["sample_rate"] = 16000,
                    ["status"] = 2,
                    ["audio"] = Convert.ToBase64String(audio)
                }
            }
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var token = generate_jwt();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _asr_endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SpeechRecognitionResult.fail($"讯飞语音识别失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("header", out var header)
            && header.TryGetProperty("code", out var code)
            && code.GetInt32() != 0)
        {
            var message = header.TryGetProperty("message", out var msg)
                ? msg.get_string() ?? "未知错误"
                : "未知错误";
            return SpeechRecognitionResult.fail($"讯飞语音识别错误: {message}");
        }

        if (root.TryGetProperty("payload", out var resultPayload)
            && resultPayload.TryGetProperty("result", out var result)
            && result.TryGetProperty("text", out var textElement))
        {
            var decodedText = textElement.get_string() ?? string.Empty;
            return SpeechRecognitionResult.ok(decodedText, 1.0, "zh");
        }

        return SpeechRecognitionResult.fail("讯飞语音识别响应缺少 payload 字段");
    }

    private string generate_jwt()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = JsonSerializer.Serialize(new { alg = "HS256", sign_type = "signature", typ = "JWT" });
        var payloadObj = JsonSerializer.Serialize(new { iss = _api_key, sub = "iat", nbf = now, exp = now + 3600 });
        var headerBase64 = to_base64_url(header);
        var payloadBase64 = to_base64_url(payloadObj);
        var signContent = $"{headerBase64}.{payloadBase64}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_api_secret));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signContent));
        var signature = to_base64_url_raw(signatureBytes);

        return $"{signContent}.{signature}";
    }

    private static string to_base64_url(string data)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string to_base64_url_raw(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
