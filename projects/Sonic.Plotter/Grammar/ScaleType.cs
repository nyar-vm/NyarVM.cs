namespace Plotter.Grammar;

/// <summary>
///     刻度类型枚举，定义坐标轴的刻度方式。
/// </summary>
public enum ScaleType
{
    /// <summary>
    ///     数值刻度。
    /// </summary>
    Numeric,

    /// <summary>
    ///     时间刻度。
    /// </summary>
    Time,

    /// <summary>
    ///     分类刻度。
    /// </summary>
    Category,

    /// <summary>
    ///     对数刻度。
    /// </summary>
    Logarithmic
}