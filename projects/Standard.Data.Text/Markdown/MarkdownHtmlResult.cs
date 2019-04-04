using Std.Data.Text.Markdown.Syntax;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown HTML 渲染结果
/// </summary>
public sealed class MarkdownHtmlResult
{
    /// <summary>
    ///     渲染后的 HTML
    /// </summary>
    public string html { get; init; } = string.Empty;


    /// <summary>
    ///     目录项列表
    /// </summary>
    public IReadOnlyList<TocItem> toc_items { get; init; } = [];


    /// <summary>
    ///     脚注定义列表
    /// </summary>
    public IReadOnlyList<MarkdownFootnoteDefinition> footnotes { get; init; } =
        [];


    /// <summary>
    ///     是否包含 KaTeX 渲染的数学公式（需要引入 KaTeX CSS）
    /// </summary>
    public bool has_ka_te_x_math { get; init; }
}