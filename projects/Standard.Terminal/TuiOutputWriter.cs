using Core.Console;
using Core.Terminal;

namespace Std.Terminal;

/// <summary>
///     TUI 输出写入器，将 IOutputWriter 的文本输出映射到 ScreenBuffer
///     使用惰性缓冲引用，在 TuiApplication.RunAsync 期间绑定
/// </summary>
public sealed class TuiOutputWriter : IOutputWriter
{
    private readonly Func<ScreenBuffer> _buffer_provider;
    private RgbColor _current_bg;
    private RgbColor _current_fg;
    private int _cursor_x;
    private int _cursor_y;

    /// <summary>
    ///     创建 TUI 输出写入器
    /// </summary>
    /// <param name="bufferProvider">屏幕缓冲区惰性提供者</param>
    public TuiOutputWriter(Func<ScreenBuffer> bufferProvider)
    {
        _buffer_provider = bufferProvider;
        _cursor_x = 0;
        _cursor_y = 0;
        _current_fg = ScreenBuffer.default_foreground;
        _current_bg = ScreenBuffer.default_background;
    }

    private ScreenBuffer _buffer => _buffer_provider();

    /// <inheritdoc />
    public bool is_redirected => false;

    /// <inheritdoc />
    public bool supports_styling => true;

    /// <inheritdoc />
    public void write(string? message)
    {
        write_internal(message, null);
    }

    /// <inheritdoc />
    public void write_line(string? message)
    {
        write_internal(message, null);
        new_line();
    }

    /// <inheritdoc />
    public void write(string? message, Style style)
    {
        var fg = style.foreground != Color.@default ? map_color(style.foreground) : _current_fg;
        var bg = style.background != Color.@default ? map_color(style.background) : _current_bg;
        write_with_colors(message, fg, bg);
    }

    /// <inheritdoc />
    public void write_line(string? message, Style style)
    {
        var fg = style.foreground != Color.@default ? map_color(style.foreground) : _current_fg;
        var bg = style.background != Color.@default ? map_color(style.background) : _current_bg;
        write_with_colors(message, fg, bg);
        new_line();
    }

    /// <inheritdoc />
    public void write_error(string? message)
    {
        write_with_colors(message, RgbColor.red, _current_bg);
    }

    /// <summary>
    ///     重置光标到缓冲区原点
    /// </summary>
    public void reset_cursor()
    {
        _cursor_x = 0;
        _cursor_y = 0;
    }

    private void write_internal(string? message, Style? style)
    {
        if (message is null) return;

        var fg = style is not null && style.foreground != Color.@default
            ? map_color(style.foreground)
            : _current_fg;
        var bg = style is not null && style.background != Color.@default
            ? map_color(style.background)
            : _current_bg;

        write_with_colors(message, fg, bg);
    }

    private void write_with_colors(string? message, RgbColor fg, RgbColor bg)
    {
        if (message is null) return;

        _current_fg = fg;
        _current_bg = bg;

        foreach (var ch in message)
        {
            if (ch == '\n')
            {
                new_line();
                continue;
            }

            if (ch == '\r')
            {
                _cursor_x = 0;
                continue;
            }

            if (_cursor_y >= _buffer.height) scroll_up();

            if (_cursor_x >= _buffer.width) new_line();

            if (_cursor_y < _buffer.height && _cursor_x < _buffer.width)
                _buffer.set_char(_cursor_x, _cursor_y, ch, _current_fg, _current_bg);

            _cursor_x++;
        }
    }

    private static RgbColor map_color(Color color)
    {
        return color switch
        {
            Color.black => RgbColor.black,
            Color.red => RgbColor.red,
            Color.green => RgbColor.green,
            Color.yellow => RgbColor.yellow,
            Color.blue => RgbColor.blue,
            Color.magenta => RgbColor.magenta,
            Color.cyan => RgbColor.cyan,
            Color.white => RgbColor.white,
            _ => RgbColor.white
        };
    }

    private void new_line()
    {
        _cursor_x = 0;
        _cursor_y++;

        if (_cursor_y >= _buffer.height)
        {
            scroll_up();
            _cursor_y = _buffer.height - 1;
        }
    }

    private void scroll_up()
    {
        for (var y = 0; y < _buffer.height - 1; y++)
        for (var x = 0; x < _buffer.width; x++)
            _buffer.set_char(x, y,
                _buffer.get_char(x, y + 1),
                _buffer.get_foreground(x, y + 1),
                _buffer.get_background(x, y + 1));

        for (var x = 0; x < _buffer.width; x++)
            _buffer.set_char(x, _buffer.height - 1, ' ',
                ScreenBuffer.default_foreground,
                ScreenBuffer.default_background);
    }
}