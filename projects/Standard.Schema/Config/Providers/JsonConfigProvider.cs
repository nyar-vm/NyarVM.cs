using System.Text.Json;

namespace Hermes.Config.Providers;

/// <summary>
///     JSON 文件 Config Provider — 消费 [config("json:...")] 声明
/// </summary>
public sealed class JsonConfigProvider : IConfigProvider
{
    private readonly List<Action<IReadOnlyDictionary<string, string>>> _watchers;
    private Dictionary<string, string> _cache;
    private FileSystemWatcher? _fileWatcher;

    /// <summary>
    ///     创建 JsonConfigProvider
    /// </summary>
    /// <param name="name">Provider 名称（如 "appsettings"、"secrets"）</param>
    /// <param name="filePath">JSON 文件路径</param>
    /// <param name="optional">文件可选</param>
    public JsonConfigProvider(string name, string filePath, bool optional = false)
    {
        Name = name;
        FilePath = filePath;
        Optional = optional;
        _watchers = [];
        _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Reload();
        SetupFileWatcher();
    }

    /// <summary>
    ///     JSON 文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     文件是否可选（不存在时不报错）
    /// </summary>
    public bool Optional { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string ProviderType => "json";

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

    private void Reload()
    {
        if (!File.Exists(FilePath))
        {
            if (!Optional) throw new FileNotFoundException($"Config JSON 文件未找到：{FilePath}");

            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        try
        {
            var json = File.ReadAllText(FilePath);

            using var doc = JsonDocument.Parse(json);

            _cache = FlattenJson(doc.RootElement);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Config JSON 文件解析失败：{FilePath}", ex);
        }
    }

    private void SetupFileWatcher()
    {
        var directory = Path.GetDirectoryName(FilePath);
        var fileName = Path.GetFileName(FilePath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName)) return;

        if (!Directory.Exists(directory)) return;

        _fileWatcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Created += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Thread.Sleep(100);
        Reload();

        var snapshot = new Dictionary<string, string>(_cache, StringComparer.OrdinalIgnoreCase);
        foreach (var watcher in _watchers) watcher(snapshot);
    }

    private static Dictionary<string, string> FlattenJson(JsonElement element, string prefix = "")
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childPrefix = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    foreach (var kvp in FlattenJson(property.Value, childPrefix)) result[kvp.Key] = kvp.Value;
                }

                break;
            case JsonValueKind.Array:
                for (var i = 0; i < element.GetArrayLength(); i++)
                {
                    var childPrefix = $"{prefix}[{i}]";
                    foreach (var kvp in FlattenJson(element[i], childPrefix)) result[kvp.Key] = kvp.Value;
                }

                break;
            default:
                result[prefix] = element.ToString();
                break;
        }

        return result;
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