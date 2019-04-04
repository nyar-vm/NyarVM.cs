namespace Std.Data.Text.Markdown;

/// <summary>
///     目录项
/// </summary>
public sealed record TocItem
{
    /// <summary>
    ///     标题级别
    /// </summary>
    public int level { get; init; }


    /// <summary>
    ///     标题文本
    /// </summary>
    public string text { get; init; } = string.Empty;


    /// <summary>
    ///     锚点 ID
    /// </summary>
    public string id { get; init; } = string.Empty;
}