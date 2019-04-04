namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     DevServer 性能监视器
///     跟踪内存使用、请求吞吐量、连接数等关键健康指标
/// </summary>
public sealed class VoaDevServerMonitor : IDisposable
{
    private readonly object _lock = new();
    private readonly Timer _snapshot_timer;
    private readonly List<MonitorSnapshot> _snapshots = [];
    private readonly DateTime _start_time;
    private long _active_connections;
    private long _peak_connections;
    private long _total_bytes_sent;
    private long _total_errors;
    private long _total_requests;

    public VoaDevServerMonitor(TimeSpan snapshotInterval)
    {
        _start_time = DateTime.UtcNow;
        _snapshot_timer = new Timer(_ => take_snapshot(), null, snapshotInterval, snapshotInterval);
    }

    /// <summary>
    ///     运行时长
    /// </summary>
    public TimeSpan uptime => DateTime.UtcNow - _start_time;

    /// <summary>
    ///     总请求数
    /// </summary>
    public long total_requests => Interlocked.Read(ref _total_requests);

    /// <summary>
    ///     总错误数
    /// </summary>
    public long total_errors => Interlocked.Read(ref _total_errors);

    /// <summary>
    ///     总发送字节数
    /// </summary>
    public long total_bytes_sent => Interlocked.Read(ref _total_bytes_sent);

    /// <summary>
    ///     当前活跃连接数
    /// </summary>
    public long active_connections => Interlocked.Read(ref _active_connections);

    /// <summary>
    ///     峰值连接数
    /// </summary>
    public long peak_connections => Interlocked.Read(ref _peak_connections);

    /// <summary>
    ///     错误率（0-1）
    /// </summary>
    public double error_rate => total_requests > 0 ? (double)total_errors / total_requests : 0;

    public void Dispose()
    {
        _snapshot_timer.Dispose();
    }

    /// <summary>
    ///     记录一个请求
    /// </summary>
    public void record_request(long bytesSent)
    {
        Interlocked.Increment(ref _total_requests);
        Interlocked.Add(ref _total_bytes_sent, bytesSent);
    }

    /// <summary>
    ///     记录一个错误
    /// </summary>
    public void record_error()
    {
        Interlocked.Increment(ref _total_errors);
    }

    /// <summary>
    ///     记录连接建立
    /// </summary>
    public void record_connection_open()
    {
        var current = Interlocked.Increment(ref _active_connections);
        var peak = Interlocked.Read(ref _peak_connections);
        while (current > peak)
        {
            Interlocked.CompareExchange(ref _peak_connections, current, peak);
            peak = Interlocked.Read(ref _peak_connections);
        }
    }

    /// <summary>
    ///     记录连接关闭
    /// </summary>
    public void record_connection_close()
    {
        Interlocked.Decrement(ref _active_connections);
    }

    /// <summary>
    ///     获取最近的快照列表
    /// </summary>
    public IReadOnlyList<MonitorSnapshot> get_recent_snapshots(int count = 60)
    {
        lock (_lock)
        {
            var start = Math.Max(0, _snapshots.Count - count);
            return _snapshots.GetRange(start, _snapshots.Count - start).AsReadOnly();
        }
    }

    /// <summary>
    ///     获取健康报告
    /// </summary>
    public HealthReport get_health_report()
    {
        var uptime = this.uptime;

        return new HealthReport
        {
            uptime = uptime,
            uptime_formatted = format_uptime(uptime),
            total_requests = total_requests,
            total_errors = total_errors,
            error_rate = error_rate,
            active_connections = active_connections,
            peak_connections = peak_connections,
            current_memory_bytes = get_current_memory_bytes(),
            average_requests_per_minute = uptime.TotalMinutes > 0 ? total_requests / uptime.TotalMinutes : 0,
            is_healthy = error_rate < 0.05 && active_connections < 500
        };
    }

    private void take_snapshot()
    {
        var snapshot = new MonitorSnapshot
        {
            timestamp = DateTime.UtcNow,
            total_requests = total_requests,
            total_errors = total_errors,
            active_connections = active_connections,
            memory_bytes = get_current_memory_bytes(),
            thread_count = ThreadPool.ThreadCount
        };

        lock (_lock)
        {
            _snapshots.Add(snapshot);
            if (_snapshots.Count > 1440)
            {
                _snapshots.RemoveRange(0, _snapshots.Count - 1440);
            }
        }
    }

    private static long get_current_memory_bytes()
    {
        try
        {
            return GC.GetTotalMemory(false);
        }
        catch
        {
            return 0;
        }
    }

    private static string format_uptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        }

        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }
}
