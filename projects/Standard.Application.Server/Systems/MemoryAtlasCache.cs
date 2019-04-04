using Microsoft.Extensions.Caching.Memory;

namespace Std.App.Server.Systems;

/// <summary>
///     基于 MemoryCache 的缓存实现
/// </summary>
public sealed class MemoryAtlasCache : IAtlasCache, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key)
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult((T?)value);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiry.HasValue) options.AbsoluteExpirationRelativeToNow = expiry.Value;

        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     释放缓存资源
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
    }
}