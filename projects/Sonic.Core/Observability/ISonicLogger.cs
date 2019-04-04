namespace Core.Observability;

/// <summary>
///     Sonic.Standard 日志记录器接口
/// </summary>
public interface ISonicLogger
{
    /// <summary>
    ///     记录日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    void log(LogLevel level, string message);
}