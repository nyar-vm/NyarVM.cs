using System.Net.Sockets;
using Std.Category;
using Std.Text;

namespace Std.Net.Http;

/// <summary>
///     HTTP 请求，封装请求行、头部和载荷。
///     请求对象在中间件管线中传递，各中间件可读取或修改请求属性。
/// </summary>
public sealed class HttpRequest
{
    /// <summary>
    ///     请求方法。
    /// </summary>
    public HttpMethod method { get; set; }

    /// <summary>
    ///     请求路径（不含查询字符串）。
    /// </summary>
    public string path { get; set; } = "/";

    /// <summary>
    ///     查询字符串（含前缀 <c>?</c>），可为空。
    /// </summary>
    public string query_string { get; set; } = "";

    /// <summary>
    ///     请求头部集合。
    /// </summary>
    public Dictionary<string, string> headers { get; } = new();

    /// <summary>
    ///     请求载荷字节。
    /// </summary>
    public byte[] body { get; set; } = [];

    /// <summary>
    ///     路由参数，由路由器在匹配时填充。
    ///     例如路径 <c>/users/:id</c> 匹配 <c>/users/42</c> 时，<c>route_params["id"]</c> 为 <c>"42"</c>。
    /// </summary>
    public Dictionary<string, string> route_params { get; } = new();

    /// <summary>
    ///     获取与此请求关联的 HTTP 响应对象。
    /// </summary>
    public HttpResponse response { get; } = new();

    /// <summary>
    ///     底层 Socket 连接，由服务器设置。中间件可通过此属性接管连接（如 WebSocket 升级）。
    /// </summary>
    public Socket? connection { get; set; }

    /// <summary>
    ///     指示连接是否已被中间件接管。当为 <c>true</c> 时，服务器不再发送 HTTP 响应和继续 HTTP 帧循环。
    /// </summary>
    public bool is_hijacked { get; set; }

    /// <summary>
    ///     获取指定请求头的值，不存在返回 <see cref="Option{T}.none" />。
    /// </summary>
    /// <param name="name">头部名称（不区分大小写）。</param>
    public Option<string> header(string name)
    {
        foreach (var (key, value) in headers)
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                return Option<string>.some(value);

        return Option<string>.none;
    }

    /// <summary>
    ///     获取指定路由参数的值，不存在返回 <see cref="Option{T}.none" />。
    /// </summary>
    /// <param name="name">参数名称。</param>
    public Option<string> param(string name)
    {
        return route_params.TryGetValue(name, out var value)
            ? Option<string>.some(value)
            : Option<string>.none;
    }

    /// <summary>
    ///     将请求载荷解码为 UTF-8 字符串。
    /// </summary>
    public string body_as_text()
    {
        return SonicEncoding.decode_utf8(body);
    }
}