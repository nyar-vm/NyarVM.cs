namespace Std.App.Server.Systems;

/// <summary>
///     Atlas 缓存接口，提供统一的键值缓存操作。
/// </summary>
public interface IAtlasCache
{
    /// <summary>
    ///     异步获取缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值，未命中时返回 null</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    ///     异步设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    ///     异步移除缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    Task RemoveAsync(string key);
}