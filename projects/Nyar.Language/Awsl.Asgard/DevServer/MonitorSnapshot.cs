namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     监控快照数据点
/// </summary>
public readonly struct MonitorSnapshot
{
    /// <summary>快照时间</summary>
    public DateTime timestamp { get; init; }

    /// <summary>累计请求数</summary>
    public long total_requests { get; init; }

    /// <summary>累计错误数</summary>
    public long total_errors { get; init; }

    /// <summary>当前活跃连接数</summary>
    public long active_connections { get; init; }

    /// <summary>当前内存占用（字节）</summary>
    public long memory_bytes { get; init; }

    /// <summary>线程池线程数</summary>
    public int thread_count { get; init; }
}
