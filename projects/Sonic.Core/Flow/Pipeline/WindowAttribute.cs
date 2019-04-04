using System;

namespace Core.Flow.Pipeline;

/// <summary>
///     Window 属性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WindowAttribute : Attribute
{
    /// <summary>
    ///     窗口类型
    /// </summary>
    public WindowType type { get; set; } = WindowType.tumbling;


    /// <summary>
    ///     窗口大小
    /// </summary>
    public int size { get; set; } = 1;


    /// <summary>
    ///     时间单位
    /// </summary>
    public TimeUnit unit { get; set; } = TimeUnit.seconds;
}