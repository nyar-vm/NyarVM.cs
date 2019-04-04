namespace Std.Terminal.Controls;

/// <summary>
///     列表视图，支持滚动和自定义项渲染
/// </summary>
/// <typeparam name="T">列表项类型</typeparam>
public sealed class ListView<T> : View
{
    private int _scroll_offset;

    /// <summary>
    ///     使用指定数据集合创建列表视图
    /// </summary>
    /// <param name="items">数据集合</param>
    public ListView(IEnumerable<T> items)
    {
        this.items = [.. items];
        width = 15;
        height = 10;
        tab_stop = true;
    }

    /// <summary>
    ///     数据项集合
    /// </summary>
    public IReadOnlyList<T> items { get; set; } = [];

    /// <summary>
    ///     当前选中项
    /// </summary>
    public T? selected_item { get; set; }

    /// <summary>
    ///     选中项索引
    /// </summary>
    public int selected_index { get; internal set; } = -1;

    /// <summary>
    ///     自定义项渲染委托
    /// </summary>
    public Action<RenderContext, T, bool>? item_renderer { get; set; }

    /// <summary>
    ///     选中项变化事件
    /// </summary>
    public event Action<ListView<T>, T>? OnSelected;

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        var innerWidth = width - 2;
        var innerHeight = height - 2;
        var visibleRows = innerHeight / 2;

        _scroll_offset = System.Math.Clamp(_scroll_offset, 0, System.Math.Max(0, items.Count - visibleRows));

        for (var i = 0; i < visibleRows && _scroll_offset + i < items.Count; i++)
        {
            var itemIndex = _scroll_offset + i;
            var item = items[itemIndex];
            var isSelected = itemIndex == selected_index;
            var y = 1 + i * 2;

            var bg = isSelected ? new RgbColor(50, 50, 100) : ctx.default_background;
            var fg = isSelected ? RgbColor.White : new RgbColor(200, 200, 200);

            if (item_renderer != null)
            {
                var itemCtx = ctx.create_child(1, y, innerWidth, 2);
                item_renderer(itemCtx, item, isSelected);
            }
            else
            {
                var text = item?.ToString() ?? string.Empty;
                if (text.Length > innerWidth) text = text[..innerWidth];

                ctx.fill_rect(1, y, innerWidth, 1, bg);
                ctx.draw_text(1, y, text, fg, bg);
            }
        }

        if (items.Count > visibleRows)
        {
            var scrollBarHeight = System.Math.Max(1, (int)((float)visibleRows / items.Count * innerHeight));
            var scrollBarPos =
                (int)((float)_scroll_offset / (items.Count - visibleRows) * (innerHeight - scrollBarHeight));
            for (var i = 0; i < innerHeight; i++)
                if (i >= scrollBarPos && i < scrollBarPos + scrollBarHeight)
                    ctx.draw_text(width - 1, 1 + i, "█", RgbColor.White, ctx.default_background);
                else
                    ctx.draw_text(width - 1, 1 + i, "░", RgbColor.Gray, ctx.default_background);
        }
    }

    internal void move_selection_up()
    {
        if (items.Count == 0) return;

        if (selected_index <= 0)
            selected_index = 0;
        else
            selected_index--;

        var visibleRows = (height - 2) / 2;
        if (selected_index < _scroll_offset) _scroll_offset = selected_index;

        selected_item = items[selected_index];
        OnSelected?.Invoke(this, selected_item);
    }

    internal void move_selection_down()
    {
        if (items.Count == 0) return;

        if (selected_index >= items.Count - 1)
            selected_index = items.Count - 1;
        else
            selected_index++;

        var visibleRows = (height - 2) / 2;
        if (selected_index >= _scroll_offset + visibleRows) _scroll_offset = selected_index - visibleRows + 1;

        selected_item = items[selected_index];
        OnSelected?.Invoke(this, selected_item);
    }
}