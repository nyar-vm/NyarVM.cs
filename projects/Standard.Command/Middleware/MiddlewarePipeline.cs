using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     中间件管道，管理中间件的注册与执行
/// </summary>
public sealed class MiddlewarePipeline : IMiddlewarePipeline
{
    private readonly List<Func<ICommandContext, Func<Task<ExitCode>>, CancellationToken, Task<ExitCode>>> _middlewares =
        [];

    /// <summary>
    ///     执行管道，依次调用所有中间件后执行最终处理程序
    /// </summary>
    /// <param name="context">命令执行上下文</param>
    /// <param name="handler">最终处理程序</param>
    /// <param name="cancellation_token">取消令牌</param>
    /// <returns>强类型退出码</returns>
    public async Task<ExitCode> execute(ICommandContext context, Func<Task<ExitCode>> handler,
        CancellationToken cancellationToken)
    {
        var next = handler;

        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentNext = next;
            next = () => middleware(context, currentNext, cancellationToken);
        }

        return await next();
    }

    /// <summary>
    ///     注册中间件类型
    /// </summary>
    /// <typeparam name="T">实现了 ICommandMiddleware 的类型</typeparam>
    public MiddlewarePipeline use<T>() where T : ICommandMiddleware, new()
    {
        var middleware = new T();
        return use(middleware);
    }

    /// <summary>
    ///     注册中间件实例
    /// </summary>
    /// <param name="middleware">中间件实例</param>
    public MiddlewarePipeline use(ICommandMiddleware middleware)
    {
        _middlewares.Add((context, next, cancellationToken) => middleware.invoke(context, next, cancellationToken));
        return this;
    }

    /// <summary>
    ///     注册 Lambda 中间件
    /// </summary>
    /// <param name="middleware">中间件委托</param>
    public MiddlewarePipeline use(
        Func<ICommandContext, Func<Task<ExitCode>>, CancellationToken, Task<ExitCode>> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }
}