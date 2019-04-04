using Core.Console;

namespace Std.Terminal.Components;

/// <summary>
///     确认提示组件，提示用户进行是/否确认�?///
/// </summary>
public sealed class ConfirmPrompt
{
    private readonly IConsole _console;

    /// <summary>
    ///     初始�?<see cref="ConfirmPrompt" /> 的新实例�?    ///
    /// </summary>
    /// <param name="console">控制台实例�?/param>
    public ConfirmPrompt(IConsole console)
    {
        _console = console;
    }

    /// <summary>
    ///     获取或设置提示消息�?    ///
    /// </summary>
    public string message { get; set; } = "确认?";

    /// <summary>
    ///     获取或设置默认值（用户直接按回车时的结果）�?    ///
    /// </summary>
    public bool default_value { get; set; } = true;

    /// <summary>
    ///     显示确认提示并获取用户响应�?    ///
    /// </summary>
    /// <returns>用户确认结果�?/returns>
    public bool show()
    {
        var hint = default_value ? "[Y/n]" : "[y/N]";
        _console.@out.Write($"{message} {hint}: ");

        var input = _console.@in.ReadLine()?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(input)) return default_value;

        return input is "y" or "yes";
    }
}