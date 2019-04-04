namespace Nyar.Database.Query;

/// <summary>
///     查询结果缓存，基于输入哈希判断有效性
/// </summary>
public sealed class QueryCache : IQueryCache
{
    private readonly Dictionary<QueryKey, CacheEntry> _cache = [];
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    ///     获取当前缓存条目数量
    /// </summary>
    public int count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    ///     获取缓存的结果，如果缓存命中且输入哈希匹配则返回结果
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="key">查询缓存键。</param>
    /// <param name="inputHash">当前输入哈希。</param>
    /// <returns>缓存的结果，如果未命中则为 null。</returns>
    public QueryResult<T>? get<T>(QueryKey key, int inputHash)
    {
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var entry) && entry.input_hash == inputHash)
                return entry.result as QueryResult<T>;

            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     存储查询结果到缓存
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="key">查询缓存键。</param>
    /// <param name="result">查询结果。</param>
    public void set<T>(QueryKey key, QueryResult<T> result)
    {
        _lock.EnterWriteLock();
        try
        {
            _cache[key] = new CacheEntry(result, result.input_hash);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     使指定查询的缓存失效
    /// </summary>
    /// <param name="key">查询缓存键。</param>
    public void invalidate(QueryKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     批量使多个查询的缓存失效
    /// </summary>
    /// <param name="keys">要失效的查询缓存键集合。</param>
    public void invalidate_range(IEnumerable<QueryKey> keys)
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var key in keys) _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     清空所有缓存
    /// </summary>
    public void clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private sealed record CacheEntry(object result, int input_hash);
}