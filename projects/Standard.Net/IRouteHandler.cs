namespace Std.Net;

/// <summary>
///     路由处理器接口，处理匹配到路由的请求�?///
/// </summary>
public interface IRouteHandler
{
    /// <summary>
    ///     处理请求�?    ///
    /// </summary>
    /// <param name="context">路由上下文�?/param>
    ValueTask handle(RouteContext context);
}