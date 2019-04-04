namespace Std.Terminal;

/// <summary>
///     差分渲染引擎，对比新旧帧缓冲区，仅输出变化部分
/// </summary>
public sealed class DifferentialRenderer
{
    private readonly bool _has_console;
    private ScreenBuffer? _previous_frame;

    /// <summary>
    ///     创建差分渲染引擎
    /// </summary>
    public DifferentialRenderer()
    {
        _has_console = has_console();
    }

    private static bool has_console()
    {
        try
        {
            var _ = System.Console.CursorTop;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    ///     渲染到终端，仅输出与上一帧相比有变化的区域
    /// </summary>
    /// <param name="currentFrame">当前帧缓冲区</param>
    public void render(ScreenBuffer currentFrame)
    {
        if (!_has_console)
        {
            _previous_frame = currentFrame;
            return;
        }

        if (_previous_frame == null
            || _previous_frame.width != currentFrame.width
            || _previous_frame.height != currentFrame.height)
        {
            full_render(currentFrame);
            _previous_frame = currentFrame;
            return;
        }

        diff_render(_previous_frame, currentFrame);
        _previous_frame = currentFrame;
    }

    private static void full_render(ScreenBuffer frame)
    {
        System.Console.SetCursorPosition(0, 0);

        for (var y = 0; y < frame.height; y++)
        {
            var currentFg = frame.get_foreground(0, y);
            var currentBg = frame.get_background(0, y);

            for (var x = 0; x < frame.width; x++)
            {
                var fg = frame.get_foreground(x, y);
                var bg = frame.get_background(x, y);

                if (!fg.Equals(currentFg) || !bg.Equals(currentBg))
                {
                    write_ansi_color(fg, bg);
                    currentFg = fg;
                    currentBg = bg;
                }

                System.Console.Write(frame.get_char(x, y));
            }

            if (y < frame.height - 1) System.Console.WriteLine();
        }

        System.Console.ResetColor();
    }

    private static void diff_render(ScreenBuffer previous, ScreenBuffer current)
    {
        for (var y = 0; y < current.height; y++)
        {
            var currentFg = current.get_foreground(0, y);
            var currentBg = current.get_background(0, y);
            var hasChanges = false;

            for (var x = 0; x < current.width; x++)
            {
                if (previous.cell_equals(current, x, y)) continue;

                if (!hasChanges) hasChanges = true;

                System.Console.SetCursorPosition(x, y);

                var fg = current.get_foreground(x, y);
                var bg = current.get_background(x, y);

                if (!fg.Equals(currentFg) || !bg.Equals(currentBg))
                {
                    write_ansi_color(fg, bg);
                    currentFg = fg;
                    currentBg = bg;
                }

                System.Console.Write(current.get_char(x, y));
            }

            if (hasChanges) System.Console.ResetColor();
        }

        System.Console.SetCursorPosition(0, current.height);
    }

    private static void write_ansi_color(RgbColor fg, RgbColor bg)
    {
        System.Console.Write($"\u001b[38;2;{fg.r};{fg.g};{fg.b}m");
        System.Console.Write($"\u001b[48;2;{bg.r};{bg.g};{bg.b}m");
    }
}