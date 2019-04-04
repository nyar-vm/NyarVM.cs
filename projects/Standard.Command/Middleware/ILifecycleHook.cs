using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     命令生命周期钩子，在命令执行前后和发生错误时触发
/// </summary>
public interface ILifecycleHook
{
    /// <summary>
    ///     命令执行前调用
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <returns>返回 false 可取消执行</returns>
    Task<bool> before_execute(ICommandContext context);

    /// <summary>
    ///     命令执行后调用
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <param name="exitCode">强类型退出码</param>
    Task after_execute(ICommandContext context, ExitCode exitCode);

    /// <summary>
    ///     命令执行发生错误时调用
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <param name="exception">发生的异常</param>
    /// <returns>返回 true 表示异常已处理</returns>
    Task<bool> on_error(ICommandContext context, Exception exception);
}