namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     表格表脚
/// </summary>
public sealed record TableFoot
{
    /// <summary>
    ///     创建表脚
    /// </summary>
    public TableFoot(Attr attr, IReadOnlyList<TableRow> rows)
    {
        this.attr = attr;
        this.rows = rows;
    }

    /// <summary>
    ///     属性
    /// </summary>
    public Attr attr { get; init; }

    /// <summary>
    ///     表脚行
    /// </summary>
    public IReadOnlyList<TableRow> rows { get; init; }
}