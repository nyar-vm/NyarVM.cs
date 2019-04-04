namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     表格节点
/// </summary>
public sealed record MarkdownTable : MarkdownNode
{
    public MarkdownTable(MarkdownTableRow header, IReadOnlyList<MarkdownTableRow> rows)
    {
        this.header = header;
        this.rows = rows;
    }

    public override MarkdownNodeType node_type => MarkdownNodeType.table;


    /// <summary>
    ///     表头行
    /// </summary>
    public MarkdownTableRow header { get; init; }


    /// <summary>
    ///     数据行
    /// </summary>
    public IReadOnlyList<MarkdownTableRow> rows { get; init; } = [];
}