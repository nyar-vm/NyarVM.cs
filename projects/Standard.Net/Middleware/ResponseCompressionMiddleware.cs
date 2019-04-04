using System.IO.Compression;

namespace Std.Net.Middleware;

/// <summary>
///     响应压缩选项。
/// </summary>
public sealed class ResponseCompressionOptions
{
    /// <summary>
    ///     启用的压缩算法列表（默认 GZip + Deflate）。
    /// </summary>
    public IList<string> providers { get; set; } = new List<string> { "gzip", "deflate" };

    /// <summary>
    ///     仅对比此字节数大的响应启用压缩（默认 1024 字节）。
    /// </summary>
    public int minimum_compression_size { get; set; } = 1024;

    /// <summary>
    ///     需要压缩的 MIME 类型列表。
    /// </summary>
    public IList<string> mime_types { get; set; } = new List<string>
    {
        "text/plain",
        "text/html",
        "text/css",
        "text/javascript",
        "application/javascript",
        "application/json",
        "application/xml",
        "text/xml"
    };
}

/// <summary>
///     响应压缩中间件，根据客户端 Accept-Encoding 头选择 GZip 或 Deflate 压缩响应体。
/// </summary>
public sealed class ResponseCompressionMiddleware : IMiddleware
{
    private readonly ResponseCompressionOptions _options;

    /// <summary>
    ///     初始化响应压缩中间件。
    /// </summary>
    /// <param name="options">压缩选项</param>
    public ResponseCompressionMiddleware(ResponseCompressionOptions? options = null)
    {
        _options = options ?? new ResponseCompressionOptions();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        await next();

        var body = context.response.body;

        if (body is null || body.Length == 0) return;

        if (!should_compress(context, body)) return;

        var encoding = context.request.headers.TryGetValue("Accept-Encoding", out var enc)
            ? enc.ToLowerInvariant()
            : string.Empty;

        var compressed = compress_body(body, encoding);

        if (compressed is null) return;

        context.response.body = compressed.data;
        context.response.headers["Content-Encoding"] = compressed.algorithm;

        context.response.headers.TryAdd("Vary", "Accept-Encoding");
    }

    private bool should_compress(RouteContext context, byte[] body)
    {
        if (body.Length < _options.minimum_compression_size) return false;

        if (!context.response.headers.TryGetValue("Content-Type", out var contentType)) return false;

        var baseType = contentType.Split(';')[0].Trim().ToLowerInvariant();
        return _options.mime_types.Any(m => m.Equals(baseType, StringComparison.OrdinalIgnoreCase));
    }

    private CompressedResult? compress_body(byte[] body, string acceptEncoding)
    {
        if (_options.providers.Contains("gzip") && acceptEncoding.Contains("gzip"))
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest, true))
            {
                gzipStream.Write(body, 0, body.Length);
            }

            return new CompressedResult { data = outputStream.ToArray(), algorithm = "gzip" };
        }

        if (_options.providers.Contains("deflate") && acceptEncoding.Contains("deflate"))
        {
            using var outputStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Fastest, true))
            {
                deflateStream.Write(body, 0, body.Length);
            }

            return new CompressedResult { data = outputStream.ToArray(), algorithm = "deflate" };
        }

        return null;
    }

    private sealed class CompressedResult
    {
        public byte[] data { get; init; } = [];
        public string algorithm { get; init; } = string.Empty;
    }
}