using System;
using System.Threading.Tasks;

namespace Core.Data.Cache;

/// <summary>
///     分布式缓存接口
/// </summary>
public interface IDistributedCache
{
    /// <summary>
    ///     异步获取缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    Task<T?> get<T>(string key);

    /// <summary>
    ///     异步设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间，为 null 时永不过期</param>
    Task set<T>(string key, T value, TimeSpan? expiry = null);
}