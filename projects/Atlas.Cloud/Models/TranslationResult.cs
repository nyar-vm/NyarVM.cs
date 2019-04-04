namespace Atlas.Cloud.Models;

/// <summary>
/// 翻译结果，包含译文和语言信息
/// </summary>
public sealed class TranslationResult
{
    /// <summary>
    /// 翻译后的文本
    /// </summary>
    public string translated_text { get; init; } = string.Empty;

    /// <summary>
    /// 检测到的源语言代码
    /// </summary>
    public string source_lang { get; init; } = string.Empty;

    /// <summary>
    /// 目标语言代码
    /// </summary>
    public string target_lang { get; init; } = string.Empty;

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="translated">翻译文本</param>
    /// <param name="source">源语言代码</param>
    /// <param name="target">目标语言代码</param>
    public static TranslationResult ok(string translated, string source, string target) =>
        new() { translated_text = translated, source_lang = source, target_lang = target };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static TranslationResult fail(string error) =>
        new() { error = error };
}
