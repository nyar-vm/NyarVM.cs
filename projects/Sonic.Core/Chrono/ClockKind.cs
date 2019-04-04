namespace Core.Chrono;

/// <summary>
///     ClockKind 枚举
/// </summary>
public enum ClockKind
{
    /// <summary>
    ///     系统时钟
    /// </summary>
    system,

    /// <summary>
    ///     单调时钟
    /// </summary>
    monotonic,

    /// <summary>
    ///     稳定时钟
    /// </summary>
    steady
}