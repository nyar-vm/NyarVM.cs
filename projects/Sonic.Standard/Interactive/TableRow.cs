namespace Sonic.Interactive;

/// <summary>
/// 表格行，包含一组单元格数据
/// </summary>
public sealed class TableRow
{
    private readonly List<string?> _cells;

    /// <summary>
    /// 获取单元格列表
    /// </summary>
    public IReadOnlyList<string?> cells => _cells;

    /// <summary>
    /// 创建包含指定单元格的表格行
    /// </summary>
    /// <param name="cells">单元格内容数组</param>
    public TableRow(string[] cells)
    {
        _cells = new(cells.Length);
        foreach (var cell in cells)
        {
            _cells.Add(cell);
        }
    }
}
