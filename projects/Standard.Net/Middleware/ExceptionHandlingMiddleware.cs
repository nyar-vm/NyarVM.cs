using System.Text.Json;
using Std.Net.Http;

namespace Std.Net.Middleware;

/// <summary>
///     全局异常处理中间件，捕获管道中未处理的异常并返回标准错误响应。
/// </summary>
public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ExceptionHandlingOptions _options;

    /// <summary>
    ///     初始化异常处理中间件。
    /// </summary>
    /// <param name="options">异常处理配置选项</param>
    public ExceptionHandlingMiddleware(ExceptionHandlingOptions? options = null)
    {
        _options = options ?? new ExceptionHandlingOptions();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            context.response.status_code = get_status_code(ex);

            var errorResponse = new
            {
                error = _options.include_exception_details
                    ? ex.Message
                    : "服务器内部错误",
                type = _options.include_exception_details
                    ? ex.GetType().Name
                    : null,
#if DEBUG
                stackTrace = ex.StackTrace
#else
                stackTrace = (string?)null
#endif
            };

            var json = JsonSerializer.Serialize(errorResponse);
            context.response.json(json);
        }
    }

    private HttpStatusCode get_status_code(Exception ex)
    {
        if (_options.custom_status_code_mapping.TryGetValue(ex.GetType(), out var code)) return code;

        return ex switch
        {
            ArgumentException or FormatException => HttpStatusCode.bad_request,
            UnauthorizedAccessException => HttpStatusCode.unauthorized,
            KeyNotFoundException => HttpStatusCode.not_found,
            InvalidOperationException => HttpStatusCode.conflict,
            _ => HttpStatusCode.internal_server_error
        };
    }
}

/// <summary>
///     异常处理配置选项。
/// </summary>
public sealed class ExceptionHandlingOptions
{
    /// <summary>
    ///     是否在响应中包含异常详细信息。
    /// </summary>
    public bool include_exception_details { get; set; }

    /// <summary>
    ///     自定义异常到状态码的映射。
    /// </summary>
    public Dictionary<Type, HttpStatusCode> custom_status_code_mapping { get; set; } = [];
}