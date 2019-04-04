using Core.Data.Cache;

namespace Std.Data.Cache;

/// <summary>
///     基于 <see cref="Dictionary{TKey,TValue}" /> 的内存缓存实现，支持过期时间管理。
/// </summary>
/// <typeparam name="T">缓存值类型。</typeparam>
public sealed class MemoryCache<T> : IMemoryCache<T>
{
    private readonly Dictionary<string, CacheEntry> _entries = new();

    /// <summary>
    ///     获取缓存值。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <returns>缓存值，不存在或已过期时返回默认值。</returns>
    public T? get(string key)
    {
        if (!_entries.TryGetValue(key, out var entry)) return default;

        if (entry.is_expired())
        {
            _entries.Remove(key);
            return default;
        }

        return entry.value;
    }

    /// <summary>
    ///     设置缓存值。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="value">缓存值。</param>
    /// <param name="expiry">过期时间，为 null 时永不过期。</param>
    public void set(string key, T value, TimeSpan? expiry = null)
    {
        var expiresAt = expiry.HasValue
            ? DateTime.UtcNow + expiry.Value
            : (DateTime?)null;

        _entries[key] = new CacheEntry(value, expiresAt);
    }

    /// <summary>
    ///     移除缓存值。
    /// </summary>
    /// <param name="key">缓存键。</param>
    public void remove(string key)
    {
        _entries.Remove(key);
    }

    /// <summary>
    ///     判断缓存键是否存在且未过期。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <returns>如果键存在且未过期返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool contains(string key)
    {
        if (!_entries.TryGetValue(key, out var entry)) return false;

        if (entry.is_expired())
        {
            _entries.Remove(key);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     清除所有已过期的缓存条目。
    /// </summary>
    /// <returns>被清除的条目数量。</returns>
    public int evict_expired()
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in _entries)
            if (kvp.Value.is_expired())
                keysToRemove.Add(kvp.Key);

        foreach (var key in keysToRemove) _entries.Remove(key);

        return keysToRemove.Count;
    }

    private readonly struct CacheEntry
    {
        public readonly T value;
        private readonly DateTime? _expires_at;

        public CacheEntry(T value, DateTime? expiresAt)
        {
            this.value = value;
            _expires_at = expiresAt;
        }

        public bool is_expired()
        {
            return _expires_at.HasValue && DateTime.UtcNow > _expires_at.Value;
        }
    }
}