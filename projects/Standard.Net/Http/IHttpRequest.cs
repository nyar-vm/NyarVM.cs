namespace Std.Net.Http;

/// <summary>
///     HTTP 请求接口，封装请求行、头部和载荷。
/// </summary>
public interface IHttpRequest
{
    /// <summary>
    ///     HTTP 方法（GET、POST、PUT、DELETE 等）。
    /// </summary>
    string method { get; set; }

    /// <summary>
    ///     请求路径。
    /// </summary>
    string path { get; set; }

    /// <summary>
    ///     查询字符串。
    /// </summary>
    string query_string { get; set; }

    /// <summary>
    ///     请求头集合。
    /// </summary>
    IDictionary<string, string> headers { get; set; }

    /// <summary>
    ///     请求体流。
    /// </summary>
    System.IO.Stream body { get; set; }

    /// <summary>
    ///     请求内容类型。
    /// </summary>
    string? content_type { get; set; }
}