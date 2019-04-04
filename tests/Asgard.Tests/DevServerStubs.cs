using System.Diagnostics;

namespace VOA.ToolChain.Tests;

/// <summary>
///     HMR 时序信息（测试桩）
/// </summary>
public sealed class HmrTiming
{
    /// <summary>
    ///     文件变更时间
    /// </summary>
    public DateTime FileChangedAt { get; init; }

    /// <summary>
    ///     编译开始时间
    /// </summary>
    public DateTime CompileStartAt { get; init; }

    /// <summary>
    ///     编译结束时间
    /// </summary>
    public DateTime CompileEndAt { get; init; }

    /// <summary>
    ///     广播时间
    /// </summary>
    public DateTime BroadcastAt { get; init; }

    /// <summary>
    ///     变更的文件路径
    /// </summary>
    public string FilePath { get; init; } = "";

    /// <summary>
    ///     检测延迟（毫秒）
    /// </summary>
    public double DetectMs => (CompileStartAt - FileChangedAt).TotalMilliseconds;

    /// <summary>
    ///     编译耗时（毫秒）
    /// </summary>
    public double CompileMs => (CompileEndAt - CompileStartAt).TotalMilliseconds;

    /// <summary>
    ///     总延迟（毫秒）
    /// </summary>
    public double TotalMs => (BroadcastAt - FileChangedAt).TotalMilliseconds;
}

/// <summary>
///     健康报告（测试桩）
/// </summary>
public sealed class VoaHealthReport
{
    /// <summary>
    ///     总请求数
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    ///     总错误数
    /// </summary>
    public long TotalErrors { get; init; }

    /// <summary>
    ///     错误率
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    ///     运行时间格式化字符串
    /// </summary>
    public string UptimeFormatted { get; init; } = "";

    /// <summary>
    ///     是否健康
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    ///     峰值连接数
    /// </summary>
    public int PeakConnections { get; init; }
}

/// <summary>
///     监控快照（测试桩）
/// </summary>
public sealed class VoaMonitorSnapshot
{
    /// <summary>
    ///     累计请求数
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    ///     内存字节数
    /// </summary>
    public long MemoryBytes { get; init; }
}

/// <summary>
///     开发服务器监控器（测试桩）
/// </summary>
public sealed class VoaDevServerMonitor
{
    private readonly TimeSpan _interval;
    private readonly object _lock = new();
    private readonly List<VoaMonitorSnapshot> _snapshots = [];
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private int _activeConnections;
    private int _peakConnections;
    private long _totalErrors;
    private long _totalRequests;

    /// <summary>
    ///     创建监控器实例
    /// </summary>
    /// <param name="interval">快照采集间隔</param>
    public VoaDevServerMonitor(TimeSpan interval)
    {
        _interval = interval;
    }

    /// <summary>
    ///     当前活跃连接数
    /// </summary>
    public int ActiveConnections => _activeConnections;

    /// <summary>
    ///     峰值连接数
    /// </summary>
    public int PeakConnections => _peakConnections;

    /// <summary>
    ///     记录请求
    /// </summary>
    /// <param name="bytes">请求大小（字节）</param>
    public void RecordRequest(int bytes)
    {
        lock (_lock)
        {
            _totalRequests++;
            _snapshots.Add(new VoaMonitorSnapshot
            {
                TotalRequests = _totalRequests,
                MemoryBytes = GC.GetTotalMemory(false)
            });
        }
    }

    /// <summary>
    ///     记录错误
    /// </summary>
    public void RecordError()
    {
        Interlocked.Increment(ref _totalErrors);
    }

    /// <summary>
    ///     记录连接打开
    /// </summary>
    public void RecordConnectionOpen()
    {
        var current = Interlocked.Increment(ref _activeConnections);
        var peak = _peakConnections;
        while (current > peak)
        {
            var prev = Interlocked.CompareExchange(ref _peakConnections, current, peak);
            if (prev == peak) break;

            peak = prev;
        }
    }

    /// <summary>
    ///     记录连接关闭
    /// </summary>
    public void RecordConnectionClose()
    {
        Interlocked.Decrement(ref _activeConnections);
    }

    /// <summary>
    ///     获取健康报告
    /// </summary>
    /// <returns>健康报告</returns>
    public VoaHealthReport GetHealthReport()
    {
        var total = Interlocked.Read(ref _totalRequests);
        var errors = Interlocked.Read(ref _totalErrors);
        var rate = total > 0 ? (double)errors / total : 0.0;
        var elapsed = _uptime.Elapsed;

        return new VoaHealthReport
        {
            TotalRequests = total,
            TotalErrors = errors,
            ErrorRate = rate,
            UptimeFormatted = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}",
            IsHealthy = rate < 0.05,
            PeakConnections = _peakConnections
        };
    }

    /// <summary>
    ///     获取最近的监控快照
    /// </summary>
    /// <param name="count">最大快照数量</param>
    /// <returns>快照列表</returns>
    public List<VoaMonitorSnapshot> GetRecentSnapshots(int count)
    {
        lock (_lock)
        {
            return _snapshots.Count <= count
                ? [.. _snapshots]
                : _snapshots.GetRange(_snapshots.Count - count, count);
        }
    }
}