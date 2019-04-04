using System.Diagnostics;
using Core.Database;
using Std.Database.Core;

namespace Std.Database.Cache;

/// <summary>
///     LightDB KV 缓存层，基于 IDatabase 提供 TTL 过期 + LRU 淘汰 + Write-Through/Write-Back 模式
/// </summary>
public sealed class DatabaseCache : IDatabaseCache, IAsyncDisposable
{
    #region 构造函数

    /// <summary>
    ///     创建 DatabaseCache
    /// </summary>
    /// <param name="database">底层 LightDB 数据库</param>
    /// <param name="options">缓存配置（可选）</param>
    internal DatabaseCache(LightDb database, LightCacheOptions? options = null)
    {
        _database = database;
        _options = options ?? LightCacheOptions.@default;
        _cache = new Dictionary<string, LruNode>(_options.max_entries);
        _write_back_queue = new(_options.write_back_batch_size);

        _expiration_timer = new Timer(
            _ => scan_expired(),
            null,
            TimeSpan.FromSeconds(_options.expiration_scan_interval_seconds),
            TimeSpan.FromSeconds(_options.expiration_scan_interval_seconds));

        if (_options.mode == CacheMode.WriteBack)
            _write_back_timer = new Timer(
                async _ => await flush_write_back(),
                null,
                _options.write_back_flush_interval_ms,
                _options.write_back_flush_interval_ms);
    }

    #endregion

    #region 释放

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        await _expiration_timer.DisposeAsync();

        if (_write_back_timer is not null) await _write_back_timer.DisposeAsync();

        if (_options.mode == CacheMode.WriteBack && _write_back_queue.Count > 0) await flush_write_back();

