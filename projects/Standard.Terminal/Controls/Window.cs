using Std.Terminal.Layout;

namespace Std.Terminal.Controls;

/// <summary>
///     窗口控件，支持模态/非模态显示、居中定位、遮罩和内部焦点导航
/// </summary>
public sealed class Window : View
{
    private TaskCompletionSource<bool>? _dialog_tcs;

    /// <summary>
    ///     创建窗口
    /// </summary>
    /// <param name="title">窗口标题</param>
    public Window(string title)
    {
        this.title = title;
        width = 40;
        height = 15;
        x = 5;
        y = 3;
    }

    /// <summary>
    ///     窗口标题
    /// </summary>
    public string title { get; set; }

    /// <summary>
    ///     窗口内容
    /// </summary>
    public View? content { get; set; }

    /// <summary>
    ///     是否为模态窗口
    /// </summary>
    public bool modal { get; set; }

    /// <summary>
    ///     是否显示背景遮罩（模态时默认开启）
    /// </summary>
    public bool show_backdrop { get; set; } = true;

    /// <summary>
    ///     背景遮罩颜色
    /// </summary>
    public RgbColor backdrop_color { get; set; } = RgbColor.black;

    /// <summary>
    ///     边框样式
    /// </summary>
    public BorderStyle border_style { get; set; } = BorderStyle.rounded;

    /// <summary>
    ///     是否可以拖拽
    /// </summary>
    public bool draggable { get; set; } = true;

    /// <summary>
    ///     窗口内部的 Tab 切控列表
    /// </summary>
    internal List<View> _tab_stop_controls { get; } = [];

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        if (modal && show_backdrop) ctx.fill_rect(0, 0, ctx.width, ctx.height, backdrop_color);

        var borderFg = is_focused ? new RgbColor(0, 200, 255) : new RgbColor(100, 100, 200);
        var titleFg = is_focused ? RgbColor.White : new RgbColor(200, 200, 200);
        var contentBg = new RgbColor(10, 10, 30);

        ctx.fill_rect(x + 1, y + 1, width - 2, height - 2, contentBg);
        ctx.draw_border(x, y, width, height, border_style, borderFg, ctx.default_background);
        ctx.draw_text(x + 2, y, $" {title} ", titleFg, contentBg);

        if (content != null)
        {
            var innerWidth = width - 4;
            var innerHeight = height - 4;

            if (innerWidth > 0 && innerHeight > 0)
            {
                var innerCtx = ctx.create_child(x + 2, y + 2, innerWidth, innerHeight);
                content.width = innerWidth;
                content.height = innerHeight;
                content.render(innerCtx);
            }
        }
    }

    /// <summary>
    ///     将窗口居中于指定区域
    /// </summary>
    /// <param name="containerWidth">容器宽度</param>
    /// <param name="containerHeight">容器高度</param>
    public Window CenterOnScreen(int containerWidth, int containerHeight)
    {
        x = System.Math.Max(0, (containerWidth - width) / 2);
        y = System.Math.Max(0, (containerHeight - height) / 2);
        return this;
    }

    /// <summary>
    ///     将窗口居中于指定区域的垂直中线
    /// </summary>
    /// <param name="containerHeight">容器高度</param>
    public Window center_vertical(int containerHeight)
    {
        y = System.Math.Max(0, (containerHeight - height) / 2);
        return this;
    }

    /// <summary>
    ///     将窗口居中于指定区域的水平中线
    /// </summary>
    /// <param name="containerWidth">容器宽度</param>
    public Window center_horizontal(int containerWidth)
    {
        x = System.Math.Max(0, (containerWidth - width) / 2);
        return this;
    }

    /// <summary>
    ///     显示为模态对话框
    /// </summary>
    /// <returns>对话框结果</returns>
    public Task<bool> show_dialog()
    {
        modal = true;
        collect_tab_stops();
        _dialog_tcs = new TaskCompletionSource<bool>();
        return _dialog_tcs.Task;
    }

    /// <summary>
    ///     关闭窗口
    /// </summary>
    /// <param name="result">对话框结果</param>
    public void close(bool result = true)
    {
        if (_dialog_tcs != null) _dialog_tcs.TrySetResult(result);
    }

    /// <summary>
    ///     将焦点移到窗口内下一个控件
    /// </summary>
    public void focus_next_in_window()
    {
        var focused = _tab_stop_controls.FirstOrDefault(c => c.is_focused);
        if (focused == null)
        {
            if (_tab_stop_controls.Count > 0) _tab_stop_controls[0].set_focused(true);

            return;
        }

        var index = _tab_stop_controls.IndexOf(focused);
        var nextIndex = (index + 1) % _tab_stop_controls.Count;
        focused.set_focused(false);
        _tab_stop_controls[nextIndex].set_focused(true);
    }

    /// <summary>
    ///     将焦点移到窗口内上一个控件
    /// </summary>
    public void focus_previous_in_window()
    {
        var focused = _tab_stop_controls.FirstOrDefault(c => c.is_focused);
        if (focused == null)
        {
            if (_tab_stop_controls.Count > 0) _tab_stop_controls[^1].set_focused(true);

            return;
        }

        var index = _tab_stop_controls.IndexOf(focused);
        var prevIndex = index == 0 ? _tab_stop_controls.Count - 1 : index - 1;
        focused.set_focused(false);
        _tab_stop_controls[prevIndex].set_focused(true);
    }

    /// <summary>
    ///     收集窗口内部的所有 Tab 控件
    /// </summary>
    private void collect_tab_stops()
    {
        _tab_stop_controls.Clear();
        collect_controls(content);
        if (_tab_stop_controls.Count > 0) _tab_stop_controls[0].set_focused(true);
    }

    private void collect_controls(View? view)
    {
        if (view == null) return;

        if (view is { visible: true, tab_stop: true }) _tab_stop_controls.Add(view);

        if (view is Panel { content: not null } panel) collect_controls(panel.content);

        if (view is VBox vbox)
            foreach (var child in vbox.children)
                collect_controls(child);

        if (view is HBox hbox)
            foreach (var child in hbox.children)
                collect_controls(child);
    }
}