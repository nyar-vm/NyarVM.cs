namespace Nyar.IR.Extractor;

/// <summary>
///     优化目标枚举
/// </summary>
public enum OptimizationGoal
{
    /// <summary>
    ///     最小化延迟
    /// </summary>
    minimize_latency,

    /// <summary>
    ///     最小化吞吐量
    /// </summary>
    minimize_throughput,

    /// <summary>
    ///     最小化内存
    /// </summary>
    minimize_memory,

    /// <summary>
    ///     最小化功耗
    /// </summary>
    minimize_power,

    /// <summary>
    ///     最小化面积
    /// </summary>
    minimize_area,

    /// <summary>
    ///     平衡
    /// </summary>
    balanced
}