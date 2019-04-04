using System;

namespace Core.Security.Authorization;

/// <summary>
///     速率限制特性，控制单位时间内的请求次数
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RateLimitAttribute : Attribute
{
    /// <summary>
    ///     允许的请求次数，默认 100
    /// </summary>
    public int requests { get; set; } = 100;

    /// <summary>
    ///     时间窗口秒数，默认 60
    /// </summary>
    public int window_seconds { get; set; } = 60;
}