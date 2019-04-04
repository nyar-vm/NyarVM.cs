namespace Plotter.Grammar;

/// <summary>
///     数据变换类型枚举，定义支持的数据聚合与变换方式。
/// </summary>
public enum TransformType
{
    /// <summary>
    ///     求和聚合。
    /// </summary>
    AggregateSum,

    /// <summary>
    ///     求均值聚合。
    /// </summary>
    AggregateAverage,

    /// <summary>
    ///     分组计数。
    /// </summary>
    GroupCount,

    /// <summary>
    ///     直方图分箱。
    /// </summary>
    BinHistogram,

    /// <summary>
    ///     平滑曲线。
    /// </summary>
    SmoothCurve,

    /// <summary>
    ///     密度估计。
    /// </summary>
    DensityEstimate
}