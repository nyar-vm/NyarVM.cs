namespace Plotter.Grammar;

/// <summary>
///     数据点，表示图表中一个可视化的数据单元。
/// </summary>
public readonly struct DataPoint
{
    /// <summary>
    ///     X 轴数值。
    /// </summary>
    public readonly double x;

    /// <summary>
    ///     Y 轴数值。
    /// </summary>
    public readonly double y;

    /// <summary>
    ///     分类标签，用于分组或着色。
    /// </summary>
    public readonly string? category;

    /// <summary>
    ///     数据点大小权重，默认为 1.0。
    /// </summary>
    public readonly double size;

    /// <summary>
    ///     初始化 <see cref="DataPoint" /> 结构体的新实例。
    /// </summary>
    /// <param name="x">X 轴数值。</param>
    /// <param name="y">Y 轴数值。</param>
    /// <param name="category">分类标签，默认为 null。</param>
    /// <param name="size">大小权重，默认为 1.0。</param>
    public DataPoint(double x, double y, string? category = null, double size = 1.0)
    {
        this.x = x;
        this.y = y;
        this.category = category;
        this.size = size;
    }
}