namespace Atlas.Cloud.Models;

/// <summary>
/// 翻译请求，封装源文本和语言参数
/// </summary>
public sealed class TranslationRequest
{
    /// <summary>
    /// 待翻译的文本
    /// </summary>
    public string text { get; init; } = string.Empty;

    /// <summary>
    /// 源语言代码，默认 "auto" 表示自动检测
    /// </summary>
    public string source_lang { get; init; } = "auto";

    /// <summary>
    /// 目标语言代码，默认 "en"
    /// </summary>
    public string target_lang { get; init; } = "en";
}
