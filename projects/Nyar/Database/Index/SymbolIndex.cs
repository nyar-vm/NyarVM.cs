namespace Nyar.Database.Index;

/// <summary>
///     符号索引，按 SymbolId 存储和检索符号记录，支持按 FileUri 的 O(1) 二级索引
/// </summary>
public sealed class SymbolIndex
{
    private readonly Dictionary<string, List<SymbolRecord>> _by_file_uri = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SymbolId, SymbolRecord> _symbols = [];

    /// <summary>
    ///     获取所有符号记录
    /// </summary>
    public IEnumerable<SymbolRecord> all => _symbols.Values;

    /// <summary>
    ///     当前索引中的符号数量
    /// </summary>
    public int count => _symbols.Count;

    /// <summary>
    ///     添加符号记录，若已存在则替换
    /// </summary>
    /// <param name="record">符号记录。</param>
    public void add(SymbolRecord record)
    {
        if (_symbols.TryGetValue(record.id, out var existing)) remove_from_file_index(existing);

        _symbols[record.id] = record;
        add_to_file_index(record);
    }

    /// <summary>
    ///     批量添加符号记录
    /// </summary>
    /// <param name="records">符号记录列表。</param>
    public void add_range(IEnumerable<SymbolRecord> records)
    {
        foreach (var record in records) add(record);
    }

    /// <summary>
    ///     按 SymbolId 移除符号记录
    /// </summary>
    /// <param name="id">符号唯一标识。</param>
    /// <returns>是否成功移除。</returns>
    public bool remove(SymbolId id)
    {
        if (!_symbols.Remove(id, out var record)) return false;

        remove_from_file_index(record);
        return true;
    }

    /// <summary>
    ///     按 SymbolId 查找符号记录
    /// </summary>
    /// <param name="id">符号唯一标识。</param>
    /// <param name="record">找到的符号记录。</param>
    /// <returns>是否找到。</returns>
    public bool try_get(SymbolId id, out SymbolRecord record)
    {
        return _symbols.TryGetValue(id, out record!);
    }

    /// <summary>
    ///     获取指定文件 URI 中的所有符号（O(1) 查找）
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>符号记录列表。</returns>
    public IReadOnlyList<SymbolRecord> get_by_file_uri(string fileUri)
    {
        return _by_file_uri.TryGetValue(fileUri, out var list)
            ? list.AsReadOnly()
            : Array.Empty<SymbolRecord>();
    }

    /// <summary>
    ///     移除指定文件 URI 中的所有符号
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    /// <returns>移除的符号数量。</returns>
    public int remove_by_file_uri(string fileUri)
    {
        if (!_by_file_uri.TryGetValue(fileUri, out var fileSymbols)) return 0;

        var count = fileSymbols.Count;
        foreach (var symbol in fileSymbols) _symbols.Remove(symbol.id);

        _by_file_uri.Remove(fileUri);
        return count;
    }

    /// <summary>
    ///     清空索引
    /// </summary>
    public void clear()
    {
        _symbols.Clear();
        _by_file_uri.Clear();
    }

    private void add_to_file_index(SymbolRecord record)
    {
        if (!_by_file_uri.TryGetValue(record.file_uri, out var list))
        {
            list = [];
            _by_file_uri[record.file_uri] = list;
        }

        list.Add(record);
    }

    private void remove_from_file_index(SymbolRecord record)
    {
        if (_by_file_uri.TryGetValue(record.file_uri, out var list))
        {
            list.Remove(record);
            if (list.Count == 0) _by_file_uri.Remove(record.file_uri);
        }
    }
}