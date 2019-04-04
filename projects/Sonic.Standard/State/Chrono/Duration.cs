using Core.Chrono;

namespace Std.State.Chrono;

/// <summary>
///     时间段实现，实现 IDuration 接口，表示一段时间间隔
/// </summary>
public readonly struct Duration : IDuration
{
    /// <summary>
    ///     总纳秒数
    /// </summary>
    public long total_nanoseconds { get; }

    /// <summary>
    ///     初始化时间段
    /// </summary>
    /// <param name="nanoseconds">纳秒数</param>
    public Duration(long nanoseconds)
    {
        total_nanoseconds = nanoseconds;
    }

    /// <summary>
    ///     从毫秒数创建时间段
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    /// <returns>时间段实例</returns>
    public static Duration from_milliseconds(double milliseconds)
    {
        return new Duration((long)(milliseconds * 1_000_000));
    }

    /// <summary>
    ///     从秒数创建时间段
    /// </summary>
    /// <param name="seconds">秒数</param>
    /// <returns>时间段实例</returns>
    public static Duration from_seconds(double seconds)
    {
        return new Duration((long)(seconds * 1_000_000_000));
    }
}