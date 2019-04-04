namespace Std.Database.Cache;

/// <summary>
///     数据库缓存统计信息
/// </summary>
public sealed class DatabaseCacheStatistics
{
    /// <summary>
    ///     当前条目数
    /// </summary>
    public int entry_count { get; set; }

    /// <summary>
    ///     累计命中次数
    /// </summary>
    public long hit_count { get; set; }

    /// <summary>
    ///     累计未命中次数
    /// </summary>
    public long miss_count { get; set; }

    /// <summary>
    ///     累计淘汰次数
    /// </summary>
    public long eviction_count { get; set; }

    /// <summary>
    ///     累计过期次数
    /// </summary>
    public long expiration_count { get; set; }

    /// <summary>
    ///     命中率
    /// </summary>
    public double hit_rate => hit_count + miss_count == 0 ? 0 : (double)hit_count / (hit_count + miss_count);
}