using Std.DataProcess.Write;
using Std.Text;

namespace Std.Net.Http;

/// <summary>
///     HTTP 响应，封装状态码、头部和载荷�?/// 响应在中间件管线中逐步构建，最终由服务器序列化发送�?///
/// </summary>
public sealed class HttpResponse
{
    /// <summary>
    ///     响应状态码�?    ///
    /// </summary>
    public HttpStatusCode status_code { get; set; } = HttpStatusCode.ok;

    /// <summary>
    ///     响应头部集合�?    ///
    /// </summary>
    public Dictionary<string, string> headers { get; } = new();

    /// <summary>
    ///     响应载荷字节�?    ///
    /// </summary>
    public byte[] body { get; set; } = [];

    /// <summary>
    ///     设置响应头部�?    ///
    /// </summary>
    /// <param name="name">
    ///     头部名称�?/param>
    ///     <param name="value">头部值�?/param>
    public void set_header(string name, string value)
    {
        headers[name] = value;
    }

    /// <summary>
    ///     设置 JSON 响应。自动设�?Content-Type 和载荷�?    ///
    /// </summary>
    /// <param name="json">JSON 字符串�?/param>
    public void json(string json)
    {
        headers["Content-Type"] = "application/json; charset=utf-8";
        body = SonicEncoding.encode_utf8(json);
    }

    /// <summary>
    ///     设置纯文本响应。自动设�?Content-Type 和载荷�?    ///
    /// </summary>
    /// <param name="text">文本内容�?/param>
    public void text(string text)
    {
        headers["Content-Type"] = "text/plain; charset=utf-8";
        body = SonicEncoding.encode_utf8(text);
    }

    /// <summary>
    ///     设置 HTML 响应。自动设�?Content-Type 和载荷�?    ///
    /// </summary>
    /// <param name="html">HTML 内容�?/param>
    public void html(string html)
    {
        headers["Content-Type"] = "text/html; charset=utf-8";
        body = SonicEncoding.encode_utf8(html);
    }

    /// <summary>
    ///     设置二进制响应。自动设�?Content-Type �?application/octet-stream�?    ///
    /// </summary>
    /// <param name="data">
    ///     二进制数据�?/param>
    ///     <param name="contentType">Content-Type，默�?application/octet-stream�?/param>
    public void bytes(ReadOnlySpan<byte> data, string contentType = "application/octet-stream")
    {
        headers["Content-Type"] = contentType;
        body = [.. data];
    }

    /// <summary>
    ///     设置重定向响应�?    ///
    /// </summary>
    /// <param name="location">
    ///     目标 URL�?/param>
    ///     <param name="permanent">是否为永久重定向�?01），否则为临时重定向�?02）�?/param>
    public void redirect(string location, bool permanent = false)
    {
        status_code = permanent ? HttpStatusCode.moved_permanently : HttpStatusCode.found;
        headers["Location"] = location;
    }

    /// <summary>
    ///     设置空响应（204 No Content）�?    ///
    /// </summary>
    public void no_content()
    {
        status_code = HttpStatusCode.no_content;
        body = [];
    }

    /// <summary>
    ///     将响应序列化�?HTTP/1.1 响应字节并写�?<see cref="IBufferWriter{Byte}" />�?    ///
    /// </summary>
    /// <param name="writer">目标字节输出器�?/param>
    public void write_to(IBufferWriter<byte> writer)
    {
        var statusLine = $"HTTP/1.1 {(int)status_code} {status_phrase(status_code)}\r\n";

        if (!headers.ContainsKey("Content-Length")) headers["Content-Length"] = body.Length.ToString();

        using var headerBuffer = new ArrayBufferWriter<byte>();
        var statusBytes = SonicEncoding.encode_ascii(statusLine);
        var span = headerBuffer.get_span(statusBytes.Length);
        statusBytes.CopyTo(span);
        headerBuffer.advance(statusBytes.Length);

        foreach (var (name, value) in headers)
        {
            var headerLine = SonicEncoding.encode_ascii($"{name}: {value}\r\n");
            var hSpan = headerBuffer.get_span(headerLine.Length);
            headerLine.CopyTo(hSpan);
            headerBuffer.advance(headerLine.Length);
        }

        var crlf = SonicEncoding.encode_ascii("\r\n");
        var cSpan = headerBuffer.get_span(crlf.Length);
        crlf.CopyTo(cSpan);
        headerBuffer.advance(crlf.Length);

        var totalHeaderLength = headerBuffer.written_span.Length;
        var outputSpan = writer.get_span(totalHeaderLength + body.Length);
        headerBuffer.written_span.CopyTo(outputSpan);
        body.CopyTo(outputSpan[totalHeaderLength..]);
        writer.advance(totalHeaderLength + body.Length);
    }

    /// <summary>
    ///     获取 HTTP 状态码对应的默认原因短语�?    ///
    /// </summary>
    private static string status_phrase(HttpStatusCode code)
    {
        return code switch
        {
            HttpStatusCode.ok => "OK",
            HttpStatusCode.created => "Created",
            HttpStatusCode.switching_protocols => "Switching Protocols",
            HttpStatusCode.no_content => "No Content",
            HttpStatusCode.moved_permanently => "Moved Permanently",
            HttpStatusCode.found => "Found",
            HttpStatusCode.not_modified => "Not Modified",
            HttpStatusCode.bad_request => "Bad Request",
            HttpStatusCode.unauthorized => "Unauthorized",
            HttpStatusCode.forbidden => "Forbidden",
            HttpStatusCode.not_found => "Not Found",
            HttpStatusCode.method_not_allowed => "Method Not Allowed",
            HttpStatusCode.conflict => "Conflict",
            HttpStatusCode.unprocessable_entity => "Unprocessable Entity",
            HttpStatusCode.too_many_requests => "Too Many Requests",
            HttpStatusCode.internal_server_error => "Internal Server Error",
            HttpStatusCode.bad_gateway => "Bad Gateway",
            HttpStatusCode.service_unavailable => "Service Unavailable",
            _ => "Unknown"
        };
    }
}