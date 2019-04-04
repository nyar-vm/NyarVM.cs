namespace Std.Net.Http;

/// <summary>
///     标记类为 HTTP 服务器，指定监听端口和主机地址
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class HttpServerAttribute : Attribute
{
    /// <summary>
    ///     监听端口，默认为 8080
    /// </summary>
    public int port { get; set; } = 8080;

    /// <summary>
    ///     监听主机地址，默认为 localhost
    /// </summary>
    public string host { get; set; } = "localhost";
}