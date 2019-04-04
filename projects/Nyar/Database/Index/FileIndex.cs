namespace Nyar.Database.Index;

/// <summary>
///     文件索引，追踪文件状态和依赖关系
/// </summary>
public sealed class FileIndex
{
    private readonly Dictionary<string, HashSet<string>> _dependency_graph = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FileRecord> _files = new(StringComparer.OrdinalIgnoreCase);

    private readonly ReaderWriterLockSlim _lock = new();

    private readonly Dictionary<string, HashSet<string>>
        _reverse_dependency_graph = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     获取所有文件记录
    /// </summary>
    public IReadOnlyList<FileRecord> all
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _files.Values.ToList().AsReadOnly();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    ///     获取索引中的文件总数
    /// </summary>
    public int count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _files.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    ///     添加或更新文件记录
    /// </summary>
    /// <param name="record">文件记录。</param>
    public void add_or_update(FileRecord record)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_files.TryGetValue(record.uri, out var existing)) remove_dependencies(existing);

            _files[record.uri] = record;
            add_dependencies(record);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     移除文件记录
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>是否移除成功。</returns>
    public bool remove(string fileUri)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_files.TryGetValue(fileUri, out var existing)) return false;

            remove_dependencies(existing);
            _files.Remove(fileUri);
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     按文件 URI 查找
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>文件记录，如果未找到则为 null。</returns>
    public FileRecord? find(string fileUri)
    {
        _lock.EnterReadLock();
        try
        {
            return _files.GetValueOrDefault(fileUri);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     检查文件是否已存在
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>是否存在。</returns>
    public bool contains(string fileUri)
    {
        _lock.EnterReadLock();
        try
        {
            return _files.ContainsKey(fileUri);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     获取指定文件依赖的所有文件 URI
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>依赖的文件 URI 集合。</returns>
    public IReadOnlySet<string> get_dependencies(string fileUri)
    {
        _lock.EnterReadLock();
        try
        {
            return _dependency_graph.GetValueOrDefault(fileUri, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     获取依赖指定文件的所有文件 URI（反向依赖）
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>反向依赖的文件 URI 集合。</returns>
    public IReadOnlySet<string> get_dependents(string fileUri)
    {
        _lock.EnterReadLock();
        try
        {
            return _reverse_dependency_graph.GetValueOrDefault(fileUri,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     获取指定文件的所有传递依赖方（包括间接依赖）
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>所有传递依赖方的文件 URI 集合。</returns>
    public IReadOnlySet<string> get_transitive_dependents(string fileUri)
    {
        _lock.EnterReadLock();
        try
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();
            stack.Push(fileUri);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (_reverse_dependency_graph.TryGetValue(current, out var dependents))
                    foreach (var dep in dependents)
                        if (result.Add(dep))
                            stack.Push(dep);
            }

            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     检测文件是否发生变更（通过内容哈希比较）
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <param name="currentHash">当前内容哈希。</param>
    /// <returns>是否发生变更。</returns>
    public bool has_changed(string fileUri, string currentHash)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_files.TryGetValue(fileUri, out var record)) return true;

            return record.content_hash != currentHash;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     清空索引
    /// </summary>
    public void clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _files.Clear();
            _dependency_graph.Clear();
            _reverse_dependency_graph.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void add_dependencies(FileRecord record)
    {
        if (!_dependency_graph.ContainsKey(record.uri))
            _dependency_graph[record.uri] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var depUri in record.dependency_uris)
        {
            _dependency_graph[record.uri].Add(depUri);

            if (!_reverse_dependency_graph.ContainsKey(depUri))
                _reverse_dependency_graph[depUri] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _reverse_dependency_graph[depUri].Add(record.uri);
        }
    }

    private void remove_dependencies(FileRecord record)
    {
        if (_dependency_graph.TryGetValue(record.uri, out var deps))
        {
            foreach (var depUri in deps)
                if (_reverse_dependency_graph.TryGetValue(depUri, out var reverseDeps))
                {
                    reverseDeps.Remove(record.uri);
                    if (reverseDeps.Count == 0) _reverse_dependency_graph.Remove(depUri);
                }

            _dependency_graph.Remove(record.uri);
        }
    }
}