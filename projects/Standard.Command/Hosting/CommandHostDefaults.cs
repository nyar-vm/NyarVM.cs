using Std.Command.Middleware;

namespace Std.Command.Hosting;

/// <summary>
///     Command 宿主默认配置快捷方式
/// </summary>
public static class CommandHostDefaults
{
    /// <summary>
    ///     创建开箱即用的 CLI 宿主构建器，含默认中间件和程序集命令扫描
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <param name="args">命令行参数</param>
    /// <returns>预配置的宿主构建器</returns>
    public static CommandHostBuilder create_cli(string appName, string[]? args = null)
    {
        var builder = new CommandHostBuilder();

        builder.use_middleware<ErrorHandlingMiddleware>();
        builder.use_middleware<LoggingMiddleware>();

        return builder;
    }

    /// <summary>
    ///     创建预配置的空宿主构建器
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <returns>宿主构建器</returns>
    public static CommandHostBuilder create(string appName)
    {
        return new CommandHostBuilder()
            .use_middleware<ErrorHandlingMiddleware>()
            .use_middleware<LoggingMiddleware>();
    }
}