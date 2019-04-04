namespace Std.Data;

/// <summary>
///     <see cref="ICacheService" /> 扩展方法。
/// </summary>
public static class MemoryCacheExtensions
{
    /// <summary>
    ///     获取或创建缓存项——若缓存命中则直接返回，否则调用工厂函数创建并缓存。
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cache">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="factory">当缓存未命中时的工厂函数</param>
    /// <param name="expire">过期时间（可选）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>缓存的值</returns>
    public static async Task<T?> get_or_set<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? expire = null,
        CancellationToken ct = default)
    {
        var cached = await cache.get<T>(key, ct);

        if (cached is not null) return cached;

        var value = await factory();

        if (value is not null) await cache.set(key, value, expire, ct);

        return value;
    }

    /// <summary>
    ///     尝试获取缓存项，返回结果封装在 <see cref="CacheResult{T}" /> 中。
    ///     异步方法不能使用 out 参数，改用返回值封装。
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cache">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>缓存结果</returns>
    public static async Task<CacheResult<T>> try_get<T>(
        this ICacheService cache,
        string key,
        CancellationToken ct = default)
    {
        var value = await cache.get<T>(key, ct);
        return new CacheResult<T>(value is not null, value);
    }
}

/// <summary>
///     缓存获取结果封装，替代异步方法的 out 参数模式。
/// </summary>
/// <typeparam name="T">缓存值类型</typeparam>
public sealed class CacheResult<T>
{
    /// <summary>
    ///     初始化缓存结果。
    /// </summary>
    /// <param name="success">是否成功</param>
    /// <param name="value">缓存值</param>
    public CacheResult(bool success, T? value)
    {
        this.success = success;
        this.value = value;
    }

    /// <summary>
    ///     是否成功获取到缓存项。
    /// </summary>
    public bool success { get; }

    /// <summary>
    ///     获取到的缓存值。
    /// </summary>
    public T? value { get; }
}