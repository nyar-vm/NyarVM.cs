namespace Std.Config;

/// <summary>
///     基于 <see cref="FileSystemWatcher" /> 的配置热加载监视器实现�?/// 当监控的文件发生变化时，经过防抖延迟后自动重建配置�?///
/// </summary>
/// <typeparam name="T">配置类型，必须实�?<see cref="IConfigurable" />�?/typeparam>
public sealed class ConfigWatcher<T> : IConfigWatcher<T> where T : IConfigurable
{
    #region 常量

    /// <summary>
    ///     默认防抖延迟时间（毫秒）�?    ///
    /// </summary>
    private const int _default_debounce_ms = 500;

    #endregion

    #region 公开方法

    /// <summary>
    ///     释放所有文件系统监视器和防抖定时器�?    ///
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _debounce_timer.Change(Timeout.Infinite, Timeout.Infinite);
        _debounce_timer.Dispose();

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();
    }

    #endregion

    #region 字段

    /// <summary>
    ///     配置重建函数�?    ///
    /// </summary>
    private readonly Func<T> _rebuild_func;

    /// <summary>
    ///     防抖定时器�?    ///
    /// </summary>
    private readonly Timer _debounce_timer;

    /// <summary>
    ///     防抖延迟时间（毫秒）�?    ///
    /// </summary>
    private readonly int _debounce_ms;

    /// <summary>
    ///     文件系统监视器列表�?    ///
    /// </summary>
    private readonly List<FileSystemWatcher> _watchers;

    /// <summary>
    ///     用于同步重建操作的锁对象�?    ///
    /// </summary>
    private readonly object _reload_lock = new();

    /// <summary>
    ///     标识是否已释放�?    ///
    /// </summary>
    private volatile bool _disposed;

    #endregion

    #region 属�?

    /// <summary>
    ///     获取当前的配置实例�?    ///
    /// </summary>
    public T current { get; private set; }

    /// <summary>
    ///     配置重载完成时触发的事件�?    ///
    /// </summary>
    public event Action<T>? OnReloaded;

    #endregion

    #region 构造函�?

    /// <summary>
    ///     使用初始配置、重建函数和监控文件路径初始�?<see cref="ConfigWatcher{T}" /> 的新实例�?    ///
    /// </summary>
    /// <param name="initialValue">
    ///     初始配置实例�?/param>
    ///     <param name="rebuildFunc">
    ///         配置重建函数，当文件变化时调用以生成新配置�?/param>
    ///         <param name="filePaths">
    ///             需要监控的文件路径集合�?/param>
    ///             <exception cref="ArgumentNullException">�?<paramref name="rebuildFunc" /> �?null 时抛出�?/exception>
    public ConfigWatcher(T initialValue, Func<T> rebuildFunc, IEnumerable<string> filePaths)
        : this(initialValue, rebuildFunc, filePaths, _default_debounce_ms)
    {
    }

    /// <summary>
    ///     使用初始配置、重建函数、监控文件路径和防抖延迟初始�?<see cref="ConfigWatcher{T}" /> 的新实例�?    ///
    /// </summary>
    /// <param name="initialValue">
    ///     初始配置实例�?/param>
    ///     <param name="rebuildFunc">
    ///         配置重建函数，当文件变化时调用以生成新配置�?/param>
    ///         <param name="filePaths">
    ///             需要监控的文件路径集合�?/param>
    ///             <param name="debounceMs">
    ///                 防抖延迟时间（毫秒），默认为 500�?/param>
    ///                 <exception cref="ArgumentNullException">�?<paramref name="rebuildFunc" /> �?null 时抛出�?/exception>
    public ConfigWatcher(T initialValue, Func<T> rebuildFunc, IEnumerable<string> filePaths, int debounceMs)
    {
        _rebuild_func = rebuildFunc ?? throw new ArgumentNullException(nameof(rebuildFunc));
        _debounce_ms = debounceMs > 0 ? debounceMs : _default_debounce_ms;
        current = initialValue;
        _debounce_timer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
        _watchers = create_watchers(filePaths);
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     根据文件路径集合创建文件系统监视器，按目录分组�?    ///
    /// </summary>
    /// <param name="filePaths">
    ///     文件路径集合�?/param>
    ///     <returns>文件系统监视器列表�?/returns>
    private List<FileSystemWatcher> create_watchers(IEnumerable<string> filePaths)
    {
        var directoryGroups = new Dictionary<string, HashSet<string>>();

        foreach (var path in filePaths)
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (directory is null) continue;

            var fileName = Path.GetFileName(fullPath);

            if (!directoryGroups.TryGetValue(directory, out var fileNames))
            {
                fileNames = new(StringComparer.OrdinalIgnoreCase);
                directoryGroups[directory] = fileNames;
            }

            fileNames.Add(fileName);
        }

        var watchers = new List<FileSystemWatcher>();

        foreach (var (directory, fileNames) in directoryGroups)
        {
            if (!Directory.Exists(directory)) continue;

            var watcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            foreach (var fileName in fileNames) watcher.Filters.Add(fileName);

            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;

            watchers.Add(watcher);
        }

        return watchers;
    }

    /// <summary>
    ///     文件变化事件处理，重启防抖定时器�?    ///
    /// </summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        restart_debounce();
    }

    /// <summary>
    ///     文件重命名事件处理，重启防抖定时器�?    ///
    /// </summary>
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        restart_debounce();
    }

    /// <summary>
    ///     重启防抖定时器�?    ///
    /// </summary>
    private void restart_debounce()
    {
        if (_disposed) return;

        _debounce_timer.Change(_debounce_ms, Timeout.Infinite);
    }

    /// <summary>
    ///     防抖定时器到期回调，执行配置重建�?    ///
    /// </summary>
    private void OnDebounceElapsed(object? state)
    {
        if (_disposed) return;

        lock (_reload_lock)
        {
            try
            {
                var rebuilt = _rebuild_func();
                var errors = rebuilt.validate();

                if (errors.Count > 0) return;

                current = rebuilt;
                OnReloaded?.Invoke(current);
            }
            catch
            {
                // 重建失败时保留旧配置
            }
        }
    }

    #endregion
}