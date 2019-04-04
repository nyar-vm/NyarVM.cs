using Std.Net.Http;

namespace Std.Net;

/// <summary>
///     路由上下文，封装当前请求、响应和路由匹配结果�?/// 在中间件管线和路由处理器之间传递�?///
/// </summary>
public sealed class RouteContext
{
    /// <summary>
    ///     初始化路由上下文�?    ///
    /// </summary>
    /// <param name="request">HTTP 请求�?/param>
    public RouteContext(HttpRequest request)
    {
        this.request = request;
        response = new HttpResponse();
    }

    /// <summary>
    ///     当前 HTTP 请求�?    ///
    /// </summary>
    public HttpRequest request { get; }

    /// <summary>
    ///     当前 HTTP 响应�?    ///
    /// </summary>
    public HttpResponse response { get; }

    /// <summary>
    ///     中间件共享数据字典，用于在管线中传递认证信息等�?    ///
    /// </summary>
    public Dictionary<string, object> items { get; } = new();

    /// <summary>
    ///     指示后续中间件是否应继续执行�?    /// 中间件调�?<see cref="short_circuit" /> 后，管线不再调用后续中间件�?    ///
    /// </summary>
    public bool is_short_circuited { get; private set; }

    /// <summary>
    ///     短路管线，阻止后续中间件执行�?    /// 用于认证失败、速率限制等场景：中间件直接写入响应并终止管线�?    ///
    /// </summary>
    public void short_circuit()
    {
        is_short_circuited = true;
    }
}