namespace Core.Widget.Window;

/// <summary>
///     IWindowManager 接口
/// </summary>
public interface IWindowManager
{
    /// <summary>
    ///     创建新窗口
    /// </summary>
    IWindow create_window(string title);

    /// <summary>
    ///     关闭窗口
    /// </summary>
    void close_window(IWindow window);
}