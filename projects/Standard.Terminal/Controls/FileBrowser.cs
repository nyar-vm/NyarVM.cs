namespace Std.Terminal.Controls;

/// <summary>
///     文件浏览器控件，支持目录浏览、文件选择、路径导航
/// </summary>
public sealed class FileBrowser : View
{
    private readonly List<FlatEntry> _flat_entries = [];
    private string? _filter;
    private int _scroll_offset;
    private int _selected_index;
    private bool _show_hidden;
    private string? _status_message;

    /// <summary>创建文件浏览器</summary>
    /// <param name="initialPath">初始路径（默认当前目录）</param>
    public FileBrowser(string? initialPath = null)
    {
        current_path = initialPath ?? Directory.GetCurrentDirectory();
        width = 50;
        height = 20;
        tab_stop = true;
        refresh();
    }

    /// <summary>当前浏览路径</summary>
    public string current_path { get; private set; }

    /// <summary>选中的文件条目</summary>
    public FileEntry? selected_entry { get; private set; }

    /// <summary>是否显示隐藏文件</summary>
    public bool show_hidden
    {
        get => _show_hidden;
        set
        {
            _show_hidden = value;
            refresh();
        }
    }

    /// <summary>文件过滤器（通配符，如 *.cs）</summary>
    public string? filter
    {
        get => _filter;
        set
        {
            _filter = value;
            refresh();
        }
    }

    /// <summary>是否允许选择目录</summary>
    public bool allow_directory_selection { get; set; }

    /// <summary>选中事件</summary>
    public event Action<FileBrowser, FileEntry>? OnFileSelected;

    /// <summary>路径变更事件</summary>
    public event Action<FileBrowser, string>? OnPathChanged;

    /// <summary>
    ///     刷新当前目录内容
    /// </summary>
    public void refresh()
    {
        _flat_entries.Clear();
        _status_message = null;

        try
        {
            if (!Directory.Exists(current_path))
            {
                _status_message = "目录不存在";
                return;
            }

            var parentDir = Directory.GetParent(current_path)?.FullName;
            if (parentDir is not null)
                _flat_entries.Add(new FlatEntry(new FileEntry(parentDir, "..", true), 0, EntryType.parent));

            var entries = new List<(FileSystemInfo info, bool isDir)>();

            try
            {
                foreach (var dir in Directory.EnumerateDirectories(current_path))
                {
                    var info = new DirectoryInfo(dir);
                    if (!_show_hidden && info.Attributes.HasFlag(FileAttributes.Hidden)) continue;

                    entries.Add((info, true));
                }
            }
            catch (UnauthorizedAccessException)
            {
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(current_path))
                {
                    var info = new FileInfo(file);
                    if (!_show_hidden && info.Attributes.HasFlag(FileAttributes.Hidden)) continue;

                    if (!matches_filter(info.Name)) continue;

                    entries.Add((info, false));
                }
            }
            catch (UnauthorizedAccessException)
            {
            }

            entries.Sort((a, b) =>
            {
                if (a.isDir != b.isDir) return a.isDir ? -1 : 1;

                return string.Compare(a.info.Name, b.info.Name, StringComparison.OrdinalIgnoreCase);
            });

            foreach (var (info, isDir) in entries)
            {
                var entry = isDir
                    ? new FileEntry(info.FullName, info.Name, true, 0, info.LastWriteTime)
                    : new FileEntry(info.FullName, info.Name, false, ((FileInfo)info).Length, info.LastWriteTime);

                var entryType = isDir ? EntryType.directory : EntryType.file;
                _flat_entries.Add(new FlatEntry(entry, 0, entryType));
            }

            if (_flat_entries.Count == 0 || _flat_entries[0].type == EntryType.parent)
                _status_message = _flat_entries.Count <= 1 ? "（空目录）" : null;
        }
        catch (UnauthorizedAccessException)
        {
            _status_message = "无访问权限";
        }
        catch (IOException ex)
        {
            _status_message = $"IO 错误: {ex.Message}";
        }

