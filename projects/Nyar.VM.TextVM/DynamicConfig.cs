namespace Nyar.VM.TextVM;

/// <summary>
/// 动态查询的投机编译配置。
/// </summary>
public class DynamicConfig
{
    /// <summary>
    /// 后台编译超时毫秒数。
    /// </summary>
    public Int32 SpeculativeTimeoutMs { get; set; } = 30;

    /// <summary>
    /// 用户停止输入后开始全量编译的延迟毫秒数。
    /// </summary>
    public Int32 IdleCompilationDelayMs { get; set; } = 200;

    /// <summary>
    /// 最大 LRU 缓存条目数。
    /// </summary>
    public Int32 LruCacheSize { get; set; } = 32;

    /// <summary>
    /// 是否启用增量编译。
    /// </summary>
    public Boolean EnableIncrementalDiff { get; set; } = true;

    /// <summary>
    /// 动态场景下回溯步数上限。
    /// </summary>
    public Int64 MaxBacktrackSteps { get; set; } = 10000;

    /// <summary>
    /// 同步降级策略。
    /// </summary>
    public SyncFallbackStrategy SyncFallback { get; set; } = SyncFallbackStrategy.TwoWayBM;
}

/// <summary>
/// 同步降级策略枚举。
/// </summary>
public enum SyncFallbackStrategy
{
    /// <summary>Knuth-Morris-Pratt 算法，适合短模式。</summary>
    KMP,

    /// <summary>双向 BM，适合长模式。</summary>
    TwoWayBM,

    /// <summary>暴力逐字符。</summary>
    Naive,

    /// <summary>直接报错。</summary>
    Error,
}
