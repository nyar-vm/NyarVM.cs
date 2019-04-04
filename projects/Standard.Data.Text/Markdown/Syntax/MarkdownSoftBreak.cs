namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     软换行节点（段落内单个换行）
/// </summary>
public sealed record MarkdownSoftBreak : MarkdownNode
{
    public override MarkdownNodeType node_type => MarkdownNodeType.soft_break;
}