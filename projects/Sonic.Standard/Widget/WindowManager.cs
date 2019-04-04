using Core.Widget.Window;

namespace Std.Widget;

/// <summary>
///     窗口管理器，实现 IWindowManager 接口，管理窗口的创建和关闭
/// </summary>
public sealed class WindowManager : IWindowManager
{
    /// <summary>
    ///     已创建的窗口列表
    /// </summary>
    private readonly List<WidgetWindow> _windows = [];

    /// <summary>
    ///     创建新窗口
    /// </summary>
    /// <param name="title">窗口标题</param>
    /// <returns>创建的窗口实例</returns>
    public IWindow create_window(string title)
    {
        var window = new WidgetWindow(title);
        _windows.Add(window);
        return window;
    }

    /// <summary>
    ///     关闭窗口
    /// </summary>
    /// <param name="window">要关闭的窗口</param>
    public void close_window(IWindow window)
    {
        if (window is WidgetWindow w) _windows.Remove(w);
    }
}