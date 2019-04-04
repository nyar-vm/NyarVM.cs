using System.Collections.Concurrent;
using System.Text.Json;

namespace Hermes.Cache.Memory;

public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly Timer _cleanupTimer;
    private readonly CacheOptions _options;

    public MemoryCacheService(CacheOptions options)
    {
        _options = options;
        _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var fullKey = BuildKey(key);

        if (_cache.TryGetValue(fullKey, out var item))
        {
            if (item.ExpiresAt.HasValue && item.ExpiresAt < DateTime.UtcNow)
            {
                _cache.TryRemove(fullKey, out _);
                return Task.FromResult<T?>(default);
            }

            item.LastAccessed = DateTime.UtcNow;
            return Task.FromResult(Deserialize<T>(item.Value));
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        var fullKey = BuildKey(key);
        var expiresAt = expire.HasValue ? DateTime.UtcNow.Add(expire.Value) : (DateTime?)null;

        _cache[fullKey] = new CacheItem
        {
            Value = Serialize(value),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow
        };

        EnforceMemoryLimit();
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(_cache.TryRemove(BuildKey(key), out _));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var fullKey = BuildKey(key);

        if (_cache.TryGetValue(fullKey, out var item))
        {
            if (item.ExpiresAt.HasValue && item.ExpiresAt < DateTime.UtcNow)
            {
                _cache.TryRemove(fullKey, out _);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    private void CleanupExpiredItems(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var pair in _cache.Where(p => p.Value.ExpiresAt.HasValue && p.Value.ExpiresAt < now).ToList())
            _cache.TryRemove(pair.Key, out _);
    }

    private void EnforceMemoryLimit()
    {
        if (_cache.Count > _options.MaxItems)
        {
            var oldestItems = _cache.OrderBy(kv => kv.Value.LastAccessed)
                .Take(_cache.Count - _options.MaxItems / 2)
                .ToList();

            foreach (var item in oldestItems) _cache.TryRemove(item.Key, out _);
        }
    }

    private string BuildKey(string key)
    {
        return $"{_options.KeyPrefix}{key}";
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    private static T? Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value);
    }

    private sealed class CacheItem
    {
        public string Value { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}