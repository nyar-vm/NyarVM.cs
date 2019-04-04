using System.Text.Json;
using Std.Net.Http;

namespace Std.Net.Routing;

/// <summary>
///     控制器基类，提供 HTTP 响应便捷方法。
///     子类通过 <see cref="context" /> 访问当前请求和响应。
/// </summary>
public abstract class ControllerBase
{
    /// <summary>
    ///     当前路由上下文，在请求处理前由框架注入。
    /// </summary>
    public RouteContext context { get; internal set; } = null!;

    /// <summary>
    ///     返回 JSON 响应。
    /// </summary>
    /// <param name="data">要序列化的数据</param>
    /// <param name="statusCode">HTTP 状态码，默认 <see cref="HttpStatusCode.ok" /></param>
    protected void json(object? data, HttpStatusCode statusCode = HttpStatusCode.ok)
    {
        var json = JsonSerializer.Serialize(data);
        context.response.status_code = statusCode;
        context.response.json(json);
    }

    /// <summary>
    ///     返回成功响应（状态码 200），包含 data 字段。
    /// </summary>
    /// <param name="data">响应数据</param>
    protected void ok(object? data)
    {
        json(new { success = true, data });
    }

    /// <summary>
    ///     返回成功响应（状态码 200），无数据。
    /// </summary>
    protected void ok()
    {
        context.response.status_code = HttpStatusCode.ok;
    }

    /// <summary>
    ///     返回创建成功响应（状态码 201）。
    /// </summary>
    /// <param name="data">创建的资源数据</param>
    protected void created(object? data)
    {
        json(new { success = true, data }, HttpStatusCode.created);
    }

    /// <summary>
    ///     返回无内容响应（状态码 204）。
    /// </summary>
    protected void no_content()
    {
        context.response.no_content();
    }

    /// <summary>
    ///     返回错误请求响应（状态码 400）。
    /// </summary>
    /// <param name="message">错误消息</param>
    protected void bad_request(string message)
    {
        json(new { success = false, error = message }, HttpStatusCode.bad_request);
    }

    /// <summary>
    ///     返回未授权响应（状态码 401）。
    /// </summary>
    /// <param name="message">错误消息，默认为"未授权"</param>
    protected void unauthorized(string? message = null)
    {
        json(new { success = false, error = message ?? "未授权" }, HttpStatusCode.unauthorized);
    }

    /// <summary>
    ///     返回禁止访问响应（状态码 403）。
    /// </summary>
    /// <param name="message">错误消息，默认为"禁止访问"</param>
    protected void forbidden(string? message = null)
    {
        json(new { success = false, error = message ?? "禁止访问" }, HttpStatusCode.forbidden);
    }

    /// <summary>
    ///     返回未找到响应（状态码 404）。
    /// </summary>
    /// <param name="message">错误消息，默认为"资源未找到"</param>
    protected void not_found(string? message = null)
    {
        json(new { success = false, error = message ?? "资源未找到" }, HttpStatusCode.not_found);
    }

    /// <summary>
    ///     返回指定状态码的响应。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码</param>
    protected void status_code(HttpStatusCode statusCode)
    {
        context.response.status_code = statusCode;
    }

    /// <summary>
    ///     返回验证失败响应（状态码 422）。
    /// </summary>
    /// <param name="errors">验证错误列表</param>
    protected void validation_failed(IReadOnlyList<string> errors)
    {
        json(new { success = false, errors }, HttpStatusCode.unprocessable_entity);
    }

    /// <summary>
    ///     返回内部服务器错误响应（状态码 500）。
    /// </summary>
    /// <param name="message">错误消息，默认为"内部服务器错误"</param>
    protected void internal_server_error(string? message = null)
    {
        json(new { success = false, error = message ?? "内部服务器错误" }, HttpStatusCode.internal_server_error);
    }
}