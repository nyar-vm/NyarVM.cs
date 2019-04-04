using Core.Terminal;

namespace Core.Console;

/// <summary>
///     控制台输出写入器接口
/// </summary>
public interface IOutputWriter
{
    /// <summary>
    ///     获取输出是否被重定向。
    /// </summary>
    bool is_redirected { get; }

    /// <summary>
    ///     获取终端是否支持样式输出。
    /// </summary>
    bool supports_styling { get; }

    /// <summary>
    ///     写入消息。
    /// </summary>
    void write(string? message);

    /// <summary>
    ///     写入消息并换行。
    /// </summary>
    void write_line(string? message);

    /// <summary>
    ///     以指定样式写入消息。
    /// </summary>
    void write(string? message, Style style);

    /// <summary>
    ///     以指定样式写入消息并换行。
    /// </summary>
    void write_line(string? message, Style style);

    /// <summary>
    ///     写入错误消息。
    /// </summary>
    void write_error(string? message);
}