namespace Hermes.Rpc.Middleware;

/// <summary>
///     RPC 中间件接口
/// </summary>
public interface IHermesRpcMiddleware
{
    /// <summary>
    ///     处理 RPC 请求
    /// </summary>
    Task HandleAsync(HttpContext context, Func<Task> next);
}