namespace Valhalla;

/// <summary>
///     瓦尓哈拉统一错误响应格式
/// </summary>
public class ValhallaErrorResponse
{
    /// <summary>
    ///     错误描述信息
    /// </summary>
    public string error { get; set; } = string.Empty;

    /// <summary>
    ///     错误码（如 NOT_FOUND、VALIDATION_ERROR、INTERNAL_ERROR）
    /// </summary>
    public string code { get; set; } = string.Empty;

    /// <summary>
    ///     额外详情
    /// </summary>
    public Dictionary<string, string>? details { get; set; }

    /// <summary>
    ///     创建一个 404 错误响应
    /// </summary>
    public static ValhallaErrorResponse not_found(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { error = message, code = "NOT_FOUND", details = details };
    }

    /// <summary>
    ///     创建一个 400 验证错误响应
    /// </summary>
    public static ValhallaErrorResponse validation_error(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { error = message, code = "VALIDATION_ERROR", details = details };
    }

    /// <summary>
    ///     创建一个 500 内部错误响应
    /// </summary>
    public static ValhallaErrorResponse internal_error(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { error = message, code = "INTERNAL_ERROR", details = details };
    }

    /// <summary>
    ///     创建一个 409 冲突错误响应
    /// </summary>
    public static ValhallaErrorResponse conflict(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { error = message, code = "CONFLICT", details = details };
    }

    /// <summary>
    ///     创建一个 401 未认证错误响应
    /// </summary>
    public static ValhallaErrorResponse unauthorized(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { error = message, code = "UNAUTHORIZED", details = details };
    }

    /// <summary>
    ///     创建一个 403 禁止访问错误响应
    /// </summary>
    public static ValhallaErrorResponse forbidden(string message, Dictionary<string, string>? details = null)
    {
        return new ValhallaErrorResponse { error = message, code = "FORBIDDEN", details = details };
    }
}