namespace Std.Command;

/// <summary>
///     Shell 运行状态
/// </summary>
public enum ShellState
{
    /// <summary>
    ///     尚未启动
    /// </summary>
    idle,

    /// <summary>
    ///     正在启动中
    /// </summary>
    starting,

    /// <summary>
    ///     主循环运行中
    /// </summary>
    running,

    /// <summary>
    ///     正在停止中
    /// </summary>
    stopping,

    /// <summary>
    ///     已完全停止
    /// </summary>
    stopped
}