using Core.Console;

namespace Std.Terminal.Components;

/// <summary>
///     文本输入提示组件，提示用户输入一行文本�?///
/// </summary>
public sealed class InputPrompt
{
    private readonly IConsole _console;

    /// <summary>
    ///     初始�?<see cref="InputPrompt" /> 的新实例�?    ///
    /// </summary>
    /// <param name="console">控制台实例�?/param>
    public InputPrompt(IConsole console)
    {
        _console = console;
    }

    /// <summary>
    ///     获取或设置提示消息�?    ///
    /// </summary>
    public string message { get; set; } = "";

    /// <summary>
    ///     获取或设置默认值�?    ///
    /// </summary>
    public string? default_value { get; set; }

    /// <summary>
    ///     显示提示并获取用户输入�?    ///
    /// </summary>
    /// <returns>用户输入的文本，若为空则返回默认值�?/returns>
    public string show()
    {
        if (!string.IsNullOrEmpty(message))
        {
            _console.@out.Write(message);
            if (default_value is not null) _console.@out.Write($" [{default_value}]");

            _console.@out.Write(": ");
        }

        var input = _console.@in.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? default_value ?? "" : input;
    }
}