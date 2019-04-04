namespace Std.App.Client;

/// <summary>
///     客户端应用生命周期阶段
/// </summary>
public enum AppLifecycle
{
    /// <summary>
    ///     应用尚未启动
    /// </summary>
    Idle,

    /// <summary>
    ///     应用正在启动
    /// </summary>
    Starting,

    /// <summary>
    ///     应用已启动，正常运行中
    /// </summary>
    Running,

    /// <summary>
    ///     应用已暂停（如移动端切到后台）
    /// </summary>
    Paused,

    /// <summary>
    ///     应用正在恢复
    /// </summary>
    Resuming,

    /// <summary>
    ///     应用正在停止
    /// </summary>
    Stopping,

    /// <summary>
    ///     应用已停止
    /// </summary>
    Stopped
}