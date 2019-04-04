namespace Std.App.Client;

/// <summary>
///     宿主 Shell 类型枚举
/// </summary>
public enum HostShellKind
{
    /// <summary>
    ///     命令行界面
    /// </summary>
    Cli,

    /// <summary>
    ///     终端用户界面（基于 curses 等）
    /// </summary>
    Tui,

    /// <summary>
    ///     图形用户界面（原生控件）
    /// </summary>
    Gui,

    /// <summary>
    ///     Web 用户界面（基于 DOM）
    /// </summary>
    WebUi,

    /// <summary>
    ///     游戏内叠加层（基于纹理）
    /// </summary>
    Hud
}

/// <summary>
///     宿主 Shell 接口，提供原生平台能力。
///     每种宿主只需实现此接口即可运行 std.app.client 应用。
/// </summary>
public interface IHostShell
{
    /// <summary>
    ///     宿主类型
    /// </summary>
    HostShellKind Kind { get; }

    /// <summary>
    ///     创建或获取主窗口
    /// </summary>
    /// <param name="title">窗口标题</param>
    /// <param name="width">窗口宽度</param>
    /// <param name="height">窗口高度</param>
    Task ShowWindowAsync(string title, int width, int height);

    /// <summary>
    ///     关闭主窗口
    /// </summary>
    Task CloseWindowAsync();

    /// <summary>
    ///     读取系统剪贴板文本
    /// </summary>
    Task<string?> GetClipboardTextAsync();

    /// <summary>
    ///     写入系统剪贴板文本
    /// </summary>
    /// <param name="text">要写入的文本</param>
    Task SetClipboardTextAsync(string text);

    /// <summary>
    ///     显示本地通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    Task ShowNotificationAsync(string title, string message);
}