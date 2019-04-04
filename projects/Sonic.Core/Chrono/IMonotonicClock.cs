namespace Core.Chrono;

/// <summary>
///     IMonotonicClock 接口
/// </summary>
public interface IMonotonicClock
{
    /// <summary>
    ///     获取单调递增的纳秒时间戳
    /// </summary>
    long nanoseconds { get; }
}