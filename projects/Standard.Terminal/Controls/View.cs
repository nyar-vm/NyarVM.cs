namespace Std.Terminal.Controls;

/// <summary>
///     所有 UI 控件的抽象基类
/// </summary>
public abstract class View
{
    /// <summary>
    ///     X 坐标（距左边）
    /// </summary>
    public int x { get; set; }

    /// <summary>
    ///     Y 坐标（距顶边）
    /// </summary>
    public int y { get; set; }

    /// <summary>
    ///     宽度（字符数）
    /// </summary>
    public int width { get; set; }

    /// <summary>
    ///     高度（行数）
    /// </summary>
    public int height { get; set; }

    /// <summary>
    ///     最小宽度
    /// </summary>
    public int min_width { get; set; }

    /// <summary>
    ///     最大宽度
    /// </summary>
    public int max_width { get; set; } = int.MaxValue;

    /// <summary>
    ///     最小高度
    /// </summary>
    public int min_height { get; set; } = 1;

    /// <summary>
    ///     最大高度
    /// </summary>
    public int max_height { get; set; } = int.MaxValue;

    /// <summary>
    ///     外边距
    /// </summary>
    public Thickness margin { get; set; } = Thickness.zero;

    /// <summary>
    ///     是否可见
    /// </summary>
    public bool visible { get; set; } = true;

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool enabled { get; set; } = true;

    /// <summary>
    ///     是否接受 Tab 焦点
    /// </summary>
    public bool tab_stop { get; set; }

    /// <summary>
    ///     Tab 顺序
    /// </summary>
    public int tab_index { get; set; }

    /// <summary>
    ///     当前是否聚焦
    /// </summary>
    public bool is_focused { get; private set; }

    /// <summary>
    ///     控件 ID
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    ///     附加数据
    /// </summary>
    public object? tag { get; set; }

    /// <summary>
    ///     水平对齐
    /// </summary>
    public HorizontalAlignment horizontal_alignment { get; set; } = HorizontalAlignment.left;

    /// <summary>
    ///     垂直对齐
    /// </summary>
    public VerticalAlignment vertical_alignment { get; set; } = VerticalAlignment.top;

    /// <summary>
    ///     设置聚焦状态（供框架内部使用）
    /// </summary>
    /// <param name="focused">是否聚焦</param>
    internal void set_focused(bool focused)
    {
        is_focused = focused;
    }

    /// <summary>
    ///     渲染控件到指定上下文
    /// </summary>
    /// <param name="ctx">渲染上下文</param>
    public abstract void render(RenderContext ctx);

    /// <summary>
    ///     处理键盘输入事件，子类可重写以实现自定义键盘交互
    /// </summary>
    /// <param name="key">键盘输入信息</param>
    /// <returns>是否已处理该按键</returns>
    public virtual bool OnKeyDown(ConsoleKeyInfo key)
    {
        return false;
    }

    /// <summary>
    ///     填充父容器全部可用空间
    /// </summary>
    public View fill()
    {
        horizontal_alignment = HorizontalAlignment.fill;
        vertical_alignment = VerticalAlignment.fill;
        return this;
    }

    /// <summary>
    ///     在父容器中居中
    /// </summary>
    public View center()
    {
        horizontal_alignment = HorizontalAlignment.center;
        vertical_alignment = VerticalAlignment.center;
        return this;
    }

    /// <summary>
    ///     设置外边距
    /// </summary>
    /// <param name="top">上边距</param>
    /// <param name="right">右边距</param>
    /// <param name="bottom">下边距</param>
    /// <param name="left">左边距</param>
    public View with_margin(int top = 0, int right = 0, int bottom = 0, int left = 0)
    {
        margin = new Thickness(top, right, bottom, left);
        return this;
    }
}