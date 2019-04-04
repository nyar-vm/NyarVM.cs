namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     表格单元格节点
/// </summary>
public sealed record MarkdownTableCell : MarkdownNode
{
    public MarkdownTableCell(IReadOnlyList<MarkdownNode> children)
    {
        this.children = children;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.table_cell;


    /// <summary>
    ///     单元格内容
    /// </summary>
    public IReadOnlyList<MarkdownNode> children { get; init; } = [];
}