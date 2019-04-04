using System.Diagnostics;
using Core.Chrono;

namespace Std.State.Chrono;

/// <summary>
///     单调时钟，实现 IMonotonicClock 接口，提供单调递增的纳秒时间戳
/// </summary>
public sealed class MonotonicClock : IMonotonicClock
{
    /// <summary>
    ///     获取单调递增的纳秒时间戳
    /// </summary>
    public long nanoseconds => Stopwatch.GetTimestamp() * (1000000000L / Stopwatch.Frequency);
}