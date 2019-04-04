using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Net.HealthCheck;

/// <summary>
///     健康检查结果状态。
/// </summary>
public enum HealthStatus
{
    /// <summary>健康</summary>
    Healthy,

    /// <summary>降级（部分功能不可用）</summary>
    Degraded,

    /// <summary>不健康</summary>
    Unhealthy
}

/// <summary>
///     单个健康检查的结果。
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    ///     健康状态。
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    ///     描述信息。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     异常信息（仅在 Status 为 Unhealthy 时有效）。
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    ///     扩展数据。
    /// </summary>
    public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    ///     创建一个健康的结果。
    /// </summary>
    public static HealthCheckResult Healthy(string? description = null)
    {
        return new HealthCheckResult { Status = HealthStatus.Healthy, Description = description };
    }

    /// <summary>
    ///     创建一个降级的结果。
    /// </summary>
    public static HealthCheckResult Degraded(string description)
    {
        return new HealthCheckResult { Status = HealthStatus.Degraded, Description = description };
    }

    /// <summary>
    ///     创建一个不健康的结果。
    /// </summary>
    public static HealthCheckResult Unhealthy(string description, Exception? exception = null)
    {
        return new HealthCheckResult
            { Status = HealthStatus.Unhealthy, Description = description, Exception = exception };
    }
}

/// <summary>
///     健康检查接口，每个实现代表一个健康检查探针。
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    ///     健康检查的名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     执行健康检查。
    /// </summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     健康检查服务，聚合所有已注册的 IHealthCheck。
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    ///     运行所有 Liveness 探针。
    /// </summary>
    Task<IReadOnlyList<HealthCheckResult>> CheckLivenessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     运行所有 Readiness 探针。
    /// </summary>
    Task<IReadOnlyList<HealthCheckResult>> CheckReadinessAsync(CancellationToken cancellationToken = default);
}