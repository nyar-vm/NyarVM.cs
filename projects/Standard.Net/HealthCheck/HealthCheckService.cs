using Core.Net.HealthCheck;

namespace Std.Net.HealthCheck;

/// <summary>
///     健康检查服务实现，管理所有健康检查探针并区分 Liveness 和 Readiness 检查。
///     实现 <see cref="IHealthCheckService" /> 接口。
/// </summary>
public sealed class HealthCheckService : IHealthCheckService
{
    private readonly List<HealthCheckRegistration> _registrations = [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<HealthCheckResult>> CheckLivenessAsync(
        CancellationToken cancellationToken = default)
    {
        return await execute_checks(HealthProbeKind.liveness, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HealthCheckResult>> CheckReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        return await execute_checks(HealthProbeKind.readiness, cancellationToken);
    }

    /// <summary>
    ///     注册一个健康检查探针。
    /// </summary>
    /// <param name="check">健康检查实例。</param>
    /// <param name="kind">探针类型（Liveness 或 Readiness）。</param>
    public void register(IHealthCheck check, HealthProbeKind kind = HealthProbeKind.liveness)
    {
        _registrations.Add(new HealthCheckRegistration(check, kind));
    }

    /// <summary>
    ///     获取所有已注册的健康检查信息。
    /// </summary>
    public IReadOnlyList<(string Name, HealthProbeKind Kind)> get_registered_checks()
    {
        return [.. _registrations.Select(r => (r.check.Name, Kind: r.kind))];
    }

    private async Task<IReadOnlyList<HealthCheckResult>> execute_checks(
        HealthProbeKind kind,
        CancellationToken cancellationToken)
    {
        var tasks = _registrations
            .Where(r => r.kind == kind)
            .Select(r => execute_check(r.check, cancellationToken));

        var results = await Task.WhenAll(tasks);
        return results;
    }

    private static async Task<HealthCheckResult> execute_check(
        IHealthCheck check,
        CancellationToken cancellationToken)
    {
        try
        {
            return await check.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"健康检查 '{check.Name}' 执行时发生异常", ex);
        }
    }

    private sealed class HealthCheckRegistration
    {
        public HealthCheckRegistration(IHealthCheck check, HealthProbeKind kind)
        {
            this.check = check;
            this.kind = kind;
        }

        public IHealthCheck check { get; }
        public HealthProbeKind kind { get; }
    }
}

/// <summary>
///     健康检查探针类型。
/// </summary>
public enum HealthProbeKind
{
    /// <summary>存活探针（应用是否还活着）</summary>
    liveness,

    /// <summary>就绪探针（应用是否准备好接收请求）</summary>
    readiness
}