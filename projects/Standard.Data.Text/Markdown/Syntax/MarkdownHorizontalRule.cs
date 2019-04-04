namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     水平分割线节点
/// </summary>
public sealed record MarkdownHorizontalRule : MarkdownNode
{
    public override MarkdownNodeType node_type => MarkdownNodeType.horizontal_rule;
}