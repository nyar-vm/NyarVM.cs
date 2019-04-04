using Core.Chrono;

namespace Std.State.Chrono;

/// <summary>
///     时间提供者，实现 ITimeProvider 接口，提供时钟和单调时钟实例
/// </summary>
public sealed class TimeProvider : ITimeProvider
{
    /// <summary>
    ///     初始化时间提供者
    /// </summary>
    /// <param name="clock">时钟实例</param>
    /// <param name="monotonicClock">单调时钟实例</param>
    public TimeProvider(IClock? clock = null, IMonotonicClock? monotonicClock = null)
    {
        this.clock = clock ?? new SystemClock();
        monotonic_clock = monotonicClock ?? new MonotonicClock();
    }

    /// <summary>
    ///     获取时钟实例
    /// </summary>
    public IClock clock { get; }

    /// <summary>
    ///     获取单调时钟实例
    /// </summary>
    public IMonotonicClock monotonic_clock { get; }
}