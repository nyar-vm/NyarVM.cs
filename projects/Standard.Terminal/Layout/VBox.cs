using Std.Terminal.Controls;

namespace Std.Terminal.Layout;

/// <summary>
///     垂直线性布局容器，支持 Margin/Padding/Spacing/Alignment
/// </summary>
public sealed class VBox : View
{
    private readonly List<View> _children = [];
    private int _padding_bottom;
    private int _padding_left;
    private int _padding_right;

    private int _padding_top;
    private int _spacing;

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var currentY = _padding_top;
        var innerWidth = width - _padding_left - _padding_right;

        foreach (var child in _children)
        {
            if (!child.visible) continue;

            currentY += child.margin.top;

            var marginWidth = child.margin.left + child.margin.right;
            var childWidth = child.width > 0
                ? System.Math.Min(child.width, innerWidth - marginWidth)
                : innerWidth - marginWidth;
            var childHeight = child.height;
            var childX = _padding_left + child.margin.left;

            switch (child.horizontal_alignment)
            {
                case HorizontalAlignment.center:
                    childX = _padding_left + child.margin.left + (innerWidth - marginWidth - childWidth) / 2;
                    break;
                case HorizontalAlignment.right:
                    childX = _padding_left + innerWidth - child.margin.right - childWidth;
                    break;
                case HorizontalAlignment.fill:
                    childWidth = innerWidth - marginWidth;
                    break;
            }

            var totalChildHeight = childHeight + child.margin.top + child.margin.bottom;

            if (currentY + totalChildHeight > height - _padding_bottom) break;

            child.x = childX;
            child.y = currentY;
            child.width = childWidth;

            var childCtx = ctx.create_child(childX, currentY, childWidth, childHeight);
            child.render(childCtx);

            currentY += childHeight + child.margin.bottom + _spacing;
        }
    }

    #region 构造函数

    /// <summary>
    ///     创建空的垂直布局
    /// </summary>
    public VBox()
    {
    }

    /// <summary>
    ///     使用子控件集合创建垂直布局
    /// </summary>
    /// <param name="children">子控件集合</param>
    public VBox(IEnumerable<View> children)
    {
        _children.AddRange(children);
    }

    #endregion

    #region 集合操作

    /// <summary>
    ///     添加子控件
    /// </summary>
    public void add(View child)
    {
        _children.Add(child);
    }

    /// <summary>
    ///     获取子控件列表
    /// </summary>
    public IReadOnlyList<View> children => _children;

    #endregion

    #region 布局配置

    /// <summary>
    ///     设置内边距
    /// </summary>
    public VBox with_padding(int top = 0, int bottom = 0, int left = 0, int right = 0)
    {
        _padding_top = top;
        _padding_bottom = bottom;
        _padding_left = left;
        _padding_right = right;
        return this;
    }

    /// <summary>
    ///     设置子控件间距
    /// </summary>
    public VBox with_spacing(int spacing)
    {
        _spacing = spacing;
        return this;
    }

    #endregion
}