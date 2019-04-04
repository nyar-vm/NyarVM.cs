using Core.Data.Cache;

namespace Std.Data.Cache;

/// <summary>
///     基于 <see cref="MemoryCache{T}" /> 的分布式缓存实现，提供异步接口的本地内存适配。
/// </summary>
public sealed class DistributedCache : IDistributedCache
{
    private readonly MemoryCache<byte[]> _cache = new();
    private readonly ICacheSerializer _serializer;

    /// <summary>
    ///     初始化分布式缓存的新实例。
    /// </summary>
    /// <param name="serializer">缓存序列化器。</param>
    public DistributedCache(ICacheSerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    ///     异步获取缓存值。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <returns>缓存值，不存在时返回 null。</returns>
    public Task<T?> get<T>(string key)
    {
        var data = _cache.get(key);

        if (data is null) return Task.FromResult<T?>(default);

        var value = _serializer.deserialize<T>(data);
        return Task.FromResult<T?>(value);
    }

    /// <summary>
    ///     异步设置缓存值。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="value">缓存值。</param>
    /// <param name="expiry">过期时间，为 null 时永不过期。</param>
    public Task set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var data = _serializer.serialize(value);
        _cache.set(key, data, expiry);
        return Task.CompletedTask;
    }
}