using Std.Net.Http;
using HttpMethod = Std.Net.Http.HttpMethod;

namespace Std.Net.Middleware;

/// <summary>
///     静态文件服务选项。
/// </summary>
public sealed class StaticFileOptions
{
    /// <summary>
    ///     静态文件根目录（绝对路径或相对路径）。
    /// </summary>
    public string root_path { get; set; } = "./wwwroot";

    /// <summary>
    ///     默认文件名（目录索引）。
    /// </summary>
    public string default_file { get; set; } = "index.html";

    /// <summary>
    ///     是否允许列出目录文件。
    /// </summary>
    public bool directory_browsing { get; set; }

    /// <summary>
    ///     MIME 类型映射表。
    /// </summary>
    public IDictionary<string, string> mime_types { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".htm"] = "text/html; charset=utf-8",
            [".css"] = "text/css; charset=utf-8",
            [".js"] = "application/javascript; charset=utf-8",
            [".json"] = "application/json",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".svg"] = "image/svg+xml",
            [".ico"] = "image/x-icon",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".ttf"] = "font/ttf",
            [".txt"] = "text/plain; charset=utf-8",
            [".xml"] = "application/xml",
            [".pdf"] = "application/pdf",
            [".zip"] = "application/zip"
        };
}

/// <summary>
///     静态文件中间件——根据请求路径查找并返回对应文件。
/// </summary>
public sealed class StaticFileMiddleware : IMiddleware
{
    private readonly StaticFileOptions _options;

    /// <summary>
    ///     初始化静态文件中间件。
    /// </summary>
    /// <param name="options">静态文件选项</param>
    public StaticFileMiddleware(StaticFileOptions? options = null)
    {
        _options = options ?? new StaticFileOptions();
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        var method = context.request.method;

        if (method != HttpMethod.get && method != HttpMethod.head)
        {
            await next();
            return;
        }

        var requestPath = context.request.path.TrimStart('/');

        if (string.IsNullOrEmpty(requestPath)) requestPath = _options.default_file;

        var rootPath = Path.IsPathRooted(_options.root_path)
            ? _options.root_path
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.root_path);

        var filePath = Path.GetFullPath(Path.Combine(rootPath, requestPath));

        if (!filePath.StartsWith(Path.GetFullPath(rootPath), StringComparison.OrdinalIgnoreCase))
        {
            context.response.status_code = HttpStatusCode.forbidden;
            return;
        }

        if (!File.Exists(filePath))
        {
            await next();
            return;
        }

        var extension = Path.GetExtension(filePath);

        if (_options.mime_types.TryGetValue(extension, out var contentType))
            context.response.headers["Content-Type"] = contentType;

        context.response.status_code = HttpStatusCode.ok;

        var fileInfo = new FileInfo(filePath);
        context.response.headers["Content-Length"] = fileInfo.Length.ToString();
        context.response.headers["Cache-Control"] = "public, max-age=3600";
        context.response.headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");

        if (method == HttpMethod.get)
        {
            var content = await File.ReadAllBytesAsync(filePath);
            context.response.body = content;
        }
    }
}