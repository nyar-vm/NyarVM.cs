using System;

namespace Core.State.Observable;

/// <summary>
///     Throttle 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ThrottleAttribute : Attribute
{
    /// <summary>
    ///     节流毫秒数
    /// </summary>
    public int milliseconds { get; set; } = 300;
}