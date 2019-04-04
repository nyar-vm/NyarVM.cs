using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     生命周期钩子运行器，提供对 <see cref="ILifecycleHook" /> 集合的批量执行扩展方法
/// </summary>
public static class LifecycleHookRunner
{
    /// <summary>
    ///     批量执行所有钩子的 before_execute
    /// </summary>
    /// <param name="hooks">生命周期钩子集合</param>
    /// <param name="context">命令上下文</param>
    /// <returns>若所有钩子均同意继续执行则返回 true，否则返回 false</returns>
    public static async Task<bool> run_before_execute(this IEnumerable<ILifecycleHook> hooks, ICommandContext context)
    {
        foreach (var hook in hooks)
            if (!await hook.before_execute(context))
                return false;

        return true;
    }

    /// <summary>
    ///     批量执行所有钩子的 after_execute
    /// </summary>
    /// <param name="hooks">生命周期钩子集合</param>
    /// <param name="context">命令上下文</param>
    /// <param name="exitCode">退出码</param>
    public static async Task run_after_execute(this IEnumerable<ILifecycleHook> hooks, ICommandContext context,
        ExitCode exitCode)
    {
        foreach (var hook in hooks) await hook.after_execute(context, exitCode);
    }

    /// <summary>
    ///     批量执行所有钩子的 on_error
    /// </summary>
    /// <param name="hooks">生命周期钩子集合</param>
    /// <param name="context">命令上下文</param>
    /// <param name="exception">抛出的异常</param>
    /// <returns>若任一钩子处理了异常则返回 true，否则返回 false</returns>
    public static async Task<bool> run_on_error(this IEnumerable<ILifecycleHook> hooks, ICommandContext context,
        Exception exception)
    {
        foreach (var hook in hooks)
            if (await hook.on_error(context, exception))
                return true;

        return false;
    }
}