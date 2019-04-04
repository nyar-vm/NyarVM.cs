using Core.Console;
using Std.Text.Utf8;

namespace Std.Console;

/// <summary>
///     将 <see cref="IConsole" /> 桥接到 <see cref="IConsoleAdapter" /> 的适配器，
///     使现有 <c>SonicConsole</c> 静态类可以委托到新的 <c>IConsole</c> 实例。
/// </summary>
public sealed class ConsoleBridge : IConsoleAdapter
{
    private readonly IConsole _console;

    /// <summary>
    ///     初始化 <see cref="ConsoleBridge" /> 的新实例。
    /// </summary>
    /// <param name="console">被桥接的控制台实例。</param>
    public ConsoleBridge(IConsole console)
    {
        _console = console;
    }

    void IConsoleAdapter.write(string text)
    {
        _console.@out.Write(text);
    }

    void IConsoleAdapter.write_line(string text)
    {
        _console.@out.WriteLine(text);
    }

    string IConsoleAdapter.read_line()
    {
        return _console.@in.ReadLine() ?? "";
    }

    string IConsoleAdapter.read_all()
    {
        return _console.@in.ReadToEnd();
    }

    /// <summary>
    ///     输出字符串。
    /// </summary>
    /// <param name="text">要输出的文本。</param>
    public void write(Utf8Text text)
    {
        _console.@out.Write(text.ToString());
    }

    /// <summary>
    ///     输出字符串并换行。
    /// </summary>
    /// <param name="text">要输出的文本。</param>
    public void write_line(Utf8Text text)
    {
        _console.@out.WriteLine(text.ToString());
    }

    /// <summary>
    ///     读取一行输入。
    /// </summary>
    /// <returns>用户输入的文本行。</returns>
    public Utf8Text read_line()
    {
        var line = _console.@in.ReadLine();
        return Utf8Text.from_string(line ?? "");
    }

    /// <summary>
    ///     读取全部输入。
    /// </summary>
    /// <returns>全部输入文本。</returns>
    public Utf8Text read_all()
    {
        var text = _console.@in.ReadToEnd();
        return Utf8Text.from_string(text);
    }
}