namespace Hermes.Cache;

public sealed class CacheOptions
{
    public int DefaultExpireMinutes { get; set; } = 60;
    public int MaxItems { get; set; } = 10000;
    public string KeyPrefix { get; set; } = "hermes:cache:";
    public string? ConnectionString { get; set; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}