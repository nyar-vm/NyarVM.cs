using Std.Net.Http;

namespace Std.Net;

/// <summary>
///     Net 应用，组合路由器和中间件管线，处理 HTTP 请求
/// </summary>
public sealed class NetApp
{
    private readonly MiddlewarePipeline _pipeline;
    private readonly Router _router;

    /// <summary>
    ///     初始化 Net 应用实例
    /// </summary>
    /// <param name="router">路由器</param>
    /// <param name="pipeline">中间件管线</param>
    public NetApp(Router router, MiddlewarePipeline pipeline)
    {
        _router = router;
        _pipeline = pipeline;
    }

    /// <summary>
    ///     处理 HTTP 请求。先执行中间件管线，再路由匹配
    /// </summary>
    /// <param name="request">HTTP 请求</param>
    public async ValueTask handle(HttpRequest request)
    {
        var context = new RouteContext(request);

        await _pipeline.execute(context, async () =>
        {
            var handler = _router.match(request);

            if (handler.is_some)
            {
                await handler.value.handle(context);
            }
            else
            {
                context.response.status_code = HttpStatusCode.not_found;
                context.response.text("404 Not Found");
            }
        });
    }
}