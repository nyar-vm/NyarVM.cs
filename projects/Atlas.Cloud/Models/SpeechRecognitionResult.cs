namespace Atlas.Cloud.Models;

/// <summary>
/// 语音识别结果，包含识别文本、置信度和语言
/// </summary>
public sealed class SpeechRecognitionResult
{
    /// <summary>
    /// 识别出的文本
    /// </summary>
    public string text { get; init; } = string.Empty;

    /// <summary>
    /// 识别置信度，范围 0~1
    /// </summary>
    public double confidence { get; init; }

    /// <summary>
    /// 检测到的语言代码，如 "zh"、"en"
    /// </summary>
    public string language { get; init; } = string.Empty;

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="text">识别文本</param>
    /// <param name="confidence">置信度</param>
    /// <param name="language">语言代码</param>
    public static SpeechRecognitionResult ok(string text, double confidence, string language) =>
        new() { text = text, confidence = confidence, language = language };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static SpeechRecognitionResult fail(string error) =>
        new() { error = error };
}
