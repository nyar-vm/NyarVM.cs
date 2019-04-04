namespace Core.Flow.Message;

/// <summary>
///     消息投递保证级别枚举
/// </summary>
public enum DeliveryGuarantee
{
    /// <summary>
    ///     最多一次
    /// </summary>
    at_most_once,

    /// <summary>
    ///     至少一次
    /// </summary>
    at_least_once,

    /// <summary>
    ///     恰好一次
    /// </summary>
    exactly_once
}