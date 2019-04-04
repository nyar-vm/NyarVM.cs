using Core.Widget.Window;

namespace Std.Widget;

/// <summary>
///     窗口实现，实现 IWindow 接口，描述窗口的基本属性
/// </summary>
public sealed class WidgetWindow : IWindow
{
    /// <summary>
    ///     初始化窗口
    /// </summary>
    /// <param name="title">窗口标题</param>
    public WidgetWindow(string title)
    {
        this.title = title;
        state = WindowState.normal;
    }

    /// <summary>
    ///     窗口标题
    /// </summary>
    public string title { get; set; }

    /// <summary>
    ///     窗口状态
    /// </summary>
    public WindowState state { get; }
}