using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     中间件管道接口，定义管道执行能力
/// </summary>
public interface IMiddlewarePipeline
{
    /// <summary>
    ///     执行管道，依次调用所有中间件后执行最终处理程序
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <param name="handler">最终处理程序</param>
    /// <param name="cancellation_token">取消令牌</param>
    /// <returns>强类型退出码</returns>
    Task<ExitCode> execute(ICommandContext context, Func<Task<ExitCode>> handler, CancellationToken cancellationToken);
}