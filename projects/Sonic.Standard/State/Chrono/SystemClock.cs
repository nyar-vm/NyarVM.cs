using Core.Chrono;

namespace Std.State.Chrono;

/// <summary>
///     系统时钟，实现 IClock 接口，提供当前系统时间
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>
    ///     获取当前时间
    /// </summary>
    public DateTimeOffset now => DateTimeOffset.UtcNow;
}