namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     表格行节点
/// </summary>
public sealed record MarkdownTableRow : MarkdownNode
{
    public MarkdownTableRow(IReadOnlyList<MarkdownTableCell> cells)
    {
        this.cells = cells;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.table_row;


    /// <summary>
    ///     单元格
    /// </summary>
    public IReadOnlyList<MarkdownTableCell> cells { get; init; } = [];
}