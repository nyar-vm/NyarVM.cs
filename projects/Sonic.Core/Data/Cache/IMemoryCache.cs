using System;

namespace Core.Data.Cache;

/// <summary>
///     内存缓存接口
/// </summary>
/// <typeparam name="T">缓存值类型</typeparam>
public interface IMemoryCache<T>
{
    /// <summary>
    ///     获取缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值，不存在时返回默认值</returns>
    T? get(string key);

    /// <summary>
    ///     设置缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间，为 null 时永不过期</param>
    void set(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    ///     移除缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    void remove(string key);
}