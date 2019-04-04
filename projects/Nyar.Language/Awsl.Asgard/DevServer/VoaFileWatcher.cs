namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     VOA 文件监听器，监视项目文件变更并触发增量编译
/// </summary>
public sealed class VoaFileWatcher : IDisposable
{
    private readonly HashSet<string> _changed_files = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _debounce_ms;
    private readonly List<string> _ignore_patterns;
    private readonly object _lock = new();
    private readonly string _project_dir;
    private readonly List<string> _watch_patterns;
    private readonly List<FileSystemWatcher> _watchers = [];
    private Timer? _debounce_timer;
    private bool _disposed;

    public VoaFileWatcher(string projectDir, List<string> watchPatterns, List<string> ignorePatterns, int debounceMs)
    {
        _project_dir = projectDir;
        _watch_patterns = watchPatterns;
        _ignore_patterns = ignorePatterns;
        _debounce_ms = debounceMs;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        stop();

        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
    }

    public event Action<string>? OnFileChanged;
    public event Action<string, string?>? OnError;

    public void start()
    {
        var sourceDir = Path.Combine(_project_dir, "source");
        var assetsDir = Path.Combine(_project_dir, "assets");

        var directories = new List<string>();

        if (Directory.Exists(sourceDir))
        {
            directories.Add(sourceDir);
        }

        if (Directory.Exists(assetsDir))
        {
            directories.Add(assetsDir);
        }

        if (directories.Count == 0)
        {
            directories.Add(_project_dir);
        }

        foreach (var dir in directories)
        {
            var watcher = new FileSystemWatcher(dir)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += OnFileSystemEvent;
            watcher.Created += OnFileSystemEvent;
            watcher.Renamed += OnRenamedEvent;
            watcher.Deleted += OnDeletedEvent;

            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
    }

    public void stop()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
        }

        lock (_lock)
        {
            _debounce_timer?.Dispose();
            _debounce_timer = null;
            _changed_files.Clear();
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        if (!is_relevant_file(e.FullPath))
        {
            return;
        }

        if (e.ChangeType is WatcherChangeTypes.Changed or WatcherChangeTypes.Created)
        {
            schedule_debounce(e.FullPath);
        }
    }

    private void OnRenamedEvent(object sender, RenamedEventArgs e)
    {
        if (!is_relevant_file(e.FullPath))
        {
            return;
        }

        schedule_debounce(e.FullPath);
    }

    private void OnDeletedEvent(object sender, FileSystemEventArgs e)
    {
        if (!is_relevant_file(e.FullPath))
        {
            return;
        }

        schedule_debounce(e.FullPath);
    }

    private void schedule_debounce(string filePath)
    {
        lock (_lock)
        {
            _changed_files.Add(filePath);

            _debounce_timer?.Dispose();
            _debounce_timer = new Timer(OnDebounceElapsed, null, _debounce_ms, Timeout.Infinite);
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        List<string> files;

        lock (_lock)
        {
            files = [.._changed_files];
            _changed_files.Clear();
            _debounce_timer?.Dispose();
            _debounce_timer = null;
        }

        foreach (var file in files)
        {
            try
            {
                OnFileChanged?.Invoke(file);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"文件变更处理失败：{ex.Message}", file);
            }
        }
    }

    private bool is_relevant_file(string filePath)
    {
        var relativePath = Path.GetRelativePath(_project_dir, filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension is not (".v" or ".awsl" or ".css" or ".js" or ".html" or ".json" or ".svg" or ".png" or ".jpg"))
        {
            return false;
        }

        foreach (var ignore in _ignore_patterns)
        {
            var normalizedIgnore = ignore.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar);

            if (relativePath.StartsWith(normalizedIgnore, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
