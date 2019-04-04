using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Std.App.Server.Core;

/// <summary>
///     Atlas 应用宿主构建器，配置路由器、中间件、服务注册和控制器扫描
/// </summary>
public sealed class AtlasHost
{
    private readonly List<Assembly> _controller_assemblies = [];
    private readonly MiddlewarePipeline _pipeline = new();
    private bool _built;
    private IServiceProvider? _services;

    /// <summary>
    ///     获取已构建的路由器
    /// </summary>
    public Router router { get; } = new();

    /// <summary>
    ///     获取已构建的中间件管线
    /// </summary>
    public MiddlewarePipeline pipeline => _pipeline;

    /// <summary>
    ///     获取已配置的监听端口
    /// </summary>
    public int port { get; private set; } = 8080;

    /// <summary>
    ///     获取已配置的监听主机
    /// </summary>
    public string host { get; private set; } = "127.0.0.1";

    /// <summary>
    ///     设置监听端口
    /// </summary>
    /// <param name="port">端口号，默认 8080</param>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_port(int port)
    {
        this.port = port;
        return this;
    }

    /// <summary>
    ///     设置监听地址
    /// </summary>
    /// <param name="host">主机地址，默认 127.0.0.1</param>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_host(string host)
    {
        this.host = host;
        return this;
    }

    /// <summary>
    ///     注册中间件实例
    /// </summary>
    /// <param name="middleware">中间件实例</param>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_middleware(IMiddleware middleware)
    {
        _pipeline.use(middleware);
        return this;
    }

    /// <summary>
    ///     注册控制器所在的程序集，用于扫描特性标记
    /// </summary>
    /// <param name="assembly">包含控制器的程序集</param>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_controllers(Assembly assembly)
    {
        _controller_assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    ///     注册控制器所在的程序集（泛型版本）
    /// </summary>
    /// <typeparam name="T">程序集中的任意类型</typeparam>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_controllers<T>()
    {
        return use_controllers(typeof(T).Assembly);
    }

    /// <summary>
    ///     设置服务提供者
    /// </summary>
    /// <param name="services">服务提供者</param>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_services(IServiceProvider services)
    {
        _services = services;
        return this;
    }

    /// <summary>
    ///     设置服务集合（自动构建 ServiceProvider）
    /// </summary>
    /// <param name="configure">服务注册委托</param>
    /// <returns>当前构建器实例</returns>
    public AtlasHost use_services(Action<ServiceCollection> configure)
    {
        var collection = new ServiceCollection();
        configure(collection);
        _services = collection.BuildServiceProvider();
        return this;
    }

    /// <summary>
    ///     构建 AtlasHost，扫描控制器并注册路由，创建 NetApp
    /// </summary>
    /// <returns>已配置的 NetApp 以启动服务器</returns>
    public NetApp build()
    {
        if (_built) return new NetApp(router, _pipeline);

        _built = true;

        if (_services is null) _services = new ServiceCollection().BuildServiceProvider();

        foreach (var assembly in _controller_assemblies)
            ControllerScanner.scan_and_register(router, assembly, _services);

        return new NetApp(router, _pipeline);
    }
}