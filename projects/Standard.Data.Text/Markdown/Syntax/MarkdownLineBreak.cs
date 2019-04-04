namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     硬换行节点
/// </summary>
public sealed record MarkdownLineBreak : MarkdownNode
{
    public override MarkdownNodeType node_type => MarkdownNodeType.line_break;
}