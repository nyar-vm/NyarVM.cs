namespace Std.Terminal.Controls;

/// <summary>
///     下拉选择控件，支持展开列表和键盘导航
/// </summary>
public sealed class Dropdown : View
{
    private readonly List<DropdownItem> _items = [];
    private int _selected_index = -1;

    /// <summary>
    ///     创建下拉控件
    /// </summary>
    public Dropdown()
    {
        width = 20;
        height = 3;
        tab_stop = true;
    }

    /// <summary>
    ///     创建下拉控件
    /// </summary>
    /// <param name="items">选项列表</param>
    public Dropdown(IEnumerable<DropdownItem> items) : this()
    {
        _items.AddRange(items);
    }

    /// <summary>
    ///     选项列表
    /// </summary>
    public IReadOnlyList<DropdownItem> items => _items;

    /// <summary>
    ///     当前选中项
    /// </summary>
    public DropdownItem? selected_item
    {
        get => _selected_index >= 0 && _selected_index < _items.Count ? _items[_selected_index] : null;
        set
        {
            var idx = value != null ? _items.FindIndex(i => i.id == value.id) : -1;
            if (idx >= 0)
            {
                _selected_index = idx;
                is_open = false;
                OnSelectionChanged?.Invoke(this, _items[idx]);
            }
        }
    }

    /// <summary>
    ///     是否展开
    /// </summary>
    public bool is_open { get; set; }

    /// <summary>
    ///     占位符文本
    /// </summary>
    public string placeholder { get; set; } = "Select...";

    /// <summary>
    ///     最大展开高度
    /// </summary>
    public int max_drop_height { get; set; } = 8;

    /// <summary>
    ///     选中变化事件
    /// </summary>
    public event Action<Dropdown, DropdownItem>? OnSelectionChanged;

    /// <summary>
    ///     添加选项
    /// </summary>
    /// <param name="text">选项文本</param>
    /// <param name="value">选项值</param>
    public void add_item(string text, object? value = null)
    {
        _items.Add(new DropdownItem { text = text, value = value });
        if (_selected_index < 0) _selected_index = 0;
    }

    /// <summary>
    ///     添加分隔线
    /// </summary>
    public void add_separator()
    {
        _items.Add(new DropdownItem { text = "────────", is_separator = true, is_enabled = false });
    }

    /// <summary>
    ///     清空选项
    /// </summary>
    public void clear()
    {
        _items.Clear();
        _selected_index = -1;
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        var displayText = _selected_index >= 0 && _selected_index < _items.Count
            ? _items[_selected_index].text
            : placeholder;

        var textColor = _selected_index >= 0 ? RgbColor.White : new RgbColor(150, 150, 150);

        if (displayText.Length > width - 4) displayText = displayText[..(width - 4)];

        ctx.fill_rect(1, 1, width - 2, 1, ctx.default_background);
        ctx.draw_text(2, 1, displayText, textColor, ctx.default_background);
        ctx.draw_text(width - 3, 1, is_open ? "▲" : "▼", new RgbColor(150, 150, 150), ctx.default_background);

        if (is_open) render_dropdown(ctx);
    }

    private void render_dropdown(RenderContext ctx)
    {
        var validItems = _items.Where(i => i.is_enabled).ToList();
        var dropHeight = System.Math.Min(validItems.Count, max_drop_height);

        for (var i = 0; i < dropHeight; i++)
        {
            var itemIndex = _items.IndexOf(validItems[i]);
            var isHighlighted = itemIndex == _selected_index && _items[itemIndex].is_enabled;

            var y = height + i;

            var bg = isHighlighted ? new RgbColor(0, 100, 200) : new RgbColor(30, 30, 40);
            var fg = _items[itemIndex].is_enabled
                ? isHighlighted ? RgbColor.White : new RgbColor(200, 200, 200)
                : RgbColor.Gray;

            ctx.fill_rect(1, y, width - 2, 1, bg);

            if (_items[itemIndex].is_separator)
            {
                ctx.draw_text(2, y, _items[itemIndex].text, RgbColor.Gray, bg);
            }
            else
            {
                var itemText = _items[itemIndex].text;
                if (itemText.Length > width - 4) itemText = itemText[..(width - 4)];

                ctx.draw_text(2, y, itemText, fg, bg);
            }
        }
    }

    /// <summary>
    ///     获取总高度（包含展开列表）
    /// </summary>
    public int get_expanded_height()
    {
        if (is_open)
        {
            var validItems = _items.Where(i => i.is_enabled).ToList();
            var dropHeight = System.Math.Min(validItems.Count, max_drop_height);
            return height + dropHeight;
        }

        return height;
    }

    internal void handle_toggle()
    {
        is_open = !is_open;
    }

    internal void handle_close()
    {
        is_open = false;
    }

    internal void move_selection_up()
    {
        if (!is_open || _items.Count == 0) return;

        do
        {
            _selected_index = _selected_index <= 0 ? _items.Count - 1 : _selected_index - 1;
        } while (_selected_index >= 0 && _selected_index < _items.Count && !_items[_selected_index].is_enabled);
    }

    internal void move_selection_down()
    {
        if (!is_open || _items.Count == 0) return;

        do
        {
            _selected_index = _selected_index >= _items.Count - 1 ? 0 : _selected_index + 1;
        } while (_selected_index >= 0 && _selected_index < _items.Count && !_items[_selected_index].is_enabled);
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
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                if (is_open)
                    handle_close();
                else
                    handle_toggle();

                return true;
            case ConsoleKey.UpArrow:
                if (is_open) move_selection_up();

                return true;
            case ConsoleKey.DownArrow:
                if (is_open) move_selection_down();

                return true;
            case ConsoleKey.Escape:
                if (is_open)
                {
                    handle_close();
                    return true;
                }

                return false;
            default:
                return false;
        }
    }
}