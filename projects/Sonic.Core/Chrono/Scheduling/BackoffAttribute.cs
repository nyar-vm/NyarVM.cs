using System;

namespace Core.Chrono.Scheduling;

/// <summary>
///     配置重试退避参数
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class BackoffAttribute : Attribute
{
    /// <summary>
    ///     初始延迟（毫秒），默认为 1000
    /// </summary>
    public int initial_delay { get; set; } = 1000;

    /// <summary>
    ///     最大延迟（毫秒），默认为 60000
    /// </summary>
    public int max_delay { get; set; } = 60000;

    /// <summary>
    ///     延迟乘数，默认为 2.0
    /// </summary>
    public double multiplier { get; set; } = 2.0;
}