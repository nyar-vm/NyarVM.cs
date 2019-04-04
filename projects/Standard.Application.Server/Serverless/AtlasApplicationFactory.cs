using Std.App.Server.Core;

namespace Std.App.Server.Serverless;

/// <summary>
///     Atlas 应用工厂，用于创建预配置的 AtlasHost 实例
/// </summary>
public static class AtlasApplicationFactory
{
    /// <summary>
    ///     创建默认配置的 AtlasHost，端口随机，包含核心中间件
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>已构建的 AtlasHost 实例</returns>
    public static AtlasHost create_host(string[]? args = null)
    {
        var host = AtlasApp.builder();
        host.use_port(0);
        host.use_middleware(new ExceptionHandlingMiddleware());
        host.build();
        return host;
    }
}