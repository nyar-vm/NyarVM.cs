namespace Core.Observability;

/// <summary>
///     日志级别枚举
/// </summary>
public enum LogLevel
{
    /// <summary>
    ///     跟踪级别
    /// </summary>
    trace,

    /// <summary>
    ///     调试级别
    /// </summary>
    debug,

    /// <summary>
    ///     信息级别
    /// </summary>
    info,

    /// <summary>
    ///     警告级别
    /// </summary>
    warning,

    /// <summary>
    ///     错误级别
    /// </summary>
    error,

    /// <summary>
    ///     严重错误级别
    /// </summary>
    critical
}