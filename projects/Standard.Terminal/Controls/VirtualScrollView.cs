namespace Std.Terminal.Controls;

/// <summary>
///     虚拟滚动视图，仅渲染可见区域内的元素以优化超大列表性能
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public sealed class VirtualScrollView<T> : View
{
    private int _item_height = 1;
    private IReadOnlyList<T> _items = [];
    private int _scroll_offset;

    /// <summary>
    ///     创建虚拟滚动视图
    /// </summary>
    public VirtualScrollView()
    {
        width = 30;
        height = 10;
        tab_stop = true;
    }

    /// <summary>
    ///     创建虚拟滚动视图
    /// </summary>
    /// <param name="items">数据源</param>
    /// <param name="itemHeight">每项行高</param>
    public VirtualScrollView(IEnumerable<T> items, int itemHeight = 1) : this()
    {
        _items = [.. items];
        _item_height = System.Math.Max(1, itemHeight);
    }

    /// <summary>
    ///     数据源
    /// </summary>
    public IReadOnlyList<T> items
    {
        get => _items;
        set
        {
            _items = value;
            _scroll_offset = 0;
        }
    }

    /// <summary>
    ///     每项行高
    /// </summary>
    public int item_height
    {
        get => _item_height;
        set => _item_height = System.Math.Max(1, value);
    }

    /// <summary>
    ///     当前选中索引
    /// </summary>
    public int selected_index { get; set; } = -1;

    /// <summary>
    ///     自定义项渲染委托
    /// </summary>
    public Action<RenderContext, T, int, bool>? item_renderer { get; set; }

    /// <summary>
    ///     选中变化事件
    /// </summary>
    public event Action<VirtualScrollView<T>, int, T>? OnSelected;

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        var innerWidth = width - 2;
        var innerHeight = height - 2;
        var visibleItemCount = innerHeight / item_height;

        if (visibleItemCount == 0 || _items.Count == 0) return;

        var maxScrollOffset = System.Math.Max(0, _items.Count - visibleItemCount);
        _scroll_offset = System.Math.Clamp(_scroll_offset, 0, maxScrollOffset);

        for (var vi = 0; vi < visibleItemCount; vi++)
        {
            var itemIndex = _scroll_offset + vi;
            if (itemIndex >= _items.Count) break;

            var item = _items[itemIndex];
            var isSelected = itemIndex == selected_index;
            var itemY = 1 + vi * item_height;

            var bg = isSelected ? new RgbColor(50, 50, 100) : ctx.default_background;

            if (item_renderer != null)
            {
                var itemCtx = ctx.create_child(1, itemY, innerWidth, item_height);
                itemCtx.default_background = bg;
                item_renderer(itemCtx, item, itemIndex, isSelected);
            }
            else
            {
                ctx.fill_rect(1, itemY, innerWidth, item_height, bg);
                var text = item?.ToString() ?? string.Empty;
                if (text.Length > innerWidth) text = text[..innerWidth];

                var fg = isSelected ? RgbColor.White : new RgbColor(200, 200, 200);
                for (var row = 0; row < item_height; row++)
                    ctx.draw_text(1, itemY + row, row == 0 ? text : string.Empty, fg, bg);
            }
        }

        render_scroll_bar(ctx, innerHeight, visibleItemCount);
    }

    private void render_scroll_bar(RenderContext ctx, int innerHeight, int visibleItemCount)
    {
        if (_items.Count <= visibleItemCount) return;

        var maxScroll = System.Math.Max(1, _items.Count - visibleItemCount);
        var scrollBarHeight = System.Math.Max(1, (int)((float)visibleItemCount / _items.Count * innerHeight));
        var scrollBarPos = (int)((float)_scroll_offset / maxScroll * (innerHeight - scrollBarHeight));

        for (var i = 0; i < innerHeight; i++)
        {
            var isScrollThumb = i >= scrollBarPos && i < scrollBarPos + scrollBarHeight;
            var ch = isScrollThumb ? '█' : '░';
            var fg = isScrollThumb ? RgbColor.White : RgbColor.Gray;
            ctx.draw_text(width - 1, 1 + i, ch.ToString(), fg, ctx.default_background);
        }
    }

    /// <summary>
    ///     滚动到指定索引
    /// </summary>
    /// <param name="index">目标索引</param>
    public void scroll_to_index(int index)
    {
        var visibleItemCount = (height - 2) / item_height;
        if (visibleItemCount <= 0) return;

        _scroll_offset = System.Math.Clamp(index - visibleItemCount / 2, 0,
            System.Math.Max(0, _items.Count - visibleItemCount));
    }

    internal void move_selection_up()
    {
        if (_items.Count == 0) return;

        selected_index = selected_index <= 0 ? 0 : selected_index - 1;
        ensure_selected_visible();
        notify_selection();
    }

    internal void move_selection_down()
    {
        if (_items.Count == 0) return;

        selected_index = selected_index >= _items.Count - 1 ? _items.Count - 1 : selected_index + 1;
        ensure_selected_visible();
        notify_selection();
    }

    internal void move_selection_page_up()
    {
        if (_items.Count == 0) return;

        var pageSize = (height - 2) / item_height;
        selected_index = System.Math.Max(0, selected_index - pageSize);
        ensure_selected_visible();
        notify_selection();
    }

    internal void move_selection_page_down()
    {
        if (_items.Count == 0) return;

        var pageSize = (height - 2) / item_height;
        selected_index = System.Math.Min(_items.Count - 1, selected_index + pageSize);
        ensure_selected_visible();
        notify_selection();
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
                move_selection_page_up();
                return true;
            case ConsoleKey.PageDown:
                move_selection_page_down();
                return true;
            case ConsoleKey.Home:
                selected_index = 0;
                ensure_selected_visible();
                notify_selection();
                return true;
            case ConsoleKey.End:
                selected_index = _items.Count - 1;
                ensure_selected_visible();
                notify_selection();
                return true;
            default:
                return false;
        }
    }

    private void ensure_selected_visible()
    {
        var visibleItemCount = (height - 2) / item_height;
        if (selected_index < _scroll_offset)
            _scroll_offset = selected_index;
        else if (selected_index >= _scroll_offset + visibleItemCount)
            _scroll_offset = selected_index - visibleItemCount + 1;
    }

    private void notify_selection()
    {
        if (selected_index >= 0 && selected_index < _items.Count)
            OnSelected?.Invoke(this, selected_index, _items[selected_index]);
    }
}