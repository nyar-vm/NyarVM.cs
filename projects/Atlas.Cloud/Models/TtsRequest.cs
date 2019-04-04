namespace Atlas.Cloud.Models;

/// <summary>
/// 文本转语音请求，封装文本、音色、语速和格式参数
/// </summary>
public sealed class TtsRequest
{
    /// <summary>
    /// 待合成的文本内容
    /// </summary>
    public string text { get; init; } = string.Empty;

    /// <summary>
    /// 音色名称，默认 "default"
    /// </summary>
    public string voice { get; init; } = "default";

    /// <summary>
    /// 语速倍率，默认 1.0
    /// </summary>
    public double speed { get; init; } = 1.0;

    /// <summary>
    /// 输出音频格式，默认 "mp3"
    /// </summary>
    public string format { get; init; } = "mp3";
}
