namespace Core.Observability;

/// <summary>
///     指标类型枚举
/// </summary>
public enum MetricType
{
    /// <summary>
    ///     计数器，单调递增
    /// </summary>
    counter,

    /// <summary>
    ///     仪表盘，可增可减
    /// </summary>
    gauge,

    /// <summary>
    ///     直方图，分布统计
    /// </summary>
    histogram
}