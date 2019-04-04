namespace Std.Terminal.Controls;

/// <summary>
///     实时日志面板，支持自动滚动、级别过滤和颜色高亮
/// </summary>
public sealed class LogPanel : View
{
    private readonly List<LogEntry> _entries = [];
    private readonly object _lock = new();
    private int _scroll_offset;

    /// <summary>
    ///     创建日志面板
    /// </summary>
    public LogPanel()
    {
        width = 60;
        height = 15;
        tab_stop = true;
    }

    /// <summary>
    ///     最大保留条目数（超出时移除最早的条目）
    /// </summary>
    public int max_entries { get; set; } = 1000;

    /// <summary>
    ///     是否自动滚动到最新日志
    /// </summary>
    public bool auto_scroll { get; set; } = true;

    /// <summary>
    ///     最低显示级别（低于此级别的日志不显示）
    /// </summary>
    public LogLevel min_level { get; set; } = LogLevel.debug;

    /// <summary>
    ///     面板标题
    /// </summary>
    public string title { get; set; } = "日志";

    /// <summary>
    ///     获取所有日志条目
    /// </summary>
    public IReadOnlyList<LogEntry> entries
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }

    /// <summary>
    ///     获取筛选后的可见条目数
    /// </summary>
    public int visible_entry_count
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count(e => e.level >= min_level);
            }
        }
    }

    /// <summary>
    ///     添加日志条目
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">消息内容</param>
    /// <param name="source">来源标识</param>
    public void log(LogLevel level, string message, string? source = null)
    {
        lock (_lock)
        {
            _entries.Add(new LogEntry { level = level, message = message, source = source });

            if (_entries.Count > max_entries) _entries.RemoveAt(0);

            if (auto_scroll)
            {
                var visibleCount = _entries.Count(e => e.level >= min_level);
                var visibleHeight = System.Math.Max(1, height - 2);
                _scroll_offset = System.Math.Max(0, visibleCount - visibleHeight);
            }
        }
    }

    /// <summary>
    ///     添加调试日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="source">来源</param>
    public void debug(string message, string? source = null)
    {
        log(LogLevel.debug, message, source);
    }

    /// <summary>
    ///     添加信息日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="source">来源</param>
    public void info(string message, string? source = null)
    {
        log(LogLevel.info, message, source);
    }

    /// <summary>
    ///     添加警告日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="source">来源</param>
    public void warning(string message, string? source = null)
    {
        log(LogLevel.warning, message, source);
    }

    /// <summary>
    ///     添加错误日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="source">来源</param>
    public void error(string message, string? source = null)
    {
        log(LogLevel.error, message, source);
    }

    /// <summary>
    ///     添加成功日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="source">来源</param>
    public void success(string message, string? source = null)
    {
        log(LogLevel.success, message, source);
    }

    /// <summary>
    ///     清除所有日志
    /// </summary>
    public void clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            _scroll_offset = 0;
        }
    }

    /// <summary>
    ///     设置最低显示级别
    /// </summary>
    /// <param name="level">最低级别</param>
    public void set_filter(LogLevel level)
    {
        min_level = level;
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        if (!string.IsNullOrEmpty(title))
        {
            var titleText = $" {title} ";
            var titleX = System.Math.Max(1, (width - titleText.Length) / 2);
            ctx.draw_text(titleX, 0, titleText, RgbColor.Cyan, ctx.default_background);
        }

        List<LogEntry> visibleEntries;
        lock (_lock)
        {
            visibleEntries = [.. _entries.Where(e => e.level >= min_level)];
        }

        var contentHeight = System.Math.Max(1, height - 2);
        var contentWidth = System.Math.Max(1, width - 2);

        var startIndex = _scroll_offset;
        var endIndex = System.Math.Min(visibleEntries.Count, startIndex + contentHeight);

        for (var i = startIndex; i < endIndex; i++)
        {
            var entry = visibleEntries[i];
            var row = i - startIndex + 1;
            var (fg, prefix) = get_level_style(entry.level);

            var timeStr = entry.timestamp.ToString("HH:mm:ss");
            var sourceStr = entry.source != null ? $"[{entry.source}] " : "";
            var lineText = $"{timeStr} {prefix} {sourceStr}{entry.message}";

            if (lineText.Length > contentWidth) lineText = lineText[..(contentWidth - 1)] + "…";

            ctx.draw_text(1, row, lineText, fg, ctx.default_background);
        }

        if (visibleEntries.Count > contentHeight)
        {
            var scrollThumbSize = System.Math.Max(1, contentHeight * contentHeight / visibleEntries.Count);
            var scrollThumbPos = _scroll_offset * (contentHeight - scrollThumbSize) /
                                 System.Math.Max(1, visibleEntries.Count - contentHeight);
            for (var i = 0; i < contentHeight; i++)
            {
                var ch = i >= scrollThumbPos && i < scrollThumbPos + scrollThumbSize ? "█" : "░";
                ctx.draw_text(width - 1, i + 1, ch, RgbColor.DarkGray, ctx.default_background);
            }
        }

        var statusY = height - 1;
        var statusText = $" {_entries.Count} 条";
        if (min_level > LogLevel.debug) statusText += $" | 过滤: {min_level}";

        if (!auto_scroll) statusText += " | 手动滚动";

        ctx.draw_text(1, statusY, statusText, RgbColor.DarkGray, ctx.default_background);
    }

    /// <inheritdoc />
    public override bool OnKeyDown(ConsoleKeyInfo key)
    {
        List<LogEntry> visibleEntries;
        lock (_lock)
        {
            visibleEntries = [.. _entries.Where(e => e.level >= min_level)];
        }

        var contentHeight = System.Math.Max(1, height - 2);

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                auto_scroll = false;
                _scroll_offset = System.Math.Max(0, _scroll_offset - 1);
                return true;
            case ConsoleKey.DownArrow:
                auto_scroll = false;
                _scroll_offset = System.Math.Min(System.Math.Max(0, visibleEntries.Count - contentHeight),
                    _scroll_offset + 1);
                return true;
            case ConsoleKey.PageUp:
                auto_scroll = false;
                _scroll_offset = System.Math.Max(0, _scroll_offset - contentHeight);
                return true;
            case ConsoleKey.PageDown:
                auto_scroll = false;
                _scroll_offset = System.Math.Min(System.Math.Max(0, visibleEntries.Count - contentHeight),
                    _scroll_offset + contentHeight);
                return true;
            case ConsoleKey.Home:
                auto_scroll = false;
                _scroll_offset = 0;
                return true;
            case ConsoleKey.End:
                auto_scroll = true;
                return true;
            case ConsoleKey.F:
                if (min_level < LogLevel.error)
                    min_level++;
                else
                    min_level = LogLevel.debug;

                return true;
            case ConsoleKey.C:
                clear();
                return true;
            default:
                return false;
        }
    }

    private static (RgbColor fg, string prefix) get_level_style(LogLevel level)
    {
        return level switch
        {
            LogLevel.debug => (RgbColor.DarkGray, "DBG"),
            LogLevel.info => (RgbColor.White, "INF"),
            LogLevel.warning => (RgbColor.Yellow, "WRN"),
            LogLevel.error => (RgbColor.Red, "ERR"),
            LogLevel.success => (RgbColor.Green, "OK "),
            _ => (RgbColor.White, "???")
        };
    }
}