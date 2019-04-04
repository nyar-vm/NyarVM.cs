namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     HTML 块节点
/// </summary>
public sealed record MarkdownHtmlBlock : MarkdownNode
{
    public MarkdownHtmlBlock(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.html_block;


    /// <summary>
    ///     HTML 内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}