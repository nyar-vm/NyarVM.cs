using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云语音服务实现，使用腾讯云语音 REST API 和 TC3-HMAC-SHA256 签名
/// </summary>
public sealed class TencentSpeechService : ISpeechService
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _app_id;

    private const string _tts_host = "tts.tencentcloudapi.com";
    private const string _asr_host = "asr.tencentcloudapi.com";

    /// <summary>
    /// 初始化腾讯云语音服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="appId">语音应用 AppId</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentSpeechService(string secretId, string secretKey, string appId, HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _app_id = appId;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<byte[]> tts(TtsRequest request, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["Text"] = request.text,
            ["VoiceType"] = request.voice,
            ["Speed"] = (int)(request.speed * 100),
            ["Codec"] = request.format,
            ["SessionId"] = Guid.NewGuid().ToString("N")
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_tts_host}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        httpRequest.Headers.Add("X-TC-Action", "TextToVoice");
        httpRequest.Headers.Add("X-TC-Version", "2019-08-23");
        httpRequest.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-TC-Region", "ap-shanghai");

        var auth = sign_v3(requestBody, timestamp, _tts_host, "tts");
        httpRequest.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Response", out var resp)
            && resp.TryGetProperty("Audio", out var audioBase64))
        {
            return Convert.FromBase64String(audioBase64.get_string() ?? string.Empty);
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SpeechRecognitionResult> asr(byte[] audio, string? format = null, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["Data"] = Convert.ToBase64String(audio),
            ["DataLen"] = audio.Length,
            ["SourceType"] = 1,
            ["EngSerViceType"] = "16k_zh",
            ["VoiceFormat"] = format ?? "wav"
        };

        var requestBody = JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_asr_host}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        httpRequest.Headers.Add("X-TC-Action", "CreateRecTask");
        httpRequest.Headers.Add("X-TC-Version", "2019-08-23");
        httpRequest.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        httpRequest.Headers.Add("X-TC-Region", "ap-shanghai");

        var auth = sign_v3(requestBody, timestamp, _asr_host, "asr");
        httpRequest.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SpeechRecognitionResult.fail($"腾讯云语音识别失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Response", out var resp))
        {
            if (resp.TryGetProperty("Error", out var error))
            {
                var msg = error.TryGetProperty("Message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
                return SpeechRecognitionResult.fail(msg);
            }

            var text = resp.TryGetProperty("Result", out var result)
                ? result.get_string() ?? string.Empty
                : string.Empty;

            return SpeechRecognitionResult.ok(text, 1.0, "zh");
        }

        return SpeechRecognitionResult.fail("腾讯云语音识别响应缺少 Response 字段");
    }

    private string sign_v3(string payload, long timestamp, string host, string service)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var canonicalRequest = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{host}\n\ncontent-type;host\n{sha256_hex(payload)}";
        var credentialScope = $"{date}/{service}/tc3_request";
        var stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
        var secretService = hmac_sha256(secretDate, service);
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
