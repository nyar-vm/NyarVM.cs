namespace Std.Net.Http;

/// <summary>
///     HTTP 响应接口，封装状态码、头部和载荷。
/// </summary>
public interface IHttpResponse
{
    /// <summary>
    ///     HTTP 状态码。
    /// </summary>
    int status_code { get; set; }

    /// <summary>
    ///     响应头集合。
    /// </summary>
    IDictionary<string, string> headers { get; set; }

    /// <summary>
    ///     响应体流。
    /// </summary>
    System.IO.Stream body { get; set; }

    /// <summary>
    ///     响应内容类型。
    /// </summary>
    string? content_type { get; set; }
}