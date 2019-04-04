using System.Diagnostics;
using Core.Observability;
using LogLevel = Core.Observability.LogLevel;

namespace Std.Observability;

/// <summary>
///     Sonic.Standard 日志记录器，提供结构化日志记录能力。
///     实现 <see cref="ISonicLogger" /> 接口。
/// </summary>
public sealed class SonicLogger : ISonicLogger
{
    /// <summary>
    ///     记录指定级别的日志消息。
    /// </summary>
    /// <param name="level">日志级别。</param>
    /// <param name="message">日志消息。</param>
    public void log(LogLevel level, string message)
    {
        var prefix = level switch
        {
            LogLevel.trace => "[TRC]",
            LogLevel.debug => "[DBG]",
            LogLevel.info => "[INF]",
            LogLevel.warning => "[WRN]",
            LogLevel.error => "[ERR]",
            LogLevel.critical => "[CRT]",
            _ => "[???]"
        };

        Debug.WriteLine($"{prefix} {message}");
    }

    /// <summary>
    ///     记录信息级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    public void log_info(string message)
    {
        log(LogLevel.info, message);
    }

    /// <summary>
    ///     记录警告级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    public void log_warning(string message)
    {
        log(LogLevel.warning, message);
    }

    /// <summary>
    ///     记录错误级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    public void log_error(string message)
    {
        log(LogLevel.error, message);
    }
}