using System.Reflection;
using Std.App.Server.Core;

namespace Std.App.Server;

/// <summary>
///     Atlas 框架主入口点，提供链式 API 配置并启动 HTTP 服务器
/// </summary>
public static class AtlasApp
{
    /// <summary>
    ///     创建 AtlasHost 构建器并注册当前入口程序集中的控制器
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>AtlasHost 构建器</returns>
    public static AtlasHost create_default(string[] args)
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        var host = new AtlasHost();

        if (entryAssembly is not null) host.use_controllers(entryAssembly);

        var port = parse_port_from_args(args);
        host.use_port(port);

        return host;
    }

    /// <summary>
    ///     创建空的 AtlasHost 构建器
    /// </summary>
    /// <returns>AtlasHost 构建器</returns>
    public static AtlasHost builder()
    {
        return new AtlasHost();
    }

    /// <summary>
    ///     从命令行参数解析端口号
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>端口号</returns>
    private static int parse_port_from_args(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--port" && int.TryParse(args[i + 1], out var port))
                return port;

        var envPort = Environment.GetEnvironmentVariable("ATLAS_PORT");

        if (int.TryParse(envPort, out var envPortValue)) return envPortValue;

        return 8080;
    }
}