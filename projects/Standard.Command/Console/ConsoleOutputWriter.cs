using Core.Console;
using Core.Terminal;

namespace Std.Console;

/// <summary>
///     控制台输出写入器，将 IOutputWriter 的样式化输出转换为 ANSI 转义序列
/// </summary>
public sealed class ConsoleOutputWriter : IOutputWriter
{
    /// <summary>
    ///     共享单例实例
    /// </summary>
    public static readonly ConsoleOutputWriter instance = new();

    /// <summary>
    ///     创建控制台输出写入器
    /// </summary>
    public ConsoleOutputWriter()
    {
        try
        {
            is_redirected = System.Console.IsOutputRedirected;
        }
        catch
        {
            is_redirected = false;
        }
    }

    /// <inheritdoc />
    public bool is_redirected { get; }

    /// <inheritdoc />
    public bool supports_styling
    {
        get
        {
            if (is_redirected) return false;

            var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
            return string.IsNullOrEmpty(noColor);
        }
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
        if (!supports_styling)
        {
            System.Console.Write(message);
            return;
        }

        write_with_style(message, style, false);
    }

    /// <inheritdoc />
    public void write_line(string? message, Style style)
    {
        if (!supports_styling)
        {
            System.Console.WriteLine(message);
            return;
        }

        write_with_style(message, style, true);
    }

    /// <inheritdoc />
    public void write_error(string? message)
    {
        System.Console.Error.Write(message);
    }

    private static void write_with_style(string? message, Style style, bool newLine)
    {
        var codes = build_ansi_codes(style);

        if (codes.Count > 0) System.Console.Write($"\u001b[{string.Join(";", codes)}m");

        if (newLine)
            System.Console.WriteLine(message);
        else
            System.Console.Write(message);

        if (codes.Count > 0) System.Console.Write("\u001b[0m");
    }

    private static List<int> build_ansi_codes(Style style)
    {
        var codes = new List<int>();

        if (style.decoration.HasFlag(TextDecoration.bold)) codes.Add(1);

        if (style.decoration.HasFlag(TextDecoration.italic)) codes.Add(3);

        if (style.decoration.HasFlag(TextDecoration.underline)) codes.Add(4);

        if (style.decoration.HasFlag(TextDecoration.strikethrough)) codes.Add(9);

        if (style.foreground != Color.@default) codes.Add(map_color(style.foreground, true));

        if (style.background != Color.@default) codes.Add(map_color(style.background, false));

        return codes;
    }

    private static int map_color(Color color, bool isForeground)
    {
        var offset = isForeground ? 30 : 40;
        return color switch
        {
            Color.black => offset + 0,
            Color.red => offset + 1,
            Color.green => offset + 2,
            Color.yellow => offset + 3,
            Color.blue => offset + 4,
            Color.magenta => offset + 5,
            Color.cyan => offset + 6,
            Color.white => offset + 7,
            _ => offset + 9
        };
    }
}
