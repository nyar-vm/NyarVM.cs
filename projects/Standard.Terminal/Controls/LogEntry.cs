namespace Std.Terminal.Controls;

/// <summary>
///     日志条目
/// </summary>
public sealed class LogEntry
{
    /// <summary>时间戳</summary>
    public DateTime timestamp { get; init; } = DateTime.Now;

    /// <summary>日志级别</summary>
    public LogLevel level { get; init; }

    /// <summary>消息内容</summary>
    public string message { get; init; } = string.Empty;

    /// <summary>来源标识</summary>
    public string? source { get; init; }
}