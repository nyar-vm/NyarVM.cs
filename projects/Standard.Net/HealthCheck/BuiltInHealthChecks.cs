using Core.Net.HealthCheck;

namespace Std.Net.HealthCheck;

/// <summary>
///     存活健康检查——始终返回健康，表示进程仍在运行。
/// </summary>
public sealed class LivenessCheck : IHealthCheck
{
    /// <inheritdoc />
    public string Name => "Liveness";

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("应用进程存活"));
    }
}

/// <summary>
///     内存健康检查——基于内存使用率判定应用是否健康。
/// </summary>
public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly double _threshold_bytes;

    /// <summary>
    ///     初始化内存健康检查。
    /// </summary>
    /// <param name="maxMemoryBytes">最大内存阈值（字节），超出此值返回降级。</param>
    public MemoryHealthCheck(long maxMemoryBytes = 500 * 1024 * 1024)
    {
        _threshold_bytes = maxMemoryBytes;
    }

    /// <inheritdoc />
    public string Name => "Memory";

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var usedMemory = GC.GetTotalMemory(false);
        var data = new Dictionary<string, object?>
        {
            ["UsedMemoryBytes"] = usedMemory,
            ["MaxMemoryBytes"] = _threshold_bytes,
            ["MemoryUsagePercent"] = $"{usedMemory / _threshold_bytes * 100:F1}%"
        };

        if (usedMemory > _threshold_bytes)
            return Task.FromResult(HealthCheckResult.Degraded(
                $"内存使用量 {usedMemory / 1024 / 1024}MB 超过阈值 {_threshold_bytes / 1024 / 1024}MB"));

        return Task.FromResult(HealthCheckResult.Healthy(
            $"内存使用量 {usedMemory / 1024 / 1024}MB 正常"));
    }
}

/// <summary>
///     自定义委托健康检查——通过委托函数执行健康检查逻辑。
/// </summary>
public sealed class DelegateHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, Task<HealthCheckResult>> _check_func;

    /// <summary>
    ///     初始化自定义委托健康检查。
    /// </summary>
    /// <param name="name">检查名称。</param>
    /// <param name="checkFunc">检查委托函数。</param>
    public DelegateHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> checkFunc)
    {
        Name = name;
        _check_func = checkFunc;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return _check_func(cancellationToken);
    }
}