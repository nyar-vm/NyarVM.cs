using System.Text.RegularExpressions;

namespace Std.Terminal;

/// <summary>
///     ANSI 转义序列工具类，生成终端样式控制序列
/// </summary>
public static class AnsiStyling
{
    public static string hide_cursor()
    {
        return "\u001b[?25l";
    }

    public static string show_cursor()
    {
        return "\u001b[?25h";
    }

    public static string clear_line()
    {
        return "\u001b[2K";
    }

    public static string clear_to_end_of_line()
    {
        return "\u001b[K";
    }

    public static string bold(string text)
    {
        return $"\u001b[1m{text}\u001b[0m";
    }

    public static string dim(string text)
    {
        return $"\u001b[2m{text}\u001b[0m";
    }

    public static string italic(string text)
    {
        return $"\u001b[3m{text}\u001b[0m";
    }

    public static string underline(string text)
    {
        return $"\u001b[4m{text}\u001b[0m";
    }

    public static string black(string text)
    {
        return $"\u001b[30m{text}\u001b[0m";
    }

    public static string red(string text)
    {
        return $"\u001b[31m{text}\u001b[0m";
    }

    public static string green(string text)
    {
        return $"\u001b[32m{text}\u001b[0m";
    }

    public static string yellow(string text)
    {
        return $"\u001b[33m{text}\u001b[0m";
    }

    public static string blue(string text)
    {
        return $"\u001b[34m{text}\u001b[0m";
    }

    public static string magenta(string text)
    {
        return $"\u001b[35m{text}\u001b[0m";
    }

    public static string cyan(string text)
    {
        return $"\u001b[36m{text}\u001b[0m";
    }

    public static string white(string text)
    {
        return $"\u001b[37m{text}\u001b[0m";
    }

    public static string bg_rgb(byte r, byte g, byte b, string text)
    {
        return $"\u001b[48;2;{r};{g};{b}m{text}\u001b[0m";
    }

    public static string rgb(byte r, byte g, byte b, string text)
    {
        return $"\u001b[38;2;{r};{g};{b}m{text}\u001b[0m";
    }

    public static string success(string text)
    {
        return green($"✓ {text}");
    }

    public static string warning(string text)
    {
        return yellow($"⚠ {text}");
    }

    public static string error(string text)
    {
        return red($"✗ {text}");
    }

    public static string info(string text)
    {
        return blue($"ℹ {text}");
    }

    public static string strip_ansi(string text)
    {
        return Regex.Replace(text, @"\u001b\[[0-9;]*[a-zA-Z]", string.Empty);
    }
}