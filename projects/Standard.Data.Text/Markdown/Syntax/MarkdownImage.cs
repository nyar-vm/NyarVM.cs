namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     图片节点
/// </summary>
public sealed record MarkdownImage : MarkdownNode
{
    public MarkdownImage(string url, string alt, string? title = null)
    {
        this.url = url;
        this.alt = alt;
        this.title = title;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.image;


    /// <summary>
    ///     图片地址
    /// </summary>
    public string url { get; init; } = string.Empty;


    /// <summary>
    ///     替代文本
    /// </summary>
    public string alt { get; init; } = string.Empty;


    /// <summary>
    ///     图片标题
    /// </summary>
    public string? title { get; init; }
}