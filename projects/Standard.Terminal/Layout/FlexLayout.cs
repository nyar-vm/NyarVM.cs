using Std.Terminal.Controls;

namespace Std.Terminal.Layout;

/// <summary>
///     Flex 弹性布局容器，支持 Row/Column 方向、Wrap、主轴/交叉轴对齐
/// </summary>
public sealed class FlexLayout : View
{
    private readonly List<FlexItem> _items = [];

    /// <summary>
    ///     排列方向
    /// </summary>
    public FlexDirection direction { get; set; } = FlexDirection.row;

    /// <summary>
    ///     换行模式
    /// </summary>
    public FlexWrap wrap { get; set; } = FlexWrap.no_wrap;

    /// <summary>
    ///     主轴对齐
    /// </summary>
    public FlexJustify justify_content { get; set; } = FlexJustify.start;

    /// <summary>
    ///     交叉轴对齐
    /// </summary>
    public FlexAlign align_items { get; set; } = FlexAlign.stretch;

    /// <summary>
    ///     间距（字符数 / 行数）
    /// </summary>
    public int gap { get; set; }

    /// <summary>
    ///     内边距
    /// </summary>
    public Thickness padding { get; set; } = Thickness.zero;

    /// <summary>
    ///     添加子控件
    /// </summary>
    /// <param name="view">子控件</param>
    /// <param name="grow">弹性系数</param>
    /// <param name="basis">固定主轴大小</param>
    public FlexLayout add(View view, double grow = 0, int? basis = null)
    {
        _items.Add(new FlexItem(view, grow, basis));
        return this;
    }

    /// <summary>
    ///     添加弹性填充空间
    /// </summary>
    /// <param name="grow">弹性系数</param>
    public static FlexItem flex(View view, double grow = 1)
    {
        return new FlexItem(view, grow);
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var contentX = x + padding.left;
        var contentY = y + padding.top;
        var contentWidth = width - padding.left - padding.right;
        var contentHeight = height - padding.top - padding.bottom;

        if (direction == FlexDirection.row)
            render_row_layout(ctx, contentX, contentY, contentWidth, contentHeight);
        else
            render_column_layout(ctx, contentX, contentY, contentWidth, contentHeight);
    }

    private void render_row_layout(RenderContext ctx, int startX, int startY, int contentWidth, int contentHeight)
    {
        var fixedItems = _items.Where(i => i.grow <= 0).ToList();
        var flexItems = _items.Where(i => i.grow > 0).ToList();

        var fixedWidth = fixedItems.Sum(i => (i.basis ?? i.view.width) + gap);
        fixedWidth -= _items.Count > 0 && fixedItems.Count == 0 ? 0 : gap;
        var remainingWidth = System.Math.Max(0, contentWidth - fixedWidth);

        var totalGrow = flexItems.Sum(i => i.grow);
        var currentX = startX;

        var spacing = calculate_spacing(contentWidth, totalGrow == 0);
        if (justify_content is FlexJustify.space_between or FlexJustify.space_around) currentX += spacing;

        foreach (var item in _items)
        {
            if (currentX >= startX + contentWidth && wrap == FlexWrap.no_wrap) break;

            var itemWidth = item.basis ?? item.view.width;
            if (item.grow > 0 && totalGrow > 0) itemWidth = (int)(remainingWidth * item.grow / totalGrow);

            item.view.x = currentX;
            item.view.y = startY + get_cross_axis_offset(item, contentHeight);
            item.view.width = itemWidth;
            item.view.render(ctx);

            currentX += itemWidth + gap + spacing;
        }
    }

    private void render_column_layout(RenderContext ctx, int startX, int startY, int contentWidth, int contentHeight)
    {
        var fixedItems = _items.Where(i => i.grow <= 0).ToList();
        var flexItems = _items.Where(i => i.grow > 0).ToList();

        var fixedHeight = fixedItems.Sum(i => (i.basis ?? i.view.height) + gap);
        fixedHeight -= _items.Count > 0 && flexItems.Count == 0 ? 0 : gap;
        var remainingHeight = System.Math.Max(0, contentHeight - fixedHeight);

        var totalGrow = flexItems.Sum(i => i.grow);
        var currentY = startY;

        foreach (var item in _items)
        {
            if (currentY >= startY + contentHeight) break;

            var itemHeight = item.basis ?? item.view.height;
            if (item.grow > 0 && totalGrow > 0) itemHeight = (int)(remainingHeight * item.grow / totalGrow);

            item.view.x = startX + get_cross_axis_offset(item, contentWidth);
            item.view.y = currentY;
            item.view.height = itemHeight;
            item.view.render(ctx);

            currentY += itemHeight + gap;
        }
    }

    private int calculate_spacing(int contentWidth, bool allFixed)
    {
        if (!allFixed) return 0;

        var totalWidth = _items.Sum(i => i.view.width);
        var remaining = contentWidth - totalWidth;

        if (justify_content == FlexJustify.space_between && _items.Count > 1) return remaining / (_items.Count - 1);

        if (justify_content == FlexJustify.space_around) return _items.Count > 0 ? remaining / _items.Count / 2 : 0;

        if (justify_content == FlexJustify.center) return remaining / 2;

        if (justify_content == FlexJustify.end) return remaining;

        return 0;
    }

    private int get_cross_axis_offset(FlexItem item, int containerSize)
    {
        var itemSize = direction == FlexDirection.row ? item.view.height : item.view.width;

        return align_items switch
        {
            FlexAlign.center => (containerSize - itemSize) / 2,
            FlexAlign.end => containerSize - itemSize,
            FlexAlign.stretch => 0,
            _ => 0
        };
    }
}