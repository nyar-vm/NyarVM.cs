namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     行内数学公式节点 $...$
/// </summary>
public sealed record MarkdownMathInline : MarkdownNode
{
    public MarkdownMathInline(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.math_inline;


    /// <summary>
    ///     公式内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}