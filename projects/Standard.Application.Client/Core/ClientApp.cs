namespace Std.App.Client;

/// <summary>
///     客户端应用的默认实现，管理生命周期并协调各子系统
/// </summary>
public sealed class ClientApp : IClientApp, IDisposable
{
    private readonly CancellationTokenSource _appCts = new();
    private readonly List<Action<IClientApp>> _lifecycleHooks;
    private readonly ILogger<ClientApp> _logger;
    private readonly Func<IClientApp, Task>? _onStart;
    private readonly Func<IClientApp, Task>? _onStop;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     初始化客户端应用实例
    /// </summary>
    internal ClientApp(
        IServiceProvider serviceProvider,
        IHostShell? hostShell,
        Func<IClientApp, Task>? onStart,
        Func<IClientApp, Task>? onStop,
        List<Action<IClientApp>> lifecycleHooks,
        ILogger<ClientApp> logger)
    {
        _serviceProvider = serviceProvider;
        HostShell = hostShell;
        _onStart = onStart;
        _onStop = onStop;
        _lifecycleHooks = lifecycleHooks;
        _logger = logger;
    }

    /// <summary>
    ///     依赖注入服务提供者
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    /// <summary>
    ///     宿主 Shell（可能为 null，若无宿主绑定）
    /// </summary>
    public IHostShell? HostShell { get; }

    /// <inheritdoc />
    public AppLifecycle Lifecycle { get; private set; } = AppLifecycle.Idle;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Lifecycle is AppLifecycle.Running or AppLifecycle.Starting) return;

        Lifecycle = AppLifecycle.Starting;
        _logger.LogInformation("客户端应用正在启动...");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _appCts.Token);

        foreach (var hook in _lifecycleHooks) hook(this);

        if (_onStart != null) await _onStart(this);

        Lifecycle = AppLifecycle.Running;
        _logger.LogInformation("客户端应用已启动");
    }

    /// <inheritdoc />
    public Task PauseAsync()
    {
        if (Lifecycle != AppLifecycle.Running) return Task.CompletedTask;

        Lifecycle = AppLifecycle.Paused;
        _logger.LogInformation("客户端应用已暂停");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResumeAsync()
    {
        if (Lifecycle != AppLifecycle.Paused) return Task.CompletedTask;

        Lifecycle = AppLifecycle.Resuming;
        _logger.LogInformation("客户端应用正在恢复...");
        Lifecycle = AppLifecycle.Running;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Lifecycle is AppLifecycle.Stopping or AppLifecycle.Stopped) return;

        Lifecycle = AppLifecycle.Stopping;
        _logger.LogInformation("客户端应用正在停止...");

        await _appCts.CancelAsync();

        if (_onStop != null) await _onStop(this);

        Lifecycle = AppLifecycle.Stopped;
        _logger.LogInformation("客户端应用已停止");
    }

    /// <summary>
    ///     获取指定类型的服务
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _appCts.Dispose();
    }
}