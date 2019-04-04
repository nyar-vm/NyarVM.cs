namespace Std.Terminal.Controls;

/// <summary>
///     表格列定义
/// </summary>
public sealed class TableColumn
{
    /// <summary>
    ///     列标题
    /// </summary>
    public string header { get; set; } = string.Empty;

    /// <summary>
    ///     属性名（用于从数据对象取值）
    /// </summary>
    public string property_name { get; set; } = string.Empty;

    /// <summary>
    ///     列宽（字符数），0 表示自动
    /// </summary>
    public int width { get; set; }

    /// <summary>
    ///     最小列宽
    /// </summary>
    public int min_width { get; set; } = 4;

    /// <summary>
    ///     水平对齐
    /// </summary>
    public HorizontalAlignment alignment { get; set; } = HorizontalAlignment.left;

    /// <summary>
    ///     是否可排序
    /// </summary>
    public bool sortable { get; set; } = true;

    /// <summary>
    ///     自定义格式化委托
    /// </summary>
    public Func<object?, string>? formatter { get; set; }
}