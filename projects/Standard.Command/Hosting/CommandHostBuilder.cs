using System.Globalization;
using Core.Command;
using Core.Console;
using Microsoft.Extensions.DependencyInjection;
using Std.Command.Middleware;
using Std.Command.Ports;

namespace Std.Command.Hosting;

/// <summary>
///     Command 泛用宿主构建器，集中管理本地化、DI 服务、配置、中间件、命令注册和 Shell/I/O 端口
/// </summary>
public sealed class CommandHostBuilder
{
    private readonly CommandConfiguration _configuration = new();
    private readonly List<ILifecycleHook> _lifecycle_hooks = [];
    private readonly MiddlewarePipeline _pipeline = new();
    private readonly CommandRegistry _registry = new();
    private readonly IServiceCollection _service_collection = new ServiceCollection();
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private IServiceProvider? _external_services;
    private Func<IInputReader>? _input_factory;
    private ILocalizer _localizer = new ResxLocalizer();
    private Func<IOutputWriter>? _output_factory;
    private Func<CommandRegistry, MiddlewarePipeline, IInteractiveShell>? _shell_factory;

    /// <summary>
    ///     配置本地化器
    /// </summary>
    /// <typeparam name="T">实现了 ILocalizer 的类型</typeparam>
    public CommandHostBuilder use_localizer<T>() where T : ILocalizer, new()
    {
        _localizer = new T();
        return this;
    }

    /// <summary>
    ///     设置当前文化
    /// </summary>
    /// <param name="culture">目标文化</param>
    public CommandHostBuilder use_culture(CultureInfo culture)
    {
        _culture = culture;
        return this;
    }

    /// <summary>
    ///     注册 DI 服务
    /// </summary>
    /// <param name="configure">服务配置委托</param>
    public CommandHostBuilder configure_services(Action<IServiceCollection> configure)
    {
        configure(_service_collection);
        return this;
    }

    /// <summary>
    ///     使用外部 IServiceProvider（如 Microsoft.Extensions.DependencyInjection 构建的容器）
    /// </summary>
    /// <param name="services">外部服务提供者</param>
    public CommandHostBuilder use_external_services(IServiceProvider services)
    {
        _external_services = services;
        return this;
    }

    /// <summary>
    ///     配置系统
    /// </summary>
    /// <param name="configure">配置委托</param>
    public CommandHostBuilder use_configuration(Action<CommandConfiguration> configure)
    {
        configure(_configuration);
        return this;
    }

    /// <summary>
    ///     注册命令
    /// </summary>
    /// <typeparam name="T">实现了 ICommand 的类型</typeparam>
    /// <param name="name">命令名称</param>
    public CommandHostBuilder use_command<T>(string name) where T : ICommand
    {
        _registry.register<T>(name);
        return this;
    }

    /// <summary>
    ///     注册中间件
    /// </summary>
    /// <typeparam name="T">实现了 ICommandMiddleware 的类型</typeparam>
    public CommandHostBuilder use_middleware<T>() where T : ICommandMiddleware, new()
    {
        _pipeline.use<T>();
        return this;
    }

    /// <summary>
    ///     注册中间件实例
    /// </summary>
    /// <param name="middleware">中间件实例</param>
    public CommandHostBuilder use_middleware(ICommandMiddleware middleware)
    {
        _pipeline.use(middleware);
        return this;
    }

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    public CommandHostBuilder use_lifecycle_hook(ILifecycleHook hook)
    {
        _lifecycle_hooks.Add(hook);
        return this;
    }

    /// <summary>
    ///     注入 Shell 宿主实现，工厂可访问命令注册表、中间件管道和服务提供者
    /// </summary>
    /// <param name="factory">Shell 工厂委托</param>
    public CommandHostBuilder use_shell(Func<CommandRegistry, MiddlewarePipeline, IInteractiveShell> factory)
    {
        _shell_factory = factory;
        return this;
    }

    /// <summary>
    ///     注入输出端口实现
    /// </summary>
    /// <param name="factory">输出工厂委托</param>
    public CommandHostBuilder use_output(Func<IOutputWriter> factory)
    {
        _output_factory = factory;
        return this;
    }

    /// <summary>
    ///     注入输入端口实现
    /// </summary>
    /// <param name="factory">输入工厂委托</param>
    public CommandHostBuilder use_input(Func<IInputReader> factory)
    {
        _input_factory = factory;
        return this;
    }

    /// <summary>
    ///     构建宿主
    /// </summary>
    /// <returns>Command 宿主</returns>
    public CommandHost build()
    {
        Localizer.current = _localizer;
        CultureInfo.CurrentUICulture = _culture;

        var services = _external_services ?? _service_collection.BuildServiceProvider();
        var shell = _shell_factory?.Invoke(_registry, _pipeline);
        var output = _output_factory?.Invoke();
        var input = _input_factory?.Invoke();

        return new CommandHost(_registry, _pipeline, services, _lifecycle_hooks, shell, output, input);
    }
}