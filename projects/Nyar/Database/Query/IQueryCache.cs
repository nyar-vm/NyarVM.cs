namespace Nyar.Database.Query;

/// <summary>
///     查询缓存接口，支持可替换的内存/持久化实现
/// </summary>
public interface IQueryCache
{
    /// <summary>
    ///     获取当前缓存条目数量
    /// </summary>
    int count { get; }

    /// <summary>
    ///     获取缓存的结果，如果缓存命中且输入哈希匹配则返回结果
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="key">查询缓存键。</param>
    /// <param name="inputHash">当前输入哈希。</param>
    /// <returns>缓存的结果，如果未命中则为 null。</returns>
    QueryResult<T>? get<T>(QueryKey key, int inputHash);

    /// <summary>
    ///     存储查询结果到缓存
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="key">查询缓存键。</param>
    /// <param name="result">查询结果。</param>
    void set<T>(QueryKey key, QueryResult<T> result);

    /// <summary>
    ///     使指定查询的缓存失效
    /// </summary>
    /// <param name="key">查询缓存键。</param>
    void invalidate(QueryKey key);

    /// <summary>
    ///     批量使多个查询的缓存失效
    /// </summary>
    /// <param name="keys">要失效的查询缓存键集合。</param>
    void invalidate_range(IEnumerable<QueryKey> keys);

    /// <summary>
    ///     清空所有缓存
    /// </summary>
    void clear();
}