namespace Std.Terminal.Controls;

/// <summary>
///     标签页容器控件，支持水平标签栏和标签页内容视图
/// </summary>
public sealed class TabView : View
{
    private readonly List<TabItem> _tabs = [];
    private int _header_scroll_offset;
    private int _selected_index = -1;

    /// <summary>
    ///     创建标签页容器
    /// </summary>
    public TabView()
    {
        width = 40;
        height = 15;
        tab_stop = true;
    }

    /// <summary>
    ///     标签页集合
    /// </summary>
    public IReadOnlyList<TabItem> tabs => _tabs;

    /// <summary>
    ///     当前激活标签
    /// </summary>
    public TabItem? active_tab => _selected_index >= 0 && _selected_index < _tabs.Count ? _tabs[_selected_index] : null;

    /// <summary>
    ///     标签头高度（行数）
    /// </summary>
    public int header_height { get; set; } = 3;

    /// <summary>
    ///     标签变化事件
    /// </summary>
    public event Action<TabView, TabItem?>? OnActiveTabChanged;

    /// <summary>
    ///     添加标签页
    /// </summary>
    /// <param name="title">标签标题</param>
    /// <param name="content">标签内容</param>
    /// <param name="closable">是否可关闭</param>
    public TabItem add_tab(string title, View? content = null, bool closable = true)
    {
        var tab = new TabItem { title = title, content = content, closable = closable };
        _tabs.Add(tab);

        if (_tabs.Count == 1) select_tab(0);

        return tab;
    }

    /// <summary>
    ///     移除标签页
    /// </summary>
    /// <param name="index">标签索引</param>
    public void remove_tab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;

        _tabs.RemoveAt(index);
        if (_selected_index >= _tabs.Count) select_tab(_tabs.Count - 1);
    }

    /// <summary>
    ///     选中标签页
    /// </summary>
    /// <param name="index">标签索引</param>
    public void select_tab(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            _selected_index = -1;
            OnActiveTabChanged?.Invoke(this, null);
            return;
        }

        if (_selected_index >= 0 && _selected_index < _tabs.Count) _tabs[_selected_index].is_active = false;

        _selected_index = index;
        _tabs[index].is_active = true;
        OnActiveTabChanged?.Invoke(this, _tabs[index]);
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        if (height < header_height + 2) return;

        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, header_height - 1, width, height - header_height + 1, BorderStyle.single, borderFg,
            ctx.default_background);

        render_headers(ctx);

        if (active_tab?.content != null)
        {
            var contentCtx = ctx.create_child(1, header_height, width - 2, height - header_height - 1);
            contentCtx.default_background = ctx.default_background;
            contentCtx.default_foreground = ctx.default_foreground;
            active_tab.content.render(contentCtx);
        }
    }

    private void render_headers(RenderContext ctx)
    {
        var headerBg = new RgbColor(30, 30, 40);
        ctx.fill_rect(0, 0, width, header_height - 1, headerBg);

        var x = 1;
        for (var i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            var titleText = tab.closable ? $" {tab.title} ×" : $" {tab.title} ";
            var tabWidth = titleText.Length + 2;

            var isActive = i == _selected_index;
            var tabBg = isActive ? ctx.default_background : headerBg;
            var tabFg = isActive ? RgbColor.White : RgbColor.Gray;
            var separatorFg = isActive ? new RgbColor(0, 150, 255) : RgbColor.Gray;

            if (isActive)
            {
                ctx.draw_text(x, 0, "┌", separatorFg, headerBg);
                for (var j = 1; j < tabWidth; j++) ctx.draw_text(x + j, 0, "─", separatorFg, tabBg);
                ctx.draw_text(x + tabWidth, 0, "┐", separatorFg, headerBg);
            }

            ctx.fill_rect(x, 1, tabWidth, header_height - 2, tabBg);
            ctx.draw_text(x + 1, 1, titleText, tabFg, tabBg);

            if (isActive)
            {
                ctx.draw_text(x, header_height - 1, "└", separatorFg, tabBg);
                ctx.draw_text(x + tabWidth, header_height - 1, "┘", separatorFg, tabBg);
            }

            x += tabWidth + 1;
            if (x > width - 5) break;
        }
    }

    internal void move_to_next_tab()
    {
        if (_tabs.Count == 0) return;

        select_tab((_selected_index + 1) % _tabs.Count);
    }

    internal void move_to_previous_tab()
    {
        if (_tabs.Count == 0) return;

        select_tab((_selected_index - 1 + _tabs.Count) % _tabs.Count);
    }

    internal void close_active_tab()
    {
        if (active_tab?.closable == true)
        {
            active_tab.request_close();
            if (_selected_index < _tabs.Count) remove_tab(_selected_index);
        }
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
            case ConsoleKey.LeftArrow:
                move_to_previous_tab();
                return true;
            case ConsoleKey.RightArrow:
                move_to_next_tab();
                return true;
            case ConsoleKey.W:
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    close_active_tab();
                    return true;
                }

                return false;
            default:
                return false;
        }
    }
}