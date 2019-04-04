namespace Core.Chrono;

/// <summary>
///     ITimeProvider 接口
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    ///     获取时钟实例
    /// </summary>
    IClock clock { get; }

    /// <summary>
    ///     获取单调时钟实例
    /// </summary>
    IMonotonicClock monotonic_clock { get; }
}