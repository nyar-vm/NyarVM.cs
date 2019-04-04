namespace Std.Net;

/// <summary>
///     中间件管线，按注册顺序依次执行中间件链�?/// 每个中间件可以：1) �?next 前执行逻辑�?) 调用 next 传递控制权�?) �?next 后执行逻辑�?/// �?4) 调用
///     <see cref="RouteContext.short_circuit" /> 终止管线�?///
/// </summary>
public sealed class MiddlewarePipeline
{
    private readonly List<IMiddleware> _middlewares = [];

    /// <summary>
    ///     注册中间件。中间件按注册顺序执行�?    ///
    /// </summary>
    /// <param name="middleware">要注册的中间件�?/param>
    public void use(IMiddleware middleware)
    {
        _middlewares.Add(middleware);
    }

    /// <summary>
    ///     执行管线。按顺序调用所有中间件，最终调用终端处理器�?    ///
    /// </summary>
    /// <param name="context">
    ///     路由上下文�?/param>
    ///     <param name="terminal">终端处理器（通常是路由匹配后的处理器）�?/param>
    public async ValueTask execute(RouteContext context, Func<ValueTask> terminal)
    {
        if (_middlewares.Count == 0)
        {
            await terminal();
            return;
        }

        var index = 0;

        async ValueTask Next()
        {
            if (context.is_short_circuited) return;

            if (index < _middlewares.Count)
            {
                var middleware = _middlewares[index];
                index++;
                await middleware.invoke(context, Next);
            }
            else
            {
                await terminal();
            }
        }

        await Next();
    }
}