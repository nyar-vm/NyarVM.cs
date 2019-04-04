using System.Collections.Concurrent;

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     VOA Fetch 内存缓存 — TTL 过期 + 标签反向索引 + 容量限制，仅供 DevServer/SSR 使用
/// </summary>
public sealed class VoaFetchCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly int _max_entries;
    private readonly ConcurrentDictionary<string, HashSet<string>> _tag_index = new();
    private long _evictions;
    private long _hits;
    private long _misses;

    public VoaFetchCache(int maxEntries = 1000)
    {
        _max_entries = maxEntries;
    }

    public int count => _entries.Count;
    public long hits => _hits;
    public long misses => _misses;
    public long evictions => _evictions;

    public bool try_get(string cacheKey, out FetchResponse response)
    {
        if (_entries.TryGetValue(cacheKey, out var entry))
        {
            if (entry.expires_at > DateTime.UtcNow)
            {
                Interlocked.Increment(ref _hits);
                response = entry.response;
                return true;
            }

            _entries.TryRemove(cacheKey, out _);
            remove_from_tag_index(cacheKey, entry.tags);
        }

        Interlocked.Increment(ref _misses);
        response = default!;
        return false;
    }

    public void set(string cacheKey, FetchResponse response, int ttlSeconds, List<string>? tags = null)
    {
        while (_entries.Count >= _max_entries)
        {
            var oldest = _entries.OrderBy(kvp => kvp.Value.created_at).FirstOrDefault();
            if (oldest.Key is null)
            {
                break;
            }

            if (_entries.TryRemove(oldest.Key, out var oldEntry))
            {
                remove_from_tag_index(oldest.Key, oldEntry.tags);
                Interlocked.Increment(ref _evictions);
            }
        }

        var entry = new CacheEntry
        {
            response = response,
            created_at = DateTime.UtcNow,
            expires_at = DateTime.UtcNow.AddSeconds(ttlSeconds),
            tags = tags ?? []
        };

        _entries[cacheKey] = entry;

        foreach (var tag in entry.tags)
        {
            _tag_index.AddOrUpdate(tag,
                _ => [cacheKey],
                (_, set) =>
                {
                    set.Add(cacheKey);
                    return set;
                });
        }
    }

    public int invalidate_by_tag(string tag)
    {
        if (!_tag_index.TryGetValue(tag, out var keys))
        {
            return 0;
        }

        var count = 0;
        foreach (var key in keys)
        {
            if (_entries.TryRemove(key, out _))
            {
                count++;
            }
        }

        _tag_index.TryRemove(tag, out _);
        return count;
    }

    public int invalidate_by_path(string pathPattern)
    {
        var count = 0;
        var keysToRemove = new List<string>();

        foreach (var kvp in _entries)
        {
            var url = kvp.Value.response.headers.GetValueOrDefault("x-request-url", "");
            if (url.Contains(pathPattern, StringComparison.OrdinalIgnoreCase))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            if (_entries.TryRemove(key, out var entry))
            {
                remove_from_tag_index(key, entry.tags);
                count++;
            }
        }

        return count;
    }

    public void clear()
    {
        _entries.Clear();
        _tag_index.Clear();
        _hits = 0;
        _misses = 0;
        _evictions = 0;
    }

    public CacheStats get_stats()
    {
        return new CacheStats
        {
            entries = _entries.Count,
            hits = _hits,
            misses = _misses,
            evictions = _evictions
        };
    }

    private void remove_from_tag_index(string cacheKey, List<string> tags)
    {
        foreach (var tag in tags)
        {
            if (_tag_index.TryGetValue(tag, out var keys))
            {
                keys.Remove(cacheKey);
                if (keys.Count == 0)
                {
                    _tag_index.TryRemove(tag, out _);
                }
            }
        }
    }

    private sealed class CacheEntry
    {
        public required FetchResponse response { get; init; }
        public DateTime created_at { get; init; }
        public DateTime expires_at { get; set; }
        public List<string> tags { get; init; } = [];
    }
}
