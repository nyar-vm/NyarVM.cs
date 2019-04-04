using Core.Database;

namespace Std.Database.Cache;

/// <summary>
///     KV 缓存配置
/// </summary>
public sealed class LightCacheOptions
{
    /// <summary>
    ///     最大缓存条目数，超出后触发 LRU 淘汰
    /// </summary>
    public int max_entries { get; set; } = 10000;

    /// <summary>
    ///     默认 TTL（秒），0 表示永不过期
    /// </summary>
    public int default_ttl_seconds { get; set; } = 0;

    /// <summary>
    ///     缓存写入模式
    /// </summary>
    public CacheMode mode { get; set; } = CacheMode.WriteThrough;

    /// <summary>
    ///     Write-Back 模式下异步刷盘间隔（毫秒）
    /// </summary>
    public int write_back_flush_interval_ms { get; set; } = 1000;

    /// <summary>
    ///     Write-Back 模式下积压批次大小，达到后强制刷盘
    /// </summary>
    public int write_back_batch_size { get; set; } = 100;

    /// <summary>
    ///     TTL 过期清理间隔（秒）
    /// </summary>
    public int expiration_scan_interval_seconds { get; set; } = 30;

    /// <summary>
    ///     默认配置
    /// </summary>
    public static LightCacheOptions @default => new();
}