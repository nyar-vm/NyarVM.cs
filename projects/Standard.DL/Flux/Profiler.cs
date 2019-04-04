namespace Std.DL.Flux;

/// <summary>
///     训练性能分析器 —— 测量训练吞吐量、延迟、内存使用
/// </summary>
public sealed class Profiler
{
    private readonly List<ProfileEntry> _entries;
    private readonly long _startTimestamp;

    /// <summary>
    ///     创建性能分析器
    /// </summary>
    public Profiler()
    {
        _entries = [];
        TotalTokens = 0;
        TotalSteps = 0;
        _startTimestamp = Environment.TickCount64;
    }

    /// <summary>
    ///     总处理 token 数
    /// </summary>
    public long TotalTokens { get; private set; }

    /// <summary>
    ///     总训练步数
    /// </summary>
    public long TotalSteps { get; private set; }

    /// <summary>
    ///     分析条目列表
    /// </summary>
    public IReadOnlyList<ProfileEntry> Entries => _entries;

    /// <summary>
    ///     记录一个训练步骤的性能数据
    /// </summary>
    /// <param name="step">步数</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    /// <param name="loss">损失值</param>
    /// <param name="elapsedMs">本步耗时（毫秒）</param>
    public void RecordStep(int step, int batchSize, int seqLen, float loss, long elapsedMs)
    {
        var tokensThisStep = (long)batchSize * seqLen;
        TotalTokens += tokensThisStep;
        TotalSteps++;

        var throughput = elapsedMs > 0 ? tokensThisStep * 1000.0 / elapsedMs : 0;

        _entries.Add(new ProfileEntry(
            step,
            batchSize,
            seqLen,
            tokensThisStep,
            loss,
            elapsedMs,
            throughput));
    }

    /// <summary>
    ///     获取平均吞吐量（tokens/sec）
    /// </summary>
    /// <returns>平均吞吐量</returns>
    public double AverageThroughput()
    {
        var elapsed = Environment.TickCount64 - _startTimestamp;
        if (elapsed <= 0 || TotalTokens <= 0) return 0;

        return TotalTokens * 1000.0 / elapsed;
    }

    /// <summary>
    ///     获取平均步延迟（ms）
    /// </summary>
    /// <returns>平均延迟</returns>
    public double AverageStepLatency()
    {
        if (_entries.Count == 0) return 0;

        var totalMs = 0.0;
        for (var i = 0; i < _entries.Count; i++) totalMs += _entries[i].ElapsedMs;
        return totalMs / _entries.Count;
    }

    /// <summary>
    ///     获取峰值内存使用量（字节）
    /// </summary>
    /// <returns>峰值内存</returns>
    public long PeakMemoryBytes()
    {
        return GC.GetGCMemoryInfo().HeapSizeBytes;
    }

    /// <summary>
    ///     生成性能摘要
    /// </summary>
    /// <returns>摘要字符串</returns>
    public string Summary()
    {
        var avgThroughput = AverageThroughput();
        var avgLatency = AverageStepLatency();
        var peakMem = PeakMemoryBytes();
        var elapsed = (Environment.TickCount64 - _startTimestamp) / 1000.0;

        return $"[Profiler] 总步数: {TotalSteps}, 总 token: {TotalTokens}, " +
               $"平均吞吐: {avgThroughput:F1} tokens/s, 平均延迟: {avgLatency:F1} ms/step, " +
               $"峰值内存: {peakMem / 1024.0 / 1024.0:F1} MB, 总耗时: {elapsed:F1} s";
    }

    /// <summary>
    ///     重置分析器
    /// </summary>
    public void Reset()
    {
        _entries.Clear();
        TotalTokens = 0;
        TotalSteps = 0;
    }
}

/// <summary>
///     性能分析条目
/// </summary>
public sealed class ProfileEntry
{
    /// <summary>
    ///     创建性能分析条目
    /// </summary>
    /// <param name="Step">步数</param>
    /// <param name="BatchSize">批次大小</param>
    /// <param name="SeqLen">序列长度</param>
    /// <param name="Tokens">token 数</param>
    /// <param name="Loss">损失值</param>
    /// <param name="ElapsedMs">耗时</param>
    /// <param name="ThroughputTokensPerSec">吞吐量</param>
    public ProfileEntry(int Step, int BatchSize, int SeqLen, long Tokens, float Loss, long ElapsedMs,
        double ThroughputTokensPerSec)
    {
        this.Step = Step;
        this.BatchSize = BatchSize;
        this.SeqLen = SeqLen;
        this.Tokens = Tokens;
        this.Loss = Loss;
        this.ElapsedMs = ElapsedMs;
        this.ThroughputTokensPerSec = ThroughputTokensPerSec;
    }

    /// <summary>
    ///     步数
    /// </summary>
    public int Step { get; }

    /// <summary>
    ///     批次大小
    /// </summary>
    public int BatchSize { get; }

    /// <summary>
    ///     序列长度
    /// </summary>
    public int SeqLen { get; }

    /// <summary>
    ///     本步 token 数
    /// </summary>
    public long Tokens { get; }

    /// <summary>
    ///     损失值
    /// </summary>
    public float Loss { get; }

    /// <summary>
    ///     本步耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; }

    /// <summary>
    ///     本步吞吐量（tokens/sec）
    /// </summary>
    public double ThroughputTokensPerSec { get; }
}