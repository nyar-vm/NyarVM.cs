using System.Text.Json;

namespace Hermes.Cache.Redis;

public sealed class RedisCacheService : ICacheService, IDisposable
{
    private readonly RedisClient _client;
    private readonly int _database;
    private readonly CacheOptions _options;
    private readonly string? _password;
    private bool _disposed;
    private bool _initialized;

    public RedisCacheService(CacheOptions options)
    {
        _options = options;

        var (host, port, password, database) = ParseConnectionString(options.ConnectionString);
        _password = password;
        _database = database;

        var connection = new RedisConnection(host, port);
        _client = new RedisClient(connection);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(ct);

        var fullKey = BuildKey(key);
        var value = await _client.GetStringAsync(fullKey, ct);

        if (value == null) return default;

        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(ct);

        var fullKey = BuildKey(key);
        var json = JsonSerializer.Serialize(value);

        await _client.SetStringAsync(fullKey, json, expire, ct);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(ct);

        var fullKey = BuildKey(key);
        return await _client.DeleteAsync(fullKey, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(ct);

        var fullKey = BuildKey(key);
        return await _client.ExistsAsync(fullKey, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(ct);

        await _client.FlushDbAsync(ct);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _client.Dispose();
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        if (!string.IsNullOrEmpty(_password)) await _client.AuthAsync(_password, ct);

        if (_database > 0) await _client.SelectAsync(_database, ct);

        _initialized = true;
    }

    private string BuildKey(string key)
    {
        return $"{_options.KeyPrefix}{key}";
    }

    private static (string Host, int Port, string? Password, int Database) ParseConnectionString(
        string? connectionString)
    {
        var host = "localhost";
        var port = 6379;
        string? password = null;
        var database = 0;

        if (string.IsNullOrEmpty(connectionString)) return (host, port, password, database);

        var parts = connectionString.Split(',');
        var hostPort = parts[0];

        if (hostPort.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)) hostPort = hostPort[8..];

        var hostPortParts = hostPort.Split(':');
        host = hostPortParts[0];

        if (hostPortParts.Length > 1 && int.TryParse(hostPortParts[1], out var parsedPort)) port = parsedPort;

        for (var i = 1; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=', 2);
            if (kv.Length != 2) continue;

            switch (kv[0].Trim().ToLowerInvariant())
            {
                case "password":
                    password = kv[1].Trim();
                    break;
                case "db":
                    int.TryParse(kv[1].Trim(), out database);
                    break;
            }
        }

        return (host, port, password, database);
    }
}