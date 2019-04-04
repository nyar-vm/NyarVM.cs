using Core.Terminal;

namespace Std.Command.Ports;

/// <summary>
///     交互式 Shell 生命周期接口，所有长时间运行的 Shell 形态都实现此接口
///     CliApplication、ReplApplication、TuiApplication、GuiApplication 均实现此接口
/// </summary>
public interface IInteractiveShell
{
    /// <summary>
    ///     Shell 名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     当前 Shell 状态
    /// </summary>
    ShellState state { get; }

    /// <summary>
    ///     异步启动 Shell 主循环，直到收到停止信号
    /// </summary>
    /// <param name="cancellation">取消令牌</param>
    /// <returns>退出码</returns>
    Task<ExitCode> run(CancellationToken cancellation = default);

    /// <summary>
    ///     异步停止 Shell
    /// </summary>
    Task stop();

    /// <summary>
    ///     Shell 状态变化事件
    /// </summary>
    event Action<ShellState, ShellState>? StateChanged;
}