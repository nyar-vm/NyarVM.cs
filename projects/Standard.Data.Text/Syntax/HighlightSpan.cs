namespace Std.Data.Text.Syntax;

/// <summary>
///     语法高亮片段，表示源码中一段具有特定语法类别的文本范围
/// </summary>
public sealed class HighlightSpan
{
    /// <summary>
    ///     语法类别
    /// </summary>
    public HighlightKind kind { get; init; }

    /// <summary>
    ///     起始偏移量
    /// </summary>
    public int offset { get; init; }

    /// <summary>
    ///     长度
    /// </summary>
    public int length { get; init; }
}