        _selected_index = System.Math.Clamp(_selected_index, 0, System.Math.Max(0, _flat_entries.Count - 1));
        update_selected_entry();
    }

    /// <summary>
    ///     导航到指定路径
    /// </summary>
    /// <param name="path">目标路径</param>
    public void navigate_to(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath)) return;

            current_path = fullPath;
            _selected_index = 0;
            _scroll_offset = 0;
            refresh();
            OnPathChanged?.Invoke(this, current_path);
        }
        catch
        {
        }
    }

    /// <summary>
    ///     导航到上级目录
    /// </summary>
    public void navigate_up()
    {
        var parent = Directory.GetParent(current_path)?.FullName;
        if (parent is not null) navigate_to(parent);
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        var innerWidth = width - 2;
        var innerHeight = height - 2;

        render_header(ctx, innerWidth);
        render_entries(ctx, innerWidth, innerHeight - 2);
        render_footer(ctx, innerWidth, innerHeight);
    }

    private void render_header(RenderContext ctx, int innerWidth)
    {
        var pathBg = new RgbColor(30, 30, 50);
        ctx.fill_rect(1, 1, innerWidth, 1, pathBg);

        var displayPath = current_path;
        if (displayPath.Length > innerWidth - 3)
            displayPath = "..." + displayPath[(displayPath.Length - innerWidth + 6)..];

        ctx.draw_text(1, 1, $"📂 {displayPath}", new RgbColor(180, 220, 255), pathBg);
    }

    private void render_entries(RenderContext ctx, int innerWidth, int visibleHeight)
    {
        _scroll_offset = System.Math.Clamp(_scroll_offset, 0, System.Math.Max(0, _flat_entries.Count - visibleHeight));

        for (var i = 0; i < visibleHeight && _scroll_offset + i < _flat_entries.Count; i++)
        {
            var flatIndex = _scroll_offset + i;
            var entry = _flat_entries[flatIndex];
            var isSelected = flatIndex == _selected_index;
            var y = 2 + i;

            var bg = isSelected ? new RgbColor(50, 50, 100) : ctx.default_background;
            var fg = isSelected ? RgbColor.White : new RgbColor(200, 200, 200);

            ctx.fill_rect(1, y, innerWidth, 1, bg);

            var icon = entry.type switch
            {
                EntryType.parent => "📁",
                EntryType.directory => "📁",
                EntryType.file => get_file_icon(entry.entry.name),
                _ => "📄"
            };

            var nameText = entry.type == EntryType.parent ? ".. (上级目录)" : entry.entry.name;
            var displayText = $"{icon} {nameText}";

            if (entry is { type: EntryType.file, entry.size: > 0 })
            {
                var sizeText = format_size(entry.entry.size);
                var sizeLen = sizeText.Length + 1;
                var nameMaxLen = innerWidth - 4 - sizeLen;
                if (displayText.Length > nameMaxLen) displayText = displayText[..System.Math.Max(0, nameMaxLen)] + "…";

                ctx.draw_text(1, y, displayText, fg, bg);
                ctx.draw_text(innerWidth - sizeLen, y, sizeText, new RgbColor(140, 140, 140), bg);
            }
            else
            {
                if (displayText.Length > innerWidth - 4) displayText = displayText[..(innerWidth - 5)] + "…";

                ctx.draw_text(1, y, displayText, fg, bg);
            }
        }

        if (_flat_entries.Count == 0 && _status_message is not null)
            ctx.draw_text(1, 2, _status_message, new RgbColor(140, 140, 140), ctx.default_background);
    }

    private void render_footer(RenderContext ctx, int innerWidth, int innerHeight)
    {
        var footerY = 1 + innerHeight;
        var footerBg = new RgbColor(30, 30, 50);
        ctx.fill_rect(1, footerY, innerWidth, 1, footerBg);

        var fileCount = _flat_entries.Count(e => e.type == EntryType.file);
        var dirCount = _flat_entries.Count(e => e.type == EntryType.directory);
        var footerText = $"{dirCount} 目录 | {fileCount} 文件";
        if (_status_message is not null && _flat_entries.Count == 0) footerText = _status_message;

        if (footerText.Length > innerWidth - 2) footerText = footerText[..(innerWidth - 3)] + "…";

        ctx.draw_text(1, footerY, footerText, new RgbColor(140, 160, 180), footerBg);
    }

    internal void move_selection_up()
    {
        if (_flat_entries.Count == 0) return;

        _selected_index = System.Math.Max(0, _selected_index - 1);
        ensure_visible();
        update_selected_entry();
    }

    internal void move_selection_down()
    {
        if (_flat_entries.Count == 0) return;

        _selected_index = System.Math.Min(_flat_entries.Count - 1, _selected_index + 1);
        ensure_visible();
        update_selected_entry();
    }

    internal void page_up()
    {
        if (_flat_entries.Count == 0) return;

        var visibleHeight = height - 4;
        _selected_index = System.Math.Max(0, _selected_index - visibleHeight);
        ensure_visible();
        update_selected_entry();
    }

    internal void page_down()
    {
        if (_flat_entries.Count == 0) return;

        var visibleHeight = height - 4;
        _selected_index = System.Math.Min(_flat_entries.Count - 1, _selected_index + visibleHeight);
        ensure_visible();
        update_selected_entry();
    }

    internal void activate_selected()
    {
        if (_flat_entries.Count == 0 || _selected_index >= _flat_entries.Count) return;

        var entry = _flat_entries[_selected_index].entry;

        if (entry.is_directory)
        {
            navigate_to(entry.full_path);
        }
        else
        {
            selected_entry = entry;
            OnFileSelected?.Invoke(this, entry);
        }
    }

    internal void go_to_parent()
    {
        navigate_up();
    }

    internal void toggle_hidden()
    {
        _show_hidden = !_show_hidden;
        refresh();
    }

    /// <summary>
    ///     处理键盘输入事件
    /// </summary>
    /// <param name="key">键盘输入信息</param>
    /// <returns>是否已处理该按键</returns>
    public override bool OnKeyDown(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                move_selection_up();
                return true;
            case ConsoleKey.DownArrow:
                move_selection_down();
                return true;
            case ConsoleKey.PageUp:
                page_up();
                return true;
            case ConsoleKey.PageDown:
                page_down();
                return true;
            case ConsoleKey.Enter:
                activate_selected();
                return true;
            case ConsoleKey.Backspace:
                go_to_parent();
                return true;
            case ConsoleKey.H:
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    toggle_hidden();
                    return true;
                }

                return false;
            case ConsoleKey.Escape:
                go_to_parent();
                return true;
            default:
                return false;
        }
    }

    private void ensure_visible()
    {
        var visibleHeight = height - 4;
        if (_selected_index < _scroll_offset)
            _scroll_offset = _selected_index;
        else if (_selected_index >= _scroll_offset + visibleHeight)
            _scroll_offset = _selected_index - visibleHeight + 1;
    }

    private void update_selected_entry()
    {
        if (_flat_entries.Count > 0 && _selected_index < _flat_entries.Count)
            selected_entry = _flat_entries[_selected_index].entry;
        else
            selected_entry = null;
    }

    private bool matches_filter(string fileName)
    {
        if (string.IsNullOrEmpty(_filter)) return true;

        var filters = _filter.Split(';', '|');
        foreach (var f in filters)
        {
            var pattern = f.Trim();
            if (pattern is "*.*" or "*") return true;

            if (pattern.StartsWith("*."))
            {
                var ext = pattern[1..];
                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string get_file_icon(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "💻",
            ".js" or ".ts" => "📜",
            ".json" => "📋",
            ".xml" or ".yaml" or ".yml" => "📋",
            ".md" or ".txt" => "📝",
            ".png" or ".jpg" or ".gif" or ".svg" => "🖼️",
            ".exe" or ".dll" => "⚙️",
            ".zip" or ".tar" or ".gz" => "📦",
            ".csproj" or ".sln" => "🏗️",
            _ => "📄"
        };
    }

    private static string format_size(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";

        if (bytes < 1024 * 1024) return $"{bytes / 1024}K";

        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024)}M";

        return $"{bytes / (1024L * 1024 * 1024)}G";
    }

    private enum EntryType
    {
        parent,
        directory,
        file
    }

    private sealed class FlatEntry
    {
        public FlatEntry(FileEntry entry, int depth, EntryType type)
        {
            this.entry = entry;
            this.depth = depth;
            this.type = type;
        }

        public FileEntry entry { get; }
        public int depth { get; }
        public EntryType type { get; }
    }
}