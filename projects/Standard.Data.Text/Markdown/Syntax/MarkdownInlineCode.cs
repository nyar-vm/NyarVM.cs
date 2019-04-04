namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     行内代码节点
/// </summary>
public sealed record MarkdownInlineCode : MarkdownNode
{
    public MarkdownInlineCode(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.inline_code;


    /// <summary>
    ///     代码内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}