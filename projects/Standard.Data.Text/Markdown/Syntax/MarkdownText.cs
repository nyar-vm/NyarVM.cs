namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     纯文本节点
/// </summary>
public sealed record MarkdownText : MarkdownNode
{
    public MarkdownText(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.text;


    /// <summary>
    ///     文本内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}