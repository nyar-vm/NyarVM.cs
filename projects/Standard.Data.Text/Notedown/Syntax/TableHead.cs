namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     表格表头
/// </summary>
public sealed record TableHead
{
    /// <summary>
    ///     创建表头
    /// </summary>
    public TableHead(Attr attr, IReadOnlyList<TableRow> rows)
    {
        this.attr = attr;
        this.rows = rows;
    }

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     表头行
    /// </summary>
    public IReadOnlyList<TableRow> rows { get; init; }
}