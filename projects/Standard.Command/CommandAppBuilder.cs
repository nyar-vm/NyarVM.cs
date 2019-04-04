using Core.Command;
using Microsoft.Extensions.DependencyInjection;
using Std.Command.Hosting;
using Std.Command.Middleware;

namespace Std.Command;

/// <summary>
///     Command 应用构建器，支持链式配置
/// </summary>
public sealed class CommandAppBuilder
{
    /// <summary>
    ///     设置应用描述
    /// </summary>
    /// <param name="description">应用描述</param>
    /// <returns>当前构建器</returns>
    public CommandAppBuilder with_description(string description)
    {
        CommandApp.with_description(description);
        return this;
    }

    /// <summary>
    ///     设置应用版本号
    /// </summary>
    /// <param name="version">版本号</param>
    /// <returns>当前构建器</returns>
    public CommandAppBuilder with_version(string version)
    {
        CommandApp.with_version(version);
        return this;
    }

    /// <summary>
    ///     注册中间件
    /// </summary>
    /// <typeparam name="T">中间件类型</typeparam>
    /// <returns>当前构建器</returns>
    public CommandAppBuilder use_middleware<T>() where T : ICommandMiddleware, new()
    {
        CommandApp.use_middleware<T>();
        return this;
    }

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子</param>
    /// <returns>当前构建器</returns>
    public CommandAppBuilder use_lifecycle_hook(ILifecycleHook hook)
    {
        CommandApp.use_lifecycle_hook(hook);
        return this;
    }

    /// <summary>
    ///     配置应用配置源（JSON 文件、环境变量等）
    /// </summary>
    /// <param name="configure">配置构建委托</param>
    /// <returns>当前构建器</returns>
    public CommandAppBuilder configure_app_configuration(Action<CommandConfiguration> configure)
    {
        var config = new CommandConfiguration();
        configure(config);
        CommandApp.set_configuration(config);
        return this;
    }

    /// <summary>
    ///     配置依赖注入服务
    /// </summary>
    /// <param name="configure">服务注册委托</param>
    /// <returns>当前构建器</returns>
    public CommandAppBuilder configure_services(Action<IServiceCollection> configure)
    {
        var collection = new ServiceCollection();
        configure(collection);
        CommandApp.set_services(collection.BuildServiceProvider());
        return this;
    }
}