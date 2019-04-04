using System.Collections;

namespace Hermes.Config.Providers;

/// <summary>
///     环境变量 Config Provider — 消费 [env("VAR_NAME")] 声明
/// </summary>
public sealed class EnvConfigProvider : IConfigProvider
{
    private readonly Dictionary<string, string> _cache;
    private readonly List<Action<IReadOnlyDictionary<string, string>>> _watchers;

    /// <summary>
    ///     创建 EnvConfigProvider
    /// </summary>
    /// <param name="name">Provider 名称（如 "app"、"redis"）</param>
    public EnvConfigProvider(string name)
    {
        Name = name;
        _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _watchers = [];
        LoadAllEnvVars();
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string ProviderType => "env";

    /// <inheritdoc />
    public Task<string?> TryLoadAsync(string path)
    {
        _cache.TryGetValue(path, out var value);
        return Task.FromResult<string?>(value);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> LoadAllAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(_cache, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IDisposable Watch(Action<IReadOnlyDictionary<string, string>> callback)
    {
        _watchers.Add(callback);
        return new WatchSubscription(() => _watchers.Remove(callback));
    }

    private void LoadAllEnvVars()
    {
        var envVars = Environment.GetEnvironmentVariables();

        foreach (DictionaryEntry entry in envVars)
        {
            var key = entry.Key?.ToString();
            var value = entry.Value?.ToString();

            if (key is not null && value is not null) _cache[key] = value;
        }
    }

    private sealed class WatchSubscription : IDisposable
    {
        private readonly Action _onDispose;

        public WatchSubscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}