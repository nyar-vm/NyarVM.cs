using Plotter.Grammar;

namespace Plotter.Schema;

/// <summary>
///     刻度配置，定义坐标轴的刻度方式和范围。
/// </summary>
public class ValueScaleConfig
{
    /// <summary>
    ///     X 轴刻度类型。
    /// </summary>
    public ScaleType x_scale_type { get; set; }

    /// <summary>
    ///     Y 轴刻度类型。
    /// </summary>
    public ScaleType y_scale_type { get; set; }

    /// <summary>
    ///     X 轴范围最小值。
    /// </summary>
    public double x_min { get; set; }

    /// <summary>
    ///     X 轴范围最大值。
    /// </summary>
    public double x_max { get; set; }

    /// <summary>
    ///     Y 轴范围最小值。
    /// </summary>
    public double y_min { get; set; }

    /// <summary>
    ///     Y 轴范围最大值。
    /// </summary>
    public double y_max { get; set; }

    /// <summary>
    ///     刻度数量。
    /// </summary>
    public int tick_count { get; set; } = 5;
}