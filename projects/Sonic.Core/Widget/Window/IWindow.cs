namespace Core.Widget.Window;

/// <summary>
///     IWindow 接口
/// </summary>
public interface IWindow
{
    /// <summary>
    ///     窗口标题
    /// </summary>
    string title { get; set; }

    /// <summary>
    ///     窗口状态
    /// </summary>
    WindowState state { get; }
}