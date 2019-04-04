namespace Olympus.Athena.Cache;

#region CacheEntry 缓存条目

/// <summary>
///     缓存条目，封装缓存键、值、元数据和生命周期信息
/// </summary>
public sealed class CacheEntry : IDisposable
{
    #region 构造函数

    /// <summary>
    ///     创建缓存条目
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值，可为任意对象</param>
    /// <param name="sizeBytes">估算的占用字节数</param>
    /// <param name="ttl">可选的生存时间</param>
    public CacheEntry(string key, object value, long sizeBytes, TimeSpan? ttl = null)
    {
        Key = key;
        Value = value;
        SizeBytes = sizeBytes;
        Ttl = ttl;
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
        AccessCount = 0L;
        IsFromDisk = false;
    }

    #endregion

    #region IDisposable

    /// <summary>
    ///     释放资源，如果 <see cref="Value" /> 实现了 <see cref="IDisposable" /> 则调用其 <see cref="IDisposable.Dispose" /> 方法
    /// </summary>
    public void Dispose()
    {
        if (Value is IDisposable disposable) disposable.Dispose();
    }

    #endregion

    #region 属性

    /// <summary>
    ///     缓存键
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     缓存值，可缓存任何对象（MicroPartition / 索引节点 / 图邻居等）
    /// </summary>
    public object Value { get; }

    /// <summary>
    ///     估算的占用字节数
    /// </summary>
    public long SizeBytes { get; }

    /// <summary>
    ///     创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    ///     最后访问时间（UTC），每次访问时更新
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    ///     累计访问次数
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    ///     可选的生存时间，为空时永不超时
    /// </summary>
    public TimeSpan? Ttl { get; }

    /// <summary>
    ///     是否已过期，当 <see cref="Ttl" /> 不为空且创建时间 + TTL 早于当前 UTC 时间时返回 <c>true</c>
    /// </summary>
    public bool IsExpired
    {
        get
        {
            if (Ttl is null) return false;

            return CreatedAt + Ttl.Value < DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     是否来自磁盘缓存
    /// </summary>
    public bool IsFromDisk { get; set; }

    #endregion
}

#endregion