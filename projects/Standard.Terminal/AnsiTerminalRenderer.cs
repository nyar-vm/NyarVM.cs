using System.Text;
using Core.Console;
using Core.Terminal;

namespace Std.Terminal;

/// <summary>
///     基于 ANSI/VT 序列的终端渲染器默认实现，将结构化内容渲染为 ANSI 转义序列输出
/// </summary>
public sealed class AnsiTerminalRenderer : ITerminalRenderer
{
    private const string _ansi_reset = "\x1b[0m";

    /// <summary>
    ///     初始化 <see cref="AnsiTerminalRenderer" /> 的新实例
    /// </summary>
    /// <param name="console">控制台实例</param>
    /// <param name="theme">终端主题，若为 null 则使用默认主题</param>
    public AnsiTerminalRenderer(IConsole console, Theme? theme = null)
    {
        this.console = console;
        current_theme = theme ?? Themes.default_theme;
    }

    /// <summary>
    ///     获取关联的控制台实例
    /// </summary>
    public IConsole console { get; }

    /// <summary>
    ///     获取当前使用的主题
    /// </summary>
    public Theme current_theme { get; }

    /// <summary>
    ///     获取终端可用宽度
    /// </summary>
    public int width => console.buffer_width;

    /// <summary>
    ///     获取终端可用高度
    /// </summary>
    public int height => console.buffer_height;

    /// <summary>
    ///     向终端写入文本
    /// </summary>
    /// <param name="text">要写入的文本内容</param>
    void ITerminalRenderer.write(string text)
    {
        console.@out.Write(text);
    }

    /// <summary>
    ///     设置终端光标位置
    /// </summary>
    /// <param name="left">列位置</param>
    /// <param name="top">行位置</param>
    public void set_cursor_position(int left, int top)
    {
        console.set_cursor_position(left, top);
    }

    /// <summary>
    ///     清除终端屏幕
    /// </summary>
    public void clear()
    {
        console.clear();
    }

    /// <summary>
    ///     写入包含 ANSI 转义序列的文本
    /// </summary>
    /// <param name="text">要写入的 ANSI 字符串</param>
    public void write(AnsiString text)
    {
        console.@out.Write(text.ansi_sequence);
    }

    /// <summary>
    ///     写入纯文本行
    /// </summary>
    /// <param name="text">要写入的文本</param>
    public void write_line(string text)
    {
        console.@out.WriteLine(text);
    }

    /// <summary>
    ///     在终端中渲染指定可渲染布局
    /// </summary>
    /// <param name="renderable">要渲染的可渲染布局</param>
    public void render(IRenderable renderable)
    {
        var region = new Rectangle(0, console.cursor_top, console.buffer_width, renderable.desired_height);
        renderable.render(this, region);
    }

    /// <summary>
    ///     使用指定样式渲染文本，返回包含 ANSI 转义序列的 <see cref="AnsiString" />
    /// </summary>
    /// <param name="text">要渲染的纯文本</param>
    /// <param name="style">应用的样式</param>
    /// <returns>包含 ANSI 转义序列的字符串</returns>
    public AnsiString styled(string text, ConsoleStyle style)
    {
        if (!console.supports_ansi) return AnsiString.plain(text);

        var sb = new StringBuilder();
        sb.Append(style_to_ansi(style));
        sb.Append(text);
        sb.Append(_ansi_reset);
        return new AnsiString(text, sb.ToString());
    }

    /// <summary>
    ///     使用主题中的命名样式渲染文本
    /// </summary>
    /// <param name="text">要渲染的纯文本</param>
    /// <param name="styleName">主题中的样式名称</param>
    /// <returns>包含 ANSI 转义序列的字符串</returns>
    public AnsiString styled(string text, string styleName)
    {
        return styled(text, current_theme.get_style(styleName));
    }

    private static string style_to_ansi(ConsoleStyle style)
    {
        var sb = new StringBuilder("\x1b[");

        var first = true;

        if (style.bold)
        {
            sb.Append('1');
            first = false;
        }

        if (style.italic)
        {
            if (!first) sb.Append(';');

            sb.Append('3');
            first = false;
        }

        if (style.underline)
        {
            if (!first) sb.Append(';');

            sb.Append('4');
            first = false;
        }

        if (style.foreground.HasValue)
        {
            if (!first) sb.Append(';');

            sb.Append(color_to_ansi_foreground(style.foreground.Value));
            first = false;
        }

        if (style.background.HasValue)
        {
            if (!first) sb.Append(';');

            sb.Append(color_to_ansi_background(style.background.Value));
        }

        sb.Append('m');
        return sb.ToString();
    }

    private static string color_to_ansi_foreground(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "30",
            ConsoleColor.DarkRed => "31",
            ConsoleColor.DarkGreen => "32",
            ConsoleColor.DarkYellow => "33",
            ConsoleColor.DarkBlue => "34",
            ConsoleColor.DarkMagenta => "35",
            ConsoleColor.DarkCyan => "36",
            ConsoleColor.Gray => "37",
            ConsoleColor.DarkGray => "90",
            ConsoleColor.Red => "91",
            ConsoleColor.Green => "92",
            ConsoleColor.Yellow => "93",
            ConsoleColor.Blue => "94",
            ConsoleColor.Magenta => "95",
            ConsoleColor.Cyan => "96",
            ConsoleColor.White => "97",
            _ => "39"
        };
    }

    private static string color_to_ansi_background(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "40",
            ConsoleColor.DarkRed => "41",
            ConsoleColor.DarkGreen => "42",
            ConsoleColor.DarkYellow => "43",
            ConsoleColor.DarkBlue => "44",
            ConsoleColor.DarkMagenta => "45",
            ConsoleColor.DarkCyan => "46",
            ConsoleColor.Gray => "47",
            ConsoleColor.DarkGray => "100",
            ConsoleColor.Red => "101",
            ConsoleColor.Green => "102",
            ConsoleColor.Yellow => "103",
            ConsoleColor.Blue => "104",
            ConsoleColor.Magenta => "105",
            ConsoleColor.Cyan => "106",
            ConsoleColor.White => "107",
            _ => "49"
        };
    }
}