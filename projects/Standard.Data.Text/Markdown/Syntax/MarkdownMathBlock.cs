namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     块级数学公式节点 $$...$$
/// </summary>
public sealed record MarkdownMathBlock : MarkdownNode
{
    public MarkdownMathBlock(string content)
    {
        this.content = content;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.math_block;


    /// <summary>
    ///     公式内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}