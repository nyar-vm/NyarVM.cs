namespace Std.Net;

/// <summary>
///     Net 应用构建器，提供流式 API 配置路由和中间件，最终构�?<see cref="NetApp" /> 实例�?///
/// </summary>
public sealed class NetApplicationBuilder
{
    private readonly MiddlewarePipeline _pipeline = new();
    private readonly Router _router = new();

    /// <summary>
    ///     注册 GET 路由�?    ///
    /// </summary>
    /// <param name="pattern">
    ///     URL 模式，如 <c>/users/:id</c>�?/param>
    ///     <param name="handler">
    ///         路由处理器�?/param>
    ///         <returns>当前构建器实例，支持链式调用�?/returns>
    public NetApplicationBuilder get(string pattern, IRouteHandler handler)
    {
        _router.get(pattern, handler);
        return this;
    }

    /// <summary>
    ///     注册 POST 路由�?    ///
    /// </summary>
    /// <param name="pattern">
    ///     URL 模式�?/param>
    ///     <param name="handler">
    ///         路由处理器�?/param>
    ///         <returns>当前构建器实例�?/returns>
    public NetApplicationBuilder post(string pattern, IRouteHandler handler)
    {
        _router.post(pattern, handler);
        return this;
    }

    /// <summary>
    ///     注册 PUT 路由�?    ///
    /// </summary>
    /// <param name="pattern">
    ///     URL 模式�?/param>
    ///     <param name="handler">
    ///         路由处理器�?/param>
    ///         <returns>当前构建器实例�?/returns>
    public NetApplicationBuilder put(string pattern, IRouteHandler handler)
    {
        _router.put(pattern, handler);
        return this;
    }

    /// <summary>
    ///     注册 DELETE 路由�?    ///
    /// </summary>
    /// <param name="pattern">
    ///     URL 模式�?/param>
    ///     <param name="handler">
    ///         路由处理器�?/param>
    ///         <returns>当前构建器实例�?/returns>
    public NetApplicationBuilder delete(string pattern, IRouteHandler handler)
    {
        _router.delete(pattern, handler);
        return this;
    }

    /// <summary>
    ///     注册 PATCH 路由�?    ///
    /// </summary>
    /// <param name="pattern">
    ///     URL 模式�?/param>
    ///     <param name="handler">
    ///         路由处理器�?/param>
    ///         <returns>当前构建器实例�?/returns>
    public NetApplicationBuilder patch(string pattern, IRouteHandler handler)
    {
        _router.patch(pattern, handler);
        return this;
    }

    /// <summary>
    ///     注册中间件。中间件按注册顺序执行�?    ///
    /// </summary>
    /// <param name="middleware">
    ///     要注册的中间件�?/param>
    ///     <returns>当前构建器实例�?/returns>
    public NetApplicationBuilder use(IMiddleware middleware)
    {
        _pipeline.use(middleware);
        return this;
    }

    /// <summary>
    ///     构建 <see cref="NetApp" /> 实例�?    ///
    /// </summary>
    /// <returns>配置完成�?Net 应用�?/returns>
    public NetApp build()
    {
        return new NetApp(_router, _pipeline);
    }
}