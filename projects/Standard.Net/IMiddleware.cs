namespace Std.Net;

/// <summary>
///     中间件接口，在请求处理管线中执行横切关注点逻辑�?/// 中间件通过调用 <c>next</c> 委托将控制权传递给下一个中间件�?/// 也可以通过
///     <see cref="RouteContext.short_circuit" /> 终止管线�?///
/// </summary>
public interface IMiddleware
{
    /// <summary>
    ///     处理请求。中间件在调�?<c>next</c> 前后均可执行逻辑�?    ///
    /// </summary>
    /// <param name="context">
    ///     路由上下文�?/param>
    ///     <param name="next">调用下一个中间件的委托�?/param>
    ValueTask invoke(RouteContext context, Func<ValueTask> next);
}