using Std.Terminal.Controls;

namespace Std.Terminal.Layout;

/// <summary>
///     水平线性布局容器，支持 Margin/Padding/Spacing/Alignment
/// </summary>
public sealed class HBox : View
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
        var currentX = _padding_left;
        var innerHeight = height - _padding_top - _padding_bottom;

        foreach (var child in _children)
        {
            if (!child.visible) continue;

            currentX += child.margin.left;

            var marginHeight = child.margin.top + child.margin.bottom;
            var childWidth = child.width > 0 ? child.width : 1;
            var childHeight = System.Math.Min(child.height, innerHeight - marginHeight);
            var childY = _padding_top + child.margin.top;

            switch (child.vertical_alignment)
            {
                case VerticalAlignment.center:
                    childY = _padding_top + child.margin.top + (innerHeight - marginHeight - childHeight) / 2;
                    break;
                case VerticalAlignment.bottom:
                    childY = _padding_top + innerHeight - child.margin.bottom - childHeight;
                    break;
                case VerticalAlignment.fill:
                    childHeight = innerHeight - marginHeight;
                    break;
            }

            var totalChildWidth = childWidth + child.margin.left + child.margin.right;

            if (currentX + totalChildWidth > width - _padding_right) break;

            child.x = currentX;
            child.y = childY;
            child.width = childWidth;
            child.height = childHeight;

            var childCtx = ctx.create_child(currentX, childY, childWidth, childHeight);
            child.render(childCtx);

            currentX += childWidth + child.margin.right + _spacing;
        }
    }

    #region 构造函数

    /// <summary>
    ///     创建空的水平布局
    /// </summary>
    public HBox()
    {
    }

    /// <summary>
    ///     使用子控件集合创建水平布局
    /// </summary>
    /// <param name="children">子控件集合</param>
    public HBox(IEnumerable<View> children)
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
    public HBox with_padding(int top = 0, int bottom = 0, int left = 0, int right = 0)
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
    public HBox with_spacing(int spacing)
    {
        _spacing = spacing;
        return this;
    }

    /// <summary>
    ///     设置固定宽度
    /// </summary>
    public HBox with_width(int width)
    {
        this.width = width;
        return this;
    }

    #endregion
}