using System.Text.Json;
using Core.Net.HealthCheck;
using Std.Net.Http;

namespace Std.Net.HealthCheck;

/// <summary>
///     健康检查中间件，拦截 /health/liveness 和 /health/readiness 端点并返回健康报告。
///     实现 <see cref="IMiddleware" /> 接口，在 Sonic.Standard 中间件管线中执行。
/// </summary>
public sealed class HealthCheckMiddleware : IMiddleware
{
    private static readonly JsonSerializerOptions _json_options = new() { WriteIndented = false };
    private readonly IHealthCheckService _health_check_service;

    /// <summary>
    ///     初始化健康检查中间件。
    /// </summary>
    /// <param name="healthCheckService">健康检查服务实例。</param>
    public HealthCheckMiddleware(IHealthCheckService healthCheckService)
    {
        _health_check_service = healthCheckService;
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        var path = context.request.path.ToLowerInvariant();

        if (path == "/health/liveness")
        {
            var results = await _health_check_service.CheckLivenessAsync();
            write_health_response(context, results, HealthProbeKind.liveness);
        }
        else if (path == "/health/readiness")
        {
            var results = await _health_check_service.CheckReadinessAsync();
            write_health_response(context, results, HealthProbeKind.readiness);
        }
        else if (path == "/health")
        {
            var livenessResults = await _health_check_service.CheckLivenessAsync();
            var readinessResults = await _health_check_service.CheckReadinessAsync();
            var allResults = livenessResults.Concat(readinessResults).ToList();
            write_health_response(context, allResults, null);
        }
        else
        {
            await next();
        }
    }

    private static void write_health_response(
        RouteContext context,
        IReadOnlyList<HealthCheckResult> results,
        HealthProbeKind? probeKind)
    {
        var healthStatus = get_aggregate_status(results);
        var statusCode = healthStatus switch
        {
            HealthStatus.Healthy => HttpStatusCode.ok,
            HealthStatus.Degraded => HttpStatusCode.ok,
            HealthStatus.Unhealthy => HttpStatusCode.service_unavailable,
            _ => HttpStatusCode.internal_server_error
        };

        var response = new HealthReport
        {
            status = healthStatus.ToString(),
            probe_kind = probeKind?.ToString() ?? "All",
            timestamp = DateTimeOffset.UtcNow,
            total_checks = results.Count,
            checks =
            [
                .. results.Select(r => new HealthCheckEntry
                {
                    name = r.Description ?? "未知",
                    status = r.Status.ToString(),
                    description = r.Description,
                    data = r.Data?.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "null")
                })
            ]
        };

        var json = JsonSerializer.Serialize(response, _json_options);
        context.response.status_code = statusCode;
        context.response.json(json);
    }

    private static HealthStatus get_aggregate_status(IReadOnlyList<HealthCheckResult> results)
    {
        if (results.Count == 0) return HealthStatus.Healthy;

        if (results.Any(r => r.Status == HealthStatus.Unhealthy)) return HealthStatus.Unhealthy;

        if (results.Any(r => r.Status == HealthStatus.Degraded)) return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }

    private sealed class HealthReport
    {
        public string status { get; set; } = string.Empty;
        public string probe_kind { get; set; } = string.Empty;
        public DateTimeOffset timestamp { get; set; }
        public int total_checks { get; set; }
        public List<HealthCheckEntry> checks { get; set; } = [];
    }

    private sealed class HealthCheckEntry
    {
        public string name { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string? description { get; set; }
        public Dictionary<string, string?>? data { get; set; }
    }
}