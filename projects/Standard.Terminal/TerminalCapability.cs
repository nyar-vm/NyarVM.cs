namespace Std.Terminal;

/// <summary>
///     终端颜色级别枚举
/// </summary>
public enum TerminalColorLevel
{
    /// <summary>不支持颜色</summary>
    none = 0,

    /// <summary>基础 16 色</summary>
    basic = 1,

    /// <summary>256 色</summary>
    extended = 2,

    /// <summary>真彩色</summary>
    true_color = 3
}

/// <summary>
///     终端能力检测，判断当前终端是否支持 ANSI 转义序列、光标控制等
/// </summary>
public static class TerminalCapability
{
    /// <summary>
    ///     输出是否被重定向（文件或管道）
    /// </summary>
    public static bool is_output_redirected => System.Console.IsOutputRedirected;

    /// <summary>
    ///     当前终端的颜色级别
    /// </summary>
    public static TerminalColorLevel color_level
    {
        get
        {
            if (System.Console.IsOutputRedirected) return TerminalColorLevel.none;

            var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
            if (!string.IsNullOrEmpty(colorTerm))
                if (colorTerm.Equals("truecolor", StringComparison.OrdinalIgnoreCase)
                    || colorTerm.Equals("24bit", StringComparison.OrdinalIgnoreCase))
                    return TerminalColorLevel.true_color;

            var term = Environment.GetEnvironmentVariable("TERM") ?? string.Empty;
            if (term.Contains("256color", StringComparison.OrdinalIgnoreCase)) return TerminalColorLevel.extended;

            if (!string.IsNullOrEmpty(term) && term != "dumb") return TerminalColorLevel.basic;

            return TerminalColorLevel.none;
        }
    }

    /// <summary>
    ///     是否应该使用 ANSI 样式
    /// </summary>
    public static bool should_use_ansi => color_level != TerminalColorLevel.none;

    /// <summary>
    ///     终端是否支持光标控制（上下移动、设置位置等）
    /// </summary>
    public static bool supports_cursor_control => !System.Console.IsOutputRedirected
                                                  && Environment.GetEnvironmentVariable("TERM") != "dumb";
}