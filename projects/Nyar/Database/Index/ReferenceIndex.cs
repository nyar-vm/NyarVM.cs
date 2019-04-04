namespace Nyar.Database.Index;

/// <summary>
///     引用索引，存储和检索符号引用记录，支持按 SymbolId 和 FileUri 的 O(1) 二级索引
/// </summary>
public sealed class ReferenceIndex
{
    private readonly Dictionary<string, List<ReferenceRecord>> _by_file_uri = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SymbolId, List<ReferenceRecord>> _by_symbol_id = [];
    private readonly List<ReferenceRecord> _references = [];

    /// <summary>
    ///     获取所有引用记录
    /// </summary>
    public IEnumerable<ReferenceRecord> all => _references;

    /// <summary>
    ///     当前索引中的引用数量
    /// </summary>
    public int count => _references.Count;

    /// <summary>
    ///     添加引用记录
    /// </summary>
    /// <param name="record">引用记录。</param>
    public void add(ReferenceRecord record)
    {
        _references.Add(record);
        add_to_symbol_index(record);
        add_to_file_index(record);
    }

    /// <summary>
    ///     批量添加引用记录
    /// </summary>
    /// <param name="records">引用记录列表。</param>
    public void add_range(IEnumerable<ReferenceRecord> records)
    {
        foreach (var record in records) add(record);
    }

    /// <summary>
    ///     获取指向指定符号的所有引用（O(1) 查找）
    /// </summary>
    /// <param name="symbolId">符号唯一标识。</param>
    /// <returns>引用记录列表。</returns>
    public IReadOnlyList<ReferenceRecord> get_by_symbol_id(SymbolId symbolId)
    {
        return _by_symbol_id.TryGetValue(symbolId, out var list)
            ? list.AsReadOnly()
            : Array.Empty<ReferenceRecord>();
    }

    /// <summary>
    ///     获取指定文件 URI 中的所有引用（O(1) 查找）
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>引用记录列表。</returns>
    public IReadOnlyList<ReferenceRecord> get_by_file_uri(string fileUri)
    {
        return _by_file_uri.TryGetValue(fileUri, out var list)
            ? list.AsReadOnly()
            : Array.Empty<ReferenceRecord>();
    }

    /// <summary>
    ///     移除指定文件 URI 中的所有引用
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>移除的引用数量。</returns>
    public int remove_by_file_uri(string fileUri)
    {
        if (!_by_file_uri.TryGetValue(fileUri, out var fileRefs)) return 0;

        var count = fileRefs.Count;
        foreach (var reference in fileRefs)
        {
            _references.Remove(reference);
            remove_from_symbol_index(reference);
        }

        _by_file_uri.Remove(fileUri);
        return count;
    }

    /// <summary>
    ///     移除指向指定文件 URI 中符号的所有引用（即删除其他文件对该文件符号的引用）
    /// </summary>
    /// <param name="fileUri">符号所在文件 URI。</param>
    /// <returns>移除的引用数量。</returns>
    public int remove_by_symbol_file_uri(string fileUri)
    {
        var refsToRemove = new List<ReferenceRecord>();
        foreach (var reference in _references)
            if (reference.symbol_id.file_uri == fileUri)
                refsToRemove.Add(reference);

        foreach (var reference in refsToRemove)
        {
            _references.Remove(reference);
            remove_from_symbol_index(reference);
            remove_from_file_index(reference);
        }

        return refsToRemove.Count;
    }

    /// <summary>
    ///     清空索引
    /// </summary>
    public void clear()
    {
        _references.Clear();
        _by_symbol_id.Clear();
        _by_file_uri.Clear();
    }

    private void add_to_symbol_index(ReferenceRecord record)
    {
        if (!_by_symbol_id.TryGetValue(record.symbol_id, out var list))
        {
            list = [];
            _by_symbol_id[record.symbol_id] = list;
        }

        list.Add(record);
    }

    private void add_to_file_index(ReferenceRecord record)
    {
        if (!_by_file_uri.TryGetValue(record.file_uri, out var list))
        {
            list = [];
            _by_file_uri[record.file_uri] = list;
        }

        list.Add(record);
    }

    private void remove_from_symbol_index(ReferenceRecord record)
    {
        if (_by_symbol_id.TryGetValue(record.symbol_id, out var list))
        {
            list.Remove(record);
            if (list.Count == 0) _by_symbol_id.Remove(record.symbol_id);
        }
    }

    private void remove_from_file_index(ReferenceRecord record)
    {
        if (_by_file_uri.TryGetValue(record.file_uri, out var list))
        {
            list.Remove(record);
            if (list.Count == 0) _by_file_uri.Remove(record.file_uri);
        }
    }
}