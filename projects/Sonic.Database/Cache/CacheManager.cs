using System.Text;
using System.Text.Json;

namespace Olympus.Athena.Cache;

#region CacheManager 双模缓存管理器

/// <summary>
///     双模缓存管理器，提供内存热数据缓存和磁盘冷数据缓存两级存储，支持多种驱逐策略和自动晋升/降级
/// </summary>
public sealed class CacheManager : IDisposable
{
    #region 常量

    private const int PromotionThreshold = 5;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建双模缓存管理器实例
    /// </summary>
    /// <param name="diskCachePath">磁盘缓存目录路径</param>
    /// <param name="maxMemoryBytes">内存缓存上限字节数，默认 256MB</param>
    public CacheManager(string diskCachePath, long maxMemoryBytes = 256L * 1024 * 1024)
    {
        _memoryCache = new Dictionary<string, CacheEntry>();
        _diskCachePath = diskCachePath;
        _lock = new object();
        _memoryUsageBytes = 0L;
        _evictionPolicy = new LruPolicy();
        _maxMemoryBytes = maxMemoryBytes;
        _disposed = false;

        if (!Directory.Exists(_diskCachePath)) Directory.CreateDirectory(_diskCachePath);
    }

    #endregion

    #region IDisposable

    /// <summary>
    ///     释放缓存管理器占用的所有资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Clear();
    }

    #endregion

    #region 序列化模型

    internal sealed class DiskCacheRecord
    {
        public string Key { get; set; } = string.Empty;

        public string ValueJson { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public DateTime CreatedAt { get; set; }

        public long? TtlTicks { get; set; }
    }

    #endregion

    #region 内部状态

    private readonly Dictionary<string, CacheEntry> _memoryCache;
    private readonly string _diskCachePath;
    private readonly object _lock;
    private long _memoryUsageBytes;
    private IEvictionPolicy _evictionPolicy;
    private long _maxMemoryBytes;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    ///     内存缓存上限字节数
    /// </summary>
    public long MaxMemoryBytes
    {
        get
        {
            lock (_lock)
            {
                return _maxMemoryBytes;
            }
        }
        set
        {
            lock (_lock)
            {
                _maxMemoryBytes = value;
            }
        }
    }

    /// <summary>
    ///     驱逐策略，默认为 <see cref="LruPolicy" />
    /// </summary>
    public IEvictionPolicy EvictionPolicy
    {
        get
        {
            lock (_lock)
            {
                return _evictionPolicy;
            }
        }
        set
        {
            lock (_lock)
            {
                _evictionPolicy = value;
            }
        }
    }

    /// <summary>
    ///     当前内存缓存使用量（字节）
    /// </summary>
    public long MemoryUsageBytes
    {
        get
        {
            lock (_lock)
            {
                return _memoryUsageBytes;
            }
        }
    }

    /// <summary>
    ///     内存缓存中条目数量
    /// </summary>
    public int MemoryCount
    {
        get
        {
            lock (_lock)
            {
                return _memoryCache.Count;
            }
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     将值存入缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值，可为任意对象</param>
    /// <param name="sizeBytes">估算的占用字节数</param>
    /// <param name="ttl">可选的生存时间</param>
    public void Put(string key, object value, long sizeBytes, TimeSpan? ttl = null)
    {
        lock (_lock)
        {
            var entry = new CacheEntry(key, value, sizeBytes, ttl);
            _evictionPolicy.OnAdd(entry);

            if (_memoryUsageBytes + sizeBytes > _maxMemoryBytes)
            {
                var bytesToFree = _memoryUsageBytes + sizeBytes - _maxMemoryBytes;
                EvictInternal(bytesToFree);
            }

            _memoryCache[key] = entry;
            _memoryUsageBytes += sizeBytes;
        }
    }

    /// <summary>
    ///     从缓存中获取值，先查内存缓存再查磁盘缓存
    /// </summary>
    /// <typeparam name="T">期望的值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值，未找到时返回 <c>null</c></returns>
    public T? Get<T>(string key) where T : class
    {
        lock (_lock)
        {
            var cached = GetFromMemory<T>(key);
            if (cached is not null) return cached;

            return LoadFromDisk<T>(key);
        }
    }

    /// <summary>
    ///     从缓存中移除条目
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>移除成功返回 <c>true</c>，条目不存在返回 <c>false</c></returns>
    public bool Remove(string key)
    {
        lock (_lock)
        {
            if (_memoryCache.TryGetValue(key, out var entry))
            {
                _memoryUsageBytes -= entry.SizeBytes;
                _memoryCache.Remove(key);
                entry.Dispose();
                return true;
            }

            return TryRemoveDiskFile(key);
        }
    }

    /// <summary>
    ///     驱逐缓存条目直到释放指定字节数
    /// </summary>
    /// <param name="bytesToFree">需要释放的字节数</param>
    public void Evict(long bytesToFree)
    {
        lock (_lock)
        {
            EvictInternal(bytesToFree);
        }
    }

    /// <summary>
    ///     清空所有内存缓存，保留磁盘缓存
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var entry in _memoryCache.Values) entry.Dispose();

            _memoryCache.Clear();
            _memoryUsageBytes = 0L;
        }
    }

    /// <summary>
    ///     清空所有缓存，包括磁盘缓存
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            Clear();
            ClearDiskCache();
        }
    }

    #endregion

    #region 内存缓存操作

    private T? GetFromMemory<T>(string key) where T : class
    {
        if (_memoryCache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _memoryUsageBytes -= entry.SizeBytes;
                _memoryCache.Remove(key);
                entry.Dispose();
                return null;
            }

            entry.LastAccessedAt = DateTime.UtcNow;
            entry.AccessCount++;
            _evictionPolicy.OnAccess(entry);

            if (entry.Value is T typed) return typed;
        }

        return null;
    }

    private void EvictInternal(long bytesToFree)
    {
        var freed = 0L;
        var entries = _memoryCache.Values.ToList();

        while (freed < bytesToFree && _memoryCache.Count > 0)
        {
            var victim = _evictionPolicy.SelectVictim(entries);
            if (victim is null) break;

            WriteToDisk(victim);
            _memoryUsageBytes -= victim.SizeBytes;
            _memoryCache.Remove(victim.Key);
            entries.Remove(victim);
            freed += victim.SizeBytes;
        }
    }

    #endregion

    #region 磁盘缓存操作

    private string GetDiskFilePath(string key)
    {
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(_diskCachePath, $"{hash}.cache");
    }

    private void WriteToDisk(CacheEntry entry)
    {
        var filePath = GetDiskFilePath(entry.Key);
        var record = new DiskCacheRecord
        {
            Key = entry.Key,
            ValueJson = JsonSerializer.Serialize(entry.Value, entry.Value.GetType()),
            SizeBytes = entry.SizeBytes,
            CreatedAt = entry.CreatedAt,
            TtlTicks = entry.Ttl?.Ticks
        };

        var json = JsonSerializer.Serialize(record);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    private T? LoadFromDisk<T>(string key) where T : class
    {
        var filePath = GetDiskFilePath(key);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var record = JsonSerializer.Deserialize<DiskCacheRecord>(json);
            if (record is null) return null;

            var value = JsonSerializer.Deserialize<T>(record.ValueJson);
            if (value is null) return null;

            TimeSpan? ttl = null;
            if (record.TtlTicks.HasValue) ttl = TimeSpan.FromTicks(record.TtlTicks.Value);

            var entry = new CacheEntry(key, value, record.SizeBytes, ttl);
            entry.IsFromDisk = true;

            if (entry.AccessCount >= PromotionThreshold) PromoteToMemory(entry);

            return value;
        }
        catch
        {
            return null;
        }
    }

    private void PromoteToMemory(CacheEntry entry)
    {
        if (_memoryUsageBytes + entry.SizeBytes > _maxMemoryBytes)
        {
            var bytesToFree = _memoryUsageBytes + entry.SizeBytes - _maxMemoryBytes;
            EvictInternal(bytesToFree);
        }

        entry.IsFromDisk = false;
        _evictionPolicy.OnAdd(entry);
        _memoryCache[entry.Key] = entry;
        _memoryUsageBytes += entry.SizeBytes;

        var filePath = GetDiskFilePath(entry.Key);
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private bool TryRemoveDiskFile(string key)
    {
        var filePath = GetDiskFilePath(key);
        if (!File.Exists(filePath)) return false;

        File.Delete(filePath);
        return true;
    }

    private void ClearDiskCache()
    {
        try
        {
            if (Directory.Exists(_diskCachePath))
                foreach (var file in Directory.GetFiles(_diskCachePath, "*.cache"))
                    File.Delete(file);
        }
        catch
        {
        }
    }

    #endregion
}

#endregion