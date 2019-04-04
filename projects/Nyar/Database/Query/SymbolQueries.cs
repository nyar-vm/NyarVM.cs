using Nyar.Database.Index;

namespace Nyar.Database.Query;

/// <summary>
///     按文件 URI 查询符号列表的增量查询
/// </summary>
public sealed class SymbolsByFileQuery : IQuery<string, IReadOnlyList<SymbolRecord>>
{
    private readonly IndexManager _index_manager;

    public SymbolsByFileQuery(IndexManager indexManager)
    {
        _index_manager = indexManager;
    }

    public string name => "symbols_by_file";

    public IReadOnlyList<SymbolRecord> execute(string fileUri, QueryContext context)
    {
        return _index_manager.symbols.get_by_file_uri(fileUri);
    }

    public int compute_input_hash(string fileUri)
    {
        return fileUri.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
///     按符号 ID 查询引用列表的增量查询
/// </summary>
public sealed class ReferencesBySymbolQuery : IQuery<SymbolId, IReadOnlyList<ReferenceRecord>>
{
    private readonly IndexManager _index_manager;

    public ReferencesBySymbolQuery(IndexManager indexManager)
    {
        _index_manager = indexManager;
    }

    public string name => "references_by_symbol";

    public IReadOnlyList<ReferenceRecord> execute(SymbolId symbolId, QueryContext context)
    {
        return _index_manager.references.get_by_symbol_id(symbolId);
    }

    public int compute_input_hash(SymbolId symbolId)
    {
        return HashCode.Combine(symbolId.file_uri, symbolId.name, symbolId.kind_name);
    }
}

/// <summary>
///     按文件 URI 查询依赖列表的增量查询
/// </summary>
public sealed class FileDependenciesQuery : IQuery<string, IReadOnlySet<string>>
{
    private readonly IndexManager _index_manager;

    public FileDependenciesQuery(IndexManager indexManager)
    {
        _index_manager = indexManager;
    }

    public string name => "file_dependencies";

    public IReadOnlySet<string> execute(string fileUri, QueryContext context)
    {
        return _index_manager.files.get_dependencies(fileUri);
    }

    public int compute_input_hash(string fileUri)
    {
        var file = _index_manager.files.find(fileUri);
        return file?.content_hash.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
    }
}

/// <summary>
///     按文件 URI 查询反向依赖列表的增量查询
/// </summary>
public sealed class FileDependentsQuery : IQuery<string, IReadOnlySet<string>>
{
    private readonly IndexManager _index_manager;

    public FileDependentsQuery(IndexManager indexManager)
    {
        _index_manager = indexManager;
    }

    public string name => "file_dependents";

    public IReadOnlySet<string> execute(string fileUri, QueryContext context)
    {
        return _index_manager.files.get_transitive_dependents(fileUri);
    }

    public int compute_input_hash(string fileUri)
    {
        return fileUri.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}