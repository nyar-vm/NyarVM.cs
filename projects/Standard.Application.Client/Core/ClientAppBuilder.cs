namespace Std.App.Client;

/// <summary>
///     客户端应用构建器，提供流式 API 注册服务、配置和中间件
/// </summary>
public sealed class ClientAppBuilder
{
    private readonly List<Action<IClientApp>> _lifecycleHooks = [];
    private readonly IServiceCollection _services;
    private IHostShell? _hostShell;
    private Func<IClientApp, Task>? _onStart;
    private Func<IClientApp, Task>? _onStop;

    /// <summary>
    ///     初始化应用构建器
    /// </summary>
    public ClientAppBuilder()
    {
        _services = new ServiceCollection();
        _services.AddLogging(builder => builder.AddConsole());
    }

    /// <summary>
    ///     获取服务集合，用于注册依赖注入服务
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    ///     设置宿主 Shell，提供原生平台能力
    /// </summary>
    /// <param name="hostShell">宿主 Shell 实例</param>
    public ClientAppBuilder UseHostShell(IHostShell hostShell)
    {
        _hostShell = hostShell;
        return this;
    }

    /// <summary>
    ///     注册应用启动时的回调
    /// </summary>
    /// <param name="onStart">启动回调</param>
    public ClientAppBuilder OnStart(Func<IClientApp, Task> onStart)
    {
        _onStart = onStart;
        return this;
    }

    /// <summary>
    ///     注册应用停止时的回调
    /// </summary>
    /// <param name="onStop">停止回调</param>
    public ClientAppBuilder OnStop(Func<IClientApp, Task> onStop)
    {
        _onStop = onStop;
        return this;
    }

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期回调，参数为当前应用实例</param>
    public ClientAppBuilder AddLifecycleHook(Action<IClientApp> hook)
    {
        _lifecycleHooks.Add(hook);
        return this;
    }

    /// <summary>
    ///     构建客户端应用实例
    /// </summary>
    public ClientApp Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<ClientApp>>()
                     ?? NullLogger<ClientApp>.Instance;

        return new ClientApp(
            serviceProvider,
            _hostShell,
            _onStart,
            _onStop,
            _lifecycleHooks,
            logger);
    }
}