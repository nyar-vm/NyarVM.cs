namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     表格行
/// </summary>
public sealed record TableRow
{
    /// <summary>
    ///     创建表格行
    /// </summary>
    public TableRow(IReadOnlyList<IReadOnlyList<NotedownInline>> cells)
    {
        this.cells = cells;
    }

    /// <summary>
    ///     单元格列表，每个单元格是一组行内内容
    /// </summary>
    public IReadOnlyList<IReadOnlyList<NotedownInline>> cells { get; init; }
}