namespace Std.Database.Core;

/// <summary>
///     LightDB 统计信息
/// </summary>
public sealed class DatabaseStatistics
{
    /// <summary>
    ///     总条目数
    /// </summary>
    public long total_entries
    {
        get => Volatile.Read(ref _total_entries);
        set => Volatile.Write(ref _total_entries, value);
    }

    /// <summary>
    ///     总读取次数
    /// </summary>
    public long read_count
    {
        get => Volatile.Read(ref _read_count);
        set => Volatile.Write(ref _read_count, value);
    }

    /// <summary>
    ///     总写入次数
    /// </summary>
    public long write_count
    {
        get => Volatile.Read(ref _write_count);
        set => Volatile.Write(ref _write_count, value);
    }

    /// <summary>
    ///     总删除次数
    /// </summary>
    public long delete_count
    {
        get => Volatile.Read(ref _delete_count);
        set => Volatile.Write(ref _delete_count, value);
    }

    /// <summary>
    ///     缓存命中次数
    /// </summary>
    public long cache_hits
    {
        get => Volatile.Read(ref _cache_hits);
        set => Volatile.Write(ref _cache_hits, value);
    }

    /// <summary>
    ///     缓存未命中次数
    /// </summary>
    public long cache_misses
    {
        get => Volatile.Read(ref _cache_misses);
        set => Volatile.Write(ref _cache_misses, value);
    }

    /// <summary>
    ///     缓存命中率
    /// </summary>
    public double cache_hit_rate =>
        cache_hits + cache_misses > 0
            ? (double)cache_hits / (cache_hits + cache_misses)
            : 0;

    /// <summary>
    ///     当前活跃事务数
    /// </summary>
    public int active_transactions { get; set; }

    /// <summary>
    ///     数据库大小（字节）
    /// </summary>
    public long database_size { get; set; }

    /// <summary>
    ///     WAL 大小（字节）
    /// </summary>
    public long wal_size { get; set; }

    #region 方法

    /// <summary>
    ///     递增读取次数
    /// </summary>
    public void increment_read_count()
    {
        Interlocked.Increment(ref _read_count);
    }

    /// <summary>
    ///     递增写入次数
    /// </summary>
    public void increment_write_count()
    {
        Interlocked.Increment(ref _write_count);
    }

    /// <summary>
    ///     递增删除次数
    /// </summary>
    public void increment_delete_count()
    {
        Interlocked.Increment(ref _delete_count);
    }

    #endregion

    #region 字段

    private long _total_entries;
    private long _read_count;
    private long _write_count;
    private long _delete_count;
    private long _cache_hits;
    private long _cache_misses;

    #endregion
}