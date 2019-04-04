using Sonic.Console;
using Sonic.Terminal;
using Sonic.Console;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// REPL 输出写入器，委托给内部 IOutputWriter 实现
/// 允许在 REPL 输出流中插入缩进或前缀
/// </summary>
public sealed class ReplOutputWriter : IOutputWriter
{
    private readonly IOutputWriter _inner;

    /// <inheritdoc />
    public bool is_redirected => _inner.is_redirected;

    /// <inheritdoc />
    public bool supports_styling => _inner.supports_styling;

    /// <summary>
    /// 创建 REPL 输出写入器
    /// </summary>
    /// <param name="inner">底层输出实现</param>
    public ReplOutputWriter(IOutputWriter inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public void write(string? message)
    {
        _inner.write(message);
    }

    /// <inheritdoc />
    public void write_line(string? message)
    {
        _inner.write_line(message);
    }

    /// <inheritdoc />
    public void write(string? message, Style style)
    {
        _inner.write(message, style);
    }

    /// <inheritdoc />
    public void write_line(string? message, Style style)
    {
        _inner.write_line(message, style);
    }

    /// <inheritdoc />
    public void write_error(string? message)
    {
        _inner.write_error(message);
    }
}
