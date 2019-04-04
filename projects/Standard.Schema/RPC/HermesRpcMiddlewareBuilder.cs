using Hermes.Rpc.Middleware.Authentication;
using Hermes.Rpc.Middleware.CircuitBreaker;
using Hermes.Rpc.Middleware.Logging;
using Hermes.Rpc.Middleware.RateLimiting;

namespace Hermes.Rpc.Middleware;

/// <summary>
///     RPC 中间件管道构建器
/// </summary>
public sealed class HermesRpcMiddlewareBuilder
{
    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = [];

    /// <summary>
    ///     添加中间件
    /// </summary>
    public HermesRpcMiddlewareBuilder Use<TMiddleware>() where TMiddleware : IHermesRpcMiddleware, new()
    {
        _components.Add(next =>
        {
            var middleware = new TMiddleware();
            return async context => { await middleware.HandleAsync(context, () => next(context)); };
        });
        return this;
    }

    /// <summary>
    ///     添加认证中间件
    /// </summary>
    public HermesRpcMiddlewareBuilder UseAuthentication(IAuthenticationHandler handler)
    {
        _components.Add(next =>
        {
            var middleware = new AuthenticationMiddleware(handler);
            return async context => { await middleware.HandleAsync(context, () => next(context)); };
        });
        return this;
    }

    /// <summary>
    ///     添加限流中间件
    /// </summary>
    public HermesRpcMiddlewareBuilder UseRateLimiting(IRateLimitStore store, RateLimitOptions? options = null)
    {
        _components.Add(next =>
        {
            var middleware = new RateLimitingMiddleware(store, options ?? new RateLimitOptions());
            return async context => { await middleware.HandleAsync(context, () => next(context)); };
        });
        return this;
    }

    /// <summary>
    ///     添加熔断中间件
    /// </summary>
    public HermesRpcMiddlewareBuilder UseCircuitBreaker(ICircuitBreakerStateStore store,
        CircuitBreakerOptions? options = null)
    {
        _components.Add(next =>
        {
            var middleware = new CircuitBreakerMiddleware(store, options ?? new CircuitBreakerOptions());
            return async context => { await middleware.HandleAsync(context, () => next(context)); };
        });
        return this;
    }

    /// <summary>
    ///     添加日志中间件
    /// </summary>
    public HermesRpcMiddlewareBuilder UseLogging(ILogger<RpcLoggingMiddleware>? logger = null)
    {
        _components.Add(next =>
        {
            var middleware = logger != null
                ? new RpcLoggingMiddleware(logger)
                : new RpcLoggingMiddleware(NullLogger<RpcLoggingMiddleware>.Instance);
            return async context => { await middleware.HandleAsync(context, () => next(context)); };
        });
        return this;
    }

    /// <summary>
    ///     构建请求委托
    /// </summary>
    public RequestDelegate Build(RequestDelegate terminal)
    {
        var app = terminal;
        for (var i = _components.Count - 1; i >= 0; i--) app = _components[i](app);

        return app;
    }
}