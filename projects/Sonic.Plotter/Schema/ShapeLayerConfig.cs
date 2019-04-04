using Plotter.Grammar;

namespace Plotter.Schema;

/// <summary>
///     图形图层配置，定义图层的形状类型和样式。
/// </summary>
public class ShapeLayerConfig
{
    /// <summary>
    ///     图形类型。
    /// </summary>
    public ShapeType shape_type { get; set; }

    /// <summary>
    ///     线宽。
    /// </summary>
    public float line_width { get; set; } = 1.0f;

    /// <summary>
    ///     图层不透明度，范围 [0, 1]。
    /// </summary>
    public double layer_opacity { get; set; } = 1.0;
}