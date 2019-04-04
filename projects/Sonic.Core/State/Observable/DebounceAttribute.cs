using System;

namespace Core.State.Observable;

/// <summary>
///     Debounce 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DebounceAttribute : Attribute
{
    /// <summary>
    ///     防抖毫秒数
    /// </summary>
    public int milliseconds { get; set; } = 300;
}