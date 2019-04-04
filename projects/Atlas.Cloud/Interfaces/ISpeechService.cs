using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 语音服务抽象接口，支持语音合成和语音识别
/// </summary>
public interface ISpeechService
{
    /// <summary>
    /// 文本转语音
    /// </summary>
    /// <param name="request">语音合成请求参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>语音音频数据</returns>
    Task<byte[]> tts(TtsRequest request, CancellationToken ct = default);

    /// <summary>
    /// 语音识别
    /// </summary>
    /// <param name="audio">音频数据</param>
    /// <param name="format">音频格式，如 "wav"、"mp3"，为 null 时自动检测</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>识别结果</returns>
    Task<SpeechRecognitionResult> asr(byte[] audio, string? format = null, CancellationToken ct = default);
}
