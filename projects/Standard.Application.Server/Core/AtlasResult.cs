using System.Text.Json;
using Std.Text;

namespace Std.App.Server.Core;

/// <summary>
///     Action 执行结果，封装 HTTP 响应状态码和响应体
/// </summary>
public sealed class AtlasResult
{
    /// <summary>
    ///     委托型响应动作，用于 SSE 等流式场景
    /// </summary>
    private readonly Action<RouteContext>? _action;

    /// <summary>
    ///     初始化结果实例
    /// </summary>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="body">响应体</param>
    /// <param name="contentType">内容类型</param>
    private AtlasResult(HttpStatusCode statusCode, byte[]? body = null, string? contentType = null)
    {
        status_code = statusCode;
        this.body = body;
        content_type = contentType;
    }

    /// <summary>
    ///     使用委托初始化结果实例，用于流式响应场景
    /// </summary>
    /// <param name="action">响应配置委托</param>
    private AtlasResult(Action<RouteContext> action)
    {
        status_code = HttpStatusCode.ok;
        _action = action;
    }

    /// <summary>
    ///     HTTP 状态码
    /// </summary>
    public HttpStatusCode status_code { get; }

    /// <summary>
    ///     响应体字节
    /// </summary>
    public byte[]? body { get; }

    /// <summary>
    ///     响应内容类型
    /// </summary>
    public string? content_type { get; }

    /// <summary>
    ///     返回 200 OK 无响应体
    /// </summary>
    public static AtlasResult ok()
    {
        return new(HttpStatusCode.ok);
    }

    /// <summary>
    ///     返回 200 OK JSON 响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="value">返回对象</param>
    /// <returns>结果</returns>
    public static AtlasResult ok<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new AtlasResult(HttpStatusCode.ok, SonicEncoding.encode_utf8(json), "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回 201 Created JSON 响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="value">返回对象</param>
    public static AtlasResult created<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new AtlasResult(HttpStatusCode.created, SonicEncoding.encode_utf8(json),
            "application/json; charset=utf-8");
    }

    /// <summary>
    ///     创建 SSE (Server-Sent Events) 流式响应
    /// </summary>
    /// <typeparam name="T">流数据类型</typeparam>
    /// <param name="stream">异步数据流</param>
    public static AtlasResult sse<T>(IAsyncEnumerable<T> stream)
    {
        return new AtlasResult(ctx =>
        {
            ctx.response.status_code = HttpStatusCode.ok;
            ctx.response.headers["Content-Type"] = "text/event-stream";
            ctx.response.headers["Cache-Control"] = "no-cache";
            ctx.response.headers["Connection"] = "keep-alive";
        });
    }

    /// <summary>
    ///     返回 204 No Content
    /// </summary>
    public static AtlasResult no_content()
    {
        return new(HttpStatusCode.no_content);
    }

    /// <summary>
    ///     返回 400 Bad Request JSON 响应
    /// </summary>
    /// <param name="message">错误消息</param>
    public static AtlasResult bad_request(string message)
    {
        var json = JsonSerializer.Serialize(new { error = message });
        return new AtlasResult(HttpStatusCode.bad_request, SonicEncoding.encode_utf8(json),
            "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回 401 Unauthorized JSON 响应
    /// </summary>
    /// <param name="message">错误消息</param>
    public static AtlasResult unauthorized(string message = "未授权")
    {
        var json = JsonSerializer.Serialize(new { error = message });
        return new AtlasResult(HttpStatusCode.unauthorized, SonicEncoding.encode_utf8(json),
            "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回 403 Forbidden JSON 响应
    /// </summary>
    /// <param name="message">错误消息</param>
    public static AtlasResult forbidden(string message = "禁止访问")
    {
        var json = JsonSerializer.Serialize(new { error = message });
        return new AtlasResult(HttpStatusCode.forbidden, SonicEncoding.encode_utf8(json),
            "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回 404 Not Found JSON 响应
    /// </summary>
    /// <param name="message">错误消息</param>
    public static AtlasResult not_found(string message = "未找到")
    {
        var json = JsonSerializer.Serialize(new { error = message });
        return new AtlasResult(HttpStatusCode.not_found, SonicEncoding.encode_utf8(json),
            "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回 500 Internal Server Error JSON 响应
    /// </summary>
    /// <param name="message">错误消息</param>
    public static AtlasResult internal_error(string message = "服务器内部错误")
    {
        var json = JsonSerializer.Serialize(new { error = message });
        return new AtlasResult(HttpStatusCode.internal_server_error, SonicEncoding.encode_utf8(json),
            "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回指定状态码的 JSON 响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="statusCode">状态码</param>
    /// <param name="value">返回对象</param>
    public static AtlasResult json<T>(HttpStatusCode statusCode, T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new AtlasResult(statusCode, SonicEncoding.encode_utf8(json), "application/json; charset=utf-8");
    }

    /// <summary>
    ///     返回纯文本响应
    /// </summary>
    /// <param name="statusCode">状态码</param>
    /// <param name="text">文本内容</param>
    public static AtlasResult text(HttpStatusCode statusCode, string text)
    {
        return new AtlasResult(statusCode, SonicEncoding.encode_utf8(text), "text/plain; charset=utf-8");
    }

    /// <summary>
    ///     将结果写入路由上下文的响应中
    /// </summary>
    /// <param name="context">路由上下文</param>
    public void apply_to(RouteContext context)
    {
        _action?.Invoke(context);

        context.response.status_code = status_code;

        if (content_type is not null) context.response.headers["Content-Type"] = content_type;

        if (body is not null) context.response.body = body;
    }
}