        _lock.Dispose();
    }

    #endregion

    #region 统计

    /// <summary>
    ///     获取缓存统计
    /// </summary>
    public DatabaseCacheStatistics get_statistics()
    {
        return new DatabaseCacheStatistics
        {
            entry_count = _cache.Count,
            hit_count = Interlocked.Read(ref _hit_count),
            miss_count = Interlocked.Read(ref _miss_count),
            eviction_count = Interlocked.Read(ref _eviction_count),
            expiration_count = Interlocked.Read(ref _expiration_count)
        };
    }

    #endregion

    #region TTL 过期扫描

    private void scan_expired()
    {
        try
        {
            _lock.Wait();
            try
            {
                var expiredKeys = new List<string>();

                foreach (var kv in _cache)
                    if (kv.Value.is_expired)
                        expiredKeys.Add(kv.Key);

                foreach (var key in expiredKeys)
                    if (_cache.TryGetValue(key, out var node))
                    {
                        remove_node(node);
                        _cache.Remove(key);
                        Interlocked.Increment(ref _expiration_count);
                    }
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseCache] 过期扫描异常: {ex}");
        }
    }

    #endregion

    #region LRU 节点

    private sealed class LruNode
    {
        public readonly DatabaseKey key;
        public DateTime expires_at;
        public LruNode? next;
        public LruNode? prev;
        public object? value;

        public LruNode(DatabaseKey key)
        {
            this.key = key;
            expires_at = DateTime.MaxValue;
        }

        public bool is_expired => DateTime.UtcNow > expires_at;
    }

    #endregion

    #region 字段

    private readonly LightDb _database;
    private readonly LightCacheOptions _options;
    private readonly Dictionary<string, LruNode> _cache;
    private LruNode? _head;
    private LruNode? _tail;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Timer _expiration_timer;
    private readonly Timer? _write_back_timer;
    private readonly List<(DatabaseKey Key, object Value)> _write_back_queue;
    private long _hit_count;
    private long _miss_count;
    private long _eviction_count;
    private long _expiration_count;
    private bool _disposed;

    #endregion

    #region IDatabaseCache 实现

    /// <inheritdoc />
    public async ValueTask<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key,
        CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        var result = await get_internal<byte[]>(dbKey, cancellationToken);
        return result is null ? null : new ReadOnlyMemory<byte>(result);
    }

    /// <inheritdoc />
    public async ValueTask PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value, int ttlSeconds = 0,
        CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        await put_internal(dbKey, value.ToArray(), ttlSeconds);

        if (_options.mode == CacheMode.WriteThrough)
        {
            await _database.put(dbKey, value.ToArray(), cancellationToken);
        }
        else
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _write_back_queue.Add((dbKey, value.ToArray()));
                if (_write_back_queue.Count >= _options.write_back_batch_size) await flush_write_back_locked();
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask RemoveAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var keyStr = dbKey.ToString();
            if (_cache.TryGetValue(keyStr, out var node))
            {
                remove_node(node);
                _cache.Remove(keyStr);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> ContainsAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var keyStr = dbKey.ToString();
            if (_cache.TryGetValue(keyStr, out var node))
            {
                if (node.is_expired)
                {
                    remove_node(node);
                    _cache.Remove(keyStr);
                    Interlocked.Increment(ref _expiration_count);
                    return false;
                }

                return true;
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 类型安全 API

    /// <summary>
    ///     读取缓存（类型安全版本，内部 API）
    /// </summary>
    internal async ValueTask<TValue?> get<TValue>(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        return await get_internal<TValue>(key, cancellationToken);
    }

    /// <summary>
    ///     写入缓存（类型安全版本，内部 API）
    /// </summary>
    internal async ValueTask put<TValue>(DatabaseKey key, TValue value, int ttlSeconds = 0,
        CancellationToken cancellationToken = default)
    {
        await put_internal(key, value, ttlSeconds);

        if (_options.mode == CacheMode.WriteThrough)
        {
            await _database.put(key, value, cancellationToken);
        }
        else
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _write_back_queue.Add((key, value!));
                if (_write_back_queue.Count >= _options.write_back_batch_size) await flush_write_back_locked();
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    ///     删除缓存条目（类型安全版本，内部 API）
    /// </summary>
    internal async ValueTask remove(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var keyStr = key.ToString();
            if (_cache.TryGetValue(keyStr, out var node))
            {
                remove_node(node);
                _cache.Remove(keyStr);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     判断键是否存在且未过期（类型安全版本，内部 API）
    /// </summary>
    internal async ValueTask<bool> contains(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var keyStr = key.ToString();
            if (_cache.TryGetValue(keyStr, out var node))
            {
                if (node.is_expired)
                {
                    remove_node(node);
                    _cache.Remove(keyStr);
                    Interlocked.Increment(ref _expiration_count);
                    return false;
                }

                return true;
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 内部方法

    private async ValueTask<TValue?> get_internal<TValue>(DatabaseKey key, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var keyStr = key.ToString();

            if (_cache.TryGetValue(keyStr, out var node))
            {
                if (node.is_expired)
                {
                    remove_node(node);
                    _cache.Remove(keyStr);
                    Interlocked.Increment(ref _expiration_count);
                    Interlocked.Increment(ref _miss_count);
                    return default;
                }

                move_to_head(node);
                Interlocked.Increment(ref _hit_count);
                return node.value is TValue tv ? tv : default;
            }

            Interlocked.Increment(ref _miss_count);
        }
        finally
        {
            _lock.Release();
        }

        var dbValue = await _database.get<TValue>(key, cancellationToken);

        if (dbValue is not null) await put_internal(key, dbValue, _options.default_ttl_seconds);

        return dbValue;
    }

    private async ValueTask put_internal<TValue>(DatabaseKey key, TValue value, int ttlSeconds)
    {
        var effectiveTtl = ttlSeconds > 0 ? ttlSeconds : _options.default_ttl_seconds;
        var expiresAt = effectiveTtl > 0
            ? DateTime.UtcNow.AddSeconds(effectiveTtl)
            : DateTime.MaxValue;

        await _lock.WaitAsync();
        try
        {
            var keyStr = key.ToString();

            if (_cache.TryGetValue(keyStr, out var existing))
            {
                existing.value = value;
                existing.expires_at = expiresAt;
                move_to_head(existing);
            }
            else
            {
                if (_cache.Count >= _options.max_entries) evict_lru();

                var node = new LruNode(key)
                {
                    value = value,
                    expires_at = expiresAt
                };

                _cache[keyStr] = node;
                add_to_head(node);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region LRU 双向链表

    private void add_to_head(LruNode node)
    {
        node.next = _head;
        node.prev = null;

        _head?.prev = node;

        _head = node;

        _tail ??= node;
    }

    private void remove_node(LruNode node)
    {
        if (node.prev is not null)
            node.prev.next = node.next;
        else
            _head = node.next;

        if (node.next is not null)
            node.next.prev = node.prev;
        else
            _tail = node.prev;
    }

    private void move_to_head(LruNode node)
    {
        if (node == _head) return;

        remove_node(node);
        add_to_head(node);
    }

    private void evict_lru()
    {
        var lru = _tail;
        if (lru is null) return;

        remove_node(lru);
        _cache.Remove(lru.key.ToString());
        Interlocked.Increment(ref _eviction_count);
    }

    #endregion

    #region Write-Back 异步刷盘

    private async ValueTask flush_write_back()
    {
        await _lock.WaitAsync();
        try
        {
            await flush_write_back_locked();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask flush_write_back_locked()
    {
        if (_write_back_queue.Count == 0) return;

        for (var i = _write_back_queue.Count - 1; i >= 0; i--)
        {
            var (key, value) = _write_back_queue[i];
            try
            {
                await _database.put(key, value);
                _write_back_queue.RemoveAt(i);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseCache] Write-Back 刷盘失败: key={key}, ex={ex}");
            }
        }
    }

    #endregion
}