namespace Core.Chrono.Scheduling;

/// <summary>
///     重试策略枚举
/// </summary>
public enum RetryPolicy
{
    /// <summary>
    ///     固定间隔重试
    /// </summary>
    fixed_interval,

    /// <summary>
    ///     指数退避重试
    /// </summary>
    exponential_backoff,

    /// <summary>
    ///     线性退避重试
    /// </summary>
    linear_backoff
}