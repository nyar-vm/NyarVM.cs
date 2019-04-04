namespace Plotter.Schema;

/// <summary>
///     视觉映射配置，定义数据字段到视觉通道的映射关系。
/// </summary>
public class VisualMappingConfig
{
    /// <summary>
    ///     X 轴字段名称。
    /// </summary>
    public string x_field { get; set; } = "";

    /// <summary>
    ///     Y 轴字段名称。
    /// </summary>
    public string y_field { get; set; } = "";

    /// <summary>
    ///     填充颜色字段名称。
    /// </summary>
    public string fill_color_field { get; set; } = "";

    /// <summary>
    ///     描边颜色字段名称。
    /// </summary>
    public string stroke_color_field { get; set; } = "";

    /// <summary>
    ///     大小字段名称。
    /// </summary>
    public string size_field { get; set; } = "";

    /// <summary>
    ///     固定填充颜色值。
    /// </summary>
    public string fixed_fill { get; set; } = "";

    /// <summary>
    ///     固定描边颜色值。
    /// </summary>
    public string fixed_stroke { get; set; } = "";

    /// <summary>
    ///     固定大小值。
    /// </summary>
    public double fixed_size { get; set; }
}