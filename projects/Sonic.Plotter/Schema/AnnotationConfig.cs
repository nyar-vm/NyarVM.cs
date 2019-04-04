namespace Plotter.Schema;

/// <summary>
///     标注配置，定义图表中文本、线条和区域标注。
/// </summary>
public class AnnotationConfig
{
    /// <summary>
    ///     标注类型标识。
    /// </summary>
    public string annotation_type { get; set; } = "";

    /// <summary>
    ///     标注文本内容。
    /// </summary>
    public string? text { get; set; }

    /// <summary>
    ///     标注起始 X 坐标。
    /// </summary>
    public double? x { get; set; }

    /// <summary>
    ///     标注起始 Y 坐标。
    /// </summary>
    public double? y { get; set; }

    /// <summary>
    ///     标注结束 X 坐标（用于线段和区域标注）。
    /// </summary>
    public double? x2 { get; set; }

    /// <summary>
    ///     标注结束 Y 坐标（用于线段和区域标注）。
    /// </summary>
    public double? y2 { get; set; }
}