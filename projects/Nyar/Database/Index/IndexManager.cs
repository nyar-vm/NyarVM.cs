namespace Nyar.Database.Index;

/// <summary>
///     索引管理器，统一管理符号索引、引用索引和文件索引
/// </summary>
public sealed class IndexManager
{
    /// <summary>
    ///     符号索引
    /// </summary>
    public SymbolIndex symbols { get; } = new();

    /// <summary>
    ///     引用索引
    /// </summary>
    public ReferenceIndex references { get; } = new();

    /// <summary>
    ///     文件索引
    /// </summary>
    public FileIndex files { get; } = new();

    /// <summary>
    ///     移除指定文件的所有索引数据（符号、引用、文件记录）
    /// </summary>
    /// <param name="fileUri">文件 URI。</param>
    public void remove_file(string fileUri)
    {
        references.remove_by_file_uri(fileUri);
        references.remove_by_symbol_file_uri(fileUri);
        symbols.remove_by_file_uri(fileUri);
        files.remove(fileUri);
    }

    /// <summary>
    ///     增量更新：移除旧索引数据，添加新索引数据
    /// </summary>
    /// <param name="fileRecord">新的文件记录。</param>
    /// <param name="symbols">该文件的新符号列表。</param>
    /// <param name="references">该文件的新引用列表。</param>
    public void update_file(FileRecord fileRecord, IEnumerable<SymbolRecord> symbols,
        IEnumerable<ReferenceRecord> references)
    {
        this.references.remove_by_file_uri(fileRecord.uri);
        this.symbols.remove_by_file_uri(fileRecord.uri);

        this.symbols.add_range(symbols);
        this.references.add_range(references);
        files.add_or_update(fileRecord);
    }

    /// <summary>
    ///     清空所有索引
    /// </summary>
    public void clear()
    {
        symbols.clear();
        references.clear();
        files.clear();
    }
}