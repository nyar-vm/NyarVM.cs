using Std.App.Server.Core;

namespace Std.App.Server.DI;

/// <summary>
///     AtlasHost 扩展方法，提供 UseAtlas() 中间件注册
/// </summary>
public static class AtlasHostExtensions
{
    /// <summary>
    ///     添加 Atlas 标准中间件管线：异常处理 → 请求日志
    /// </summary>
    /// <param name="host">Atlas 宿主构建器</param>
    /// <returns>Atlas 宿主构建器（支持链式调用）</returns>
    public static AtlasHost UseAtlas(this AtlasHost host)
    {
        host.use_middleware(new ExceptionHandlingMiddleware(new ExceptionHandlingOptions
        {
            include_exception_details = true
        }));
        host.use_middleware(new RequestLoggingMiddleware());
        return host;
    }
}