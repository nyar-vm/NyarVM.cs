using System;

namespace Core.Console;

/// <summary>
///     控制台输入读取器接口
/// </summary>
public interface IInputReader
{
    /// <summary>
    ///     获取输入是否被重定向。
    /// </summary>
    bool is_redirected { get; }

    /// <summary>
    ///     读取一行输入。
    /// </summary>
    string? read_line();

    /// <summary>
    ///     读取一个按键。
    /// </summary>
    /// <param name="intercept">是否拦截按键而不显示。</param>
    ConsoleKeyInfo? read_key(bool intercept = false);

    /// <summary>
    ///     读取密码输入（不回显）。
    /// </summary>
    string? read_password();
}