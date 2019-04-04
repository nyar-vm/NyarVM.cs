namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     健康检查报告
/// </summary>
public readonly struct HealthReport
{
    public TimeSpan uptime { get; init; }
    public string uptime_formatted { get; init; }
    public long total_requests { get; init; }
    public long total_errors { get; init; }
    public double error_rate { get; init; }
    public long active_connections { get; init; }
    public long peak_connections { get; init; }
    public long current_memory_bytes { get; init; }
    public double average_requests_per_minute { get; init; }
    public bool is_healthy { get; init; }
}
