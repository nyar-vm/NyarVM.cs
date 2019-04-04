namespace Sonic.Interactive;

/// <summary>
/// 表格列定义
/// </summary>
public sealed class TableColumn
{
    /// <summary>
    /// 列标题
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 列宽（0 或负数表示自动）
    /// </summary>
    public int width { get; set; }

    /// <summary>
    /// 对齐方式
    /// </summary>
    public ColumnAlignment alignment { get; set; } = ColumnAlignment.left;

    /// <summary>
    /// 创建表格列
    /// </summary>
    /// <param name="title">列标题</param>
    /// <param name="width">列宽（0 或负数表示自动）</param>
    /// <param name="alignment">对齐方式</param>
    public TableColumn(string title, int width = 0, ColumnAlignment alignment = ColumnAlignment.left)
    {
        this.title = title;
        this.width = width;
        this.alignment = alignment;
    }
}