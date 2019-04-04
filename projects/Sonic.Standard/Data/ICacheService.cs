using System.Collections.Concurrent;
using System.Text.Json;

namespace Std.Data;

/// <summary>
///     缓存配置选项。
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    ///     默认过期时间（分钟）。
    /// </summary>
    public int default_expire_minutes { get; set; } = 60;

    /// <summary>
    ///     最大缓存项数量。
    /// </summary>
    public int max_items { get; set; } = 10000;

    /// <summary>
    ///     缓存键前缀。
    /// </summary>
    public string key_prefix { get; set; } = "sonic:cache:";
}

/// <summary>
///     缓存服务接口，提供键值对缓存能力。
/// </summary>
public interface ICacheService
{
    /// <summary>
    ///     获取缓存值。
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>缓存值，不存在返回默认值</returns>
    Task<T?> get<T>(string key, CancellationToken ct = default);

    /// <summary>
    ///     设置缓存值。
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expire">过期时间（可选）</param>
    /// <param name="ct">取消令牌</param>
    Task set<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default);

    /// <summary>
    ///     删除缓存项。
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否成功删除</returns>
    Task<bool> delete(string key, CancellationToken ct = default);

    /// <summary>
    ///     检查缓存项是否存在。
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> exists(string key, CancellationToken ct = default);

    /// <summary>
    ///     清空所有缓存项。
    /// </summary>
    /// <param name="ct">取消令牌</param>
    Task clear(CancellationToken ct = default);
}

/// <summary>
///     内存缓存服务实现，基于 <see cref="ConcurrentDictionary{TKey,TValue}" /> 存储。
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly Timer _cleanup_timer;
    private readonly CacheOptions _options;

    /// <summary>
    ///     初始化内存缓存服务。
    /// </summary>
    /// <param name="options">缓存配置选项</param>
    public MemoryCacheService(CacheOptions options)
    {
        _options = options;
        _cleanup_timer = new Timer(cleanup_expired_items, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    ///     使用默认选项初始化内存缓存服务。
    /// </summary>
    public MemoryCacheService() : this(new CacheOptions())
    {
    }

    /// <inheritdoc />
    public Task<T?> get<T>(string key, CancellationToken ct = default)
    {
        var fullKey = build_key(key);

        if (_cache.TryGetValue(fullKey, out var item))
        {
            if (item.expires_at.HasValue && item.expires_at < DateTime.UtcNow)
            {
                _cache.TryRemove(fullKey, out _);
                return Task.FromResult<T?>(default);
            }

            item.last_accessed = DateTime.UtcNow;
            return Task.FromResult(JsonSerializer.Deserialize<T>(item.value));
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc />
    public Task set<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        var fullKey = build_key(key);
        var expiresAt = expire.HasValue ? DateTime.UtcNow.Add(expire.Value) : (DateTime?)null;

        _cache[fullKey] = new CacheItem
        {
            value = JsonSerializer.Serialize(value),
            expires_at = expiresAt,
            created_at = DateTime.UtcNow,
            last_accessed = DateTime.UtcNow
        };

        enforce_memory_limit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> delete(string key, CancellationToken ct = default)
    {
        return Task.FromResult(_cache.TryRemove(build_key(key), out _));
    }

    /// <inheritdoc />
    public Task<bool> exists(string key, CancellationToken ct = default)
    {
        var fullKey = build_key(key);

        if (_cache.TryGetValue(fullKey, out var item))
        {
            if (item.expires_at.HasValue && item.expires_at < DateTime.UtcNow)
            {
                _cache.TryRemove(fullKey, out _);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task clear(CancellationToken ct = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    private void cleanup_expired_items(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var pair in _cache.Where(p => p.Value.expires_at.HasValue && p.Value.expires_at < now).ToList())
            _cache.TryRemove(pair.Key, out _);
    }

    private void enforce_memory_limit()
    {
        if (_cache.Count > _options.max_items)
        {
            var oldestItems = _cache.OrderBy(kv => kv.Value.last_accessed)
                .Take(_cache.Count - _options.max_items / 2)
                .ToList();

            foreach (var item in oldestItems) _cache.TryRemove(item.Key, out _);
        }
    }

    private string build_key(string key)
    {
        return $"{_options.key_prefix}{key}";
    }

    private sealed class CacheItem
    {
        public string value { get; set; } = string.Empty;
        public DateTime? expires_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime last_accessed { get; set; }
    }
}