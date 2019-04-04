using System;

namespace Core.Chrono.Scheduling;

/// <summary>
///     标记方法或类启用重试机制
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RetryAttribute : Attribute
{
    /// <summary>
    ///     最大重试次数，默认为 3
    /// </summary>
    public int max_retries { get; set; } = 3;

    /// <summary>
    ///     重试策略，默认为指数退避
    /// </summary>
    public RetryPolicy policy { get; set; } = RetryPolicy.exponential_backoff;
}