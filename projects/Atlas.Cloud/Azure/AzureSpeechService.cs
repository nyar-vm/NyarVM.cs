using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure 语音服务实现，使用 Azure Speech Services REST API 和 Ocp-Apim-Subscription-Key 认证
/// </summary>
public sealed class AzureSpeechService : ISpeechService
{
    private readonly HttpClient _http;
    private readonly string _subscription_key;
    private readonly string _region;

    /// <summary>
    /// 初始化 Azure 语音服务
    /// </summary>
    /// <param name="subscriptionKey">Azure 认知服务订阅密钥</param>
    /// <param name="region">Azure 区域，如 eastus</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureSpeechService(string subscriptionKey, string region = "eastus", HttpClient? httpClient = null)
    {
        _subscription_key = subscriptionKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<byte[]> tts(TtsRequest request, CancellationToken ct = default)
    {
        var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/v1";

        var ssml = $@"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
  <voice name='{request.voice}'>
    <prosody rate='{request.speed}'>
      {System.Security.SecurityElement.Escape(request.text)}
    </prosody>
  </voice>
</speak>";

        var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _subscription_key);
        httpRequest.Headers.Add("X-Microsoft-OutputFormat", get_output_format(request.format));

        var response = await _http.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SpeechRecognitionResult> asr(byte[] audio, string? format = null, CancellationToken ct = default)
    {
        var url = $"https://{_region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=zh-CN";

        var contentType = get_content_type(format);
        var content = new ByteArrayContent(audio);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _subscription_key);

        var response = await _http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return SpeechRecognitionResult.fail($"Azure 语音识别失败: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var recognitionStatus = root.TryGetProperty("RecognitionStatus", out var status)
            ? status.get_string() ?? string.Empty
            : string.Empty;

        if (recognitionStatus != "Success")
        {
            return SpeechRecognitionResult.fail($"Azure 语音识别状态: {recognitionStatus}");
        }

        var text = root.TryGetProperty("DisplayText", out var displayText)
            ? displayText.get_string() ?? string.Empty
            : string.Empty;
        var confidence = root.TryGetProperty("NBest", out var nBest)
                         && nBest.GetArrayLength() > 0
                         && nBest[0].TryGetProperty("Confidence", out var conf)
            ? conf.GetDouble()
            : 1.0;
        var language = root.TryGetProperty("Language", out var lang)
            ? lang.get_string() ?? "zh-CN"
            : "zh-CN";

        return SpeechRecognitionResult.ok(text, confidence, language);
    }

    private static string get_output_format(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "audio-16khz-128kbitrate-mono-mp3",
            "wav" => "riff-16khz-16bit-mono-pcm",
            "ogg" => "ogg-16khz-16bit-mono-opus",
            _ => "audio-16khz-128kbitrate-mono-mp3"
        };
    }

    private static string get_content_type(string? format)
    {
        return (format ?? "wav").ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "ogg" => "audio/ogg",
            "flac" => "audio/flac",
            _ => "audio/wav"
        };
    }
}
