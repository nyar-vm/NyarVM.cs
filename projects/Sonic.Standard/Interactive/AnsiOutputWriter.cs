using Sonic.Console;
using Sonic.Terminal;
using Sonic.Console;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// ANSI 增强输出写入器，将 IOutputWriter 的样式化输出转换为 ANSI 转义序列
/// </summary>
public sealed class AnsiOutputWriter : IOutputWriter
{
    /// <summary>
    /// 共享单例实例
    /// </summary>
    public static readonly AnsiOutputWriter instance = new();

    private bool _is_redirected;
    private bool _no_color;

    /// <inheritdoc />
    public bool is_redirected => _is_redirected;

    /// <inheritdoc />
    public bool supports_styling => !_is_redirected && !_no_color;

    private AnsiOutputWriter()
    {
        try
        {
            _is_redirected = System.Console.IsOutputRedirected;
        }
        catch
        {
            _is_redirected = false;
        }

        var noColorEnv = Environment.GetEnvironmentVariable("NO_COLOR");
        _no_color = !string.IsNullOrEmpty(noColorEnv);
    }

    /// <inheritdoc />
    public void write(string? message)
    {
        System.Console.Write(message);
    }

    /// <inheritdoc />
    public void write_line(string? message)
    {
        System.Console.WriteLine(message);
    }

    /// <inheritdoc />
    public void write(string? message, Style style)
    {
        if (!supports_styling || message is null)
        {
            System.Console.Write(message);
            return;
        }

        var ansiCodes = build_style_codes(style);

        if (ansiCodes.Count > 0)
        {
            System.Console.Write($"\u001b[{string.Join(";", ansiCodes)}m");
        }

        System.Console.Write(message);
        System.Console.Write("\u001b[0m");
    }

    /// <inheritdoc />
    public void write_line(string? message, Style style)
    {
        if (!supports_styling || message is null)
        {
            System.Console.WriteLine(message);
            return;
        }

        var ansiCodes = build_style_codes(style);

        if (ansiCodes.Count > 0)
        {
            System.Console.Write($"\u001b[{string.Join(";", ansiCodes)}m");
        }

        System.Console.WriteLine(message);
        System.Console.Write("\u001b[0m");
    }

    /// <inheritdoc />
    public void write_error(string? message)
    {
        if (supports_styling && message is not null)
        {
            var errorRgb = Terminal.RgbColor.red;
            System.Console.Error.Write($"\u001b[38;2;{errorRgb.r};{errorRgb.g};{errorRgb.b}m");
            System.Console.Error.Write(message);
            System.Console.Error.Write("\u001b[0m");
        }
        else
        {
            System.Console.Error.Write(message);
        }
    }

    private static List<int> build_style_codes(Style style)
    {
        var codes = new List<int>();

        if (style.decoration.HasFlag(TextDecoration.bold))
        {
            codes.Add(1);
        }

        if (style.decoration.HasFlag(TextDecoration.italic))
        {
            codes.Add(3);
        }

        if (style.decoration.HasFlag(TextDecoration.underline))
        {
            codes.Add(4);
        }

        if (style.decoration.HasFlag(TextDecoration.strikethrough))
        {
            codes.Add(9);
        }

        if (style.foreground != Color.@default)
        {
            var rgb = map_color(style.foreground);
            codes.Add(38);
            codes.Add(2);
            codes.Add(rgb.r);
            codes.Add(rgb.g);
            codes.Add(rgb.b);
        }

        if (style.background != Color.@default)
        {
            var rgb = map_color(style.background);
            codes.Add(48);
            codes.Add(2);
            codes.Add(rgb.r);
            codes.Add(rgb.g);
            codes.Add(rgb.b);
        }

        return codes;
    }

    private static Terminal.RgbColor map_color(Color color)
    {
        return color switch
        {
            Color.black => Terminal.RgbColor.black,
            Color.red => Terminal.RgbColor.red,
            Color.green => Terminal.RgbColor.green,
            Color.yellow => Terminal.RgbColor.yellow,
            Color.blue => Terminal.RgbColor.blue,
            Color.magenta => Terminal.RgbColor.magenta,
            Color.cyan => Terminal.RgbColor.cyan,
            Color.white => Terminal.RgbColor.white,
            _ => Terminal.RgbColor.white,
        };
    }
}
