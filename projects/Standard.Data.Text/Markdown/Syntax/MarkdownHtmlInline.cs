namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     HTML 行内标签节点
/// </summary>
public sealed record MarkdownHtmlInline : MarkdownNode
{
    public MarkdownHtmlInline(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.html_inline;


    /// <summary>
    ///     HTML 内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}