namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     表格表体
/// </summary>
public sealed record TableBody
{
    /// <summary>
    ///     创建表体
    /// </summary>
    public TableBody(Attr attr, RowHeadColumns rowHeadColumns, IReadOnlyList<TableRow> headRows,
        IReadOnlyList<TableRow> rows)
    {
        this.attr = attr;
        row_head_columns = rowHeadColumns;
        head_rows = headRows;
        this.rows = rows;
    }

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     行标题列数
    /// </summary>
    public RowHeadColumns row_head_columns { get; init; }

    /// <summary>
    ///     中间表头行
    /// </summary>
    public IReadOnlyList<TableRow> head_rows { get; init; }

    /// <summary>
    ///     数据行
    /// </summary>
    public IReadOnlyList<TableRow> rows { get; init; }
}