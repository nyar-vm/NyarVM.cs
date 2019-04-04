namespace Plotter.Grammar;

/// <summary>
///     图形类型枚举，定义所有支持的图表形状。
/// </summary>
public enum ShapeType
{
    /// <summary>
    ///     点图。
    /// </summary>
    Point,

    /// <summary>
    ///     折线图。
    /// </summary>
    Line,

    /// <summary>
    ///     柱状图。
    /// </summary>
    Bar,

    /// <summary>
    ///     条形图（水平柱状图）。
    /// </summary>
    HorizontalBar,

    /// <summary>
    ///     面积图。
    /// </summary>
    Area,

    /// <summary>
    ///     饼图。
    /// </summary>
    Pie,

    /// <summary>
    ///     雷达图。
    /// </summary>
    Radar,

    /// <summary>
    ///     热力图。
    /// </summary>
    Heatmap,

    /// <summary>
    ///     箱线图。
    /// </summary>
    BoxPlot
}