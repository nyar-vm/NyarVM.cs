using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Std.Net.Http;

namespace Std.Net.Hosting;

/// <summary>
///     Net 应用主机，管理应用的完整生命周期（启动、运行、优雅停机）。
///     使用 Sonic.Standard 的 <see cref="HttpServer" /> 作为 HTTP 服务器，不依赖 ValhallaServer。
/// </summary>
public sealed class NetHost : IAsyncDisposable
{
    private readonly List<IHostedService> _hosted_services = [];
    private readonly ServiceProvider _service_provider;
    private readonly CancellationTokenSource _shutdown_cts = new();
    private volatile bool _is_shutting_down;
    private volatile bool _is_started;

    /// <summary>
    ///     初始化 Net 应用主机。
    /// </summary>
    /// <param name="serviceProvider">依赖注入服务提供者。</param>
    /// <param name="configuration">应用配置根。</param>
    internal NetHost(ServiceProvider serviceProvider, IConfigurationRoot configuration)
    {
        _service_provider = serviceProvider;
        this.configuration = configuration;
    }

    /// <summary>
    ///     应用是否正在关闭。
    /// </summary>
    public bool is_shutting_down => _is_shutting_down;

    /// <summary>
    ///     应用是否已启动。
    /// </summary>
    public bool is_started => _is_started;

    /// <summary>
    ///     获取主机提供的服务。
    /// </summary>
    public IServiceProvider services => _service_provider;

    /// <summary>
    ///     获取应用配置。
    /// </summary>
    public IConfigurationRoot configuration { get; }

    /// <summary>
    ///     释放主机资源。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_is_shutting_down) await stop();

        _shutdown_cts.Dispose();
        await _service_provider.DisposeAsync();
    }

    /// <summary>
    ///     启动 Net 应用主机。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task start(CancellationToken cancellationToken = default)
    {
        if (_is_started) return;

        await start_hosted_services(cancellationToken);
        _is_started = true;
    }

    /// <summary>
    ///     优雅关闭应用。
    /// </summary>
    /// <param name="timeout">最大等待时间。</param>
    public async Task stop(TimeSpan? timeout = null)
    {
        if (_is_shutting_down) return;

        _is_shutting_down = true;
        await _shutdown_cts.CancelAsync();

        timeout ??= TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout.Value);

        try
        {
            await stop_hosted_services(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    ///     启动应用并阻塞直到收到关闭信号。
    ///     支持 Ctrl+C 和进程退出信号的优雅停机。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task run(CancellationToken cancellationToken = default)
    {
        await start(cancellationToken);

        System.Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        try
        {
            var tcs = new TaskCompletionSource<bool>();
            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(_shutdown_cts.Token, cancellationToken);
            using var registration = linkedCts.Token.Register(() => tcs.TrySetResult(true));
            await tcs.Task;
        }
        finally
        {
            System.Console.CancelKeyPress -= OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }

        await stop();
    }

    private async Task start_hosted_services(CancellationToken cancellationToken)
    {
        var hostedServices = _service_provider.GetServices<IHostedService>();
        foreach (var service in hostedServices)
        {
            _hosted_services.Add(service);
            await service.start(cancellationToken);
        }
    }

    private async Task stop_hosted_services(CancellationToken cancellationToken)
    {
        for (var i = _hosted_services.Count - 1; i >= 0; i--) await _hosted_services[i].stop(cancellationToken);
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _shutdown_cts.Cancel();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _shutdown_cts.Cancel();
    }
}