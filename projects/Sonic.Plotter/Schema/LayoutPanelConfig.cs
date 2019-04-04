namespace Plotter.Schema;

/// <summary>
///     布局面板配置，定义多图布局方式。
/// </summary>
public class LayoutPanelConfig
{
    /// <summary>
    ///     行分割字段名称。
    /// </summary>
    public string? row_field { get; set; }

    /// <summary>
    ///     列分割字段名称。
    /// </summary>
    public string? column_field { get; set; }

    /// <summary>
    ///     行分割数。
    /// </summary>
    public int row_count { get; set; }

    /// <summary>
    ///     列分割数。
    /// </summary>
    public int column_count { get; set; }
}