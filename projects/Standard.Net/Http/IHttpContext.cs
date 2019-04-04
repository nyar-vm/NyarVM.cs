namespace Std.Net.Http;

/// <summary>
///     HTTP 请求上下文接口，提供请求和响应的完整访问能力。
/// </summary>
public interface IHttpContext
{
    /// <summary>
    ///     HTTP 请求信息。
    /// </summary>
    IHttpRequest request { get; }

    /// <summary>
    ///     HTTP 响应信息。
    /// </summary>
    IHttpResponse response { get; }

    /// <summary>
    ///     请求级别的服务提供器。
    /// </summary>
    IServiceProvider request_services { get; set; }

    /// <summary>
    ///     上下文项字典，用于在中间件之间传递数据。
    /// </summary>
    IDictionary<string, object?> items { get; }

    /// <summary>
    ///     请求取消令牌。
    /// </summary>
    CancellationToken request_aborted { get; set; }
}