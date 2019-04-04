using Nyar.Database.Index;
using Nyar.Database.Query;
using Nyar.Database.Storage;
using IStorageEngine = Nyar.Database.Storage.IStorageEngine;

namespace Nyar.Database.Integration;

/// <summary>
///     符号分析引擎，整合索引管理、存储引擎和查询引擎，提供统一的符号分析 API
/// </summary>
public sealed class SymbolAnalysisEngine : IAsyncDisposable
{
    private readonly QueryRegistry _registry;
    private bool _disposed;

    // TODO: 待实现 - StorageEngine 和 PersistentQueryCache 已注释掉（依赖 LightDb internal API），
    // 待 Sonic.Standard.Database 提供高层泛型 API 后恢复此构造函数
    //
    // public SymbolAnalysisEngine(string basePath)
    // {
    //     IndexManager = new IndexManager();
    //     StorageEngine = new StorageEngine(basePath);
    //     _registry = new QueryRegistry();
    //     var options = new LightOptions
    //     {
    //         Path = Path.Combine(basePath, "query_cache"),
    //         PageSize = 4096,
    //         BTreeOrder = 32
    //     };
    //     var cacheDb = new LightDB(options);
    //     QueryEngine = new QueryEngine(_registry, new PersistentQueryCache(cacheDb), new DependencyGraph());
    //
    //     RegisterQueries();
    // }

    public SymbolAnalysisEngine(IndexManager indexManager, IStorageEngine storageEngine)
    {
        index_manager = indexManager;
        storage_engine = storageEngine;
        _registry = new QueryRegistry();
        query_engine = new QueryEngine(_registry, new QueryCache(), new DependencyGraph());

        register_queries();
    }

    /// <summary>
    ///     索引管理器
    /// </summary>
    public IndexManager index_manager { get; }

    /// <summary>
    ///     存储引擎
    /// </summary>
    public IStorageEngine storage_engine { get; }

    /// <summary>
    ///     查询引擎
    /// </summary>
    public QueryEngine query_engine { get; }

    /// <summary>
    ///     是否已完成索引恢复
    /// </summary>
    public bool is_restored { get; private set; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        await storage_engine.DisposeAsync();
    }

    /// <summary>
    ///     快速冷启动：仅加载文件索引，符号和引用按需加载
    /// </summary>
    public async Task fast_restore(CancellationToken ct = default)
    {
        await storage_engine.restore_file_index(index_manager, ct);
        is_restored = true;
    }

    /// <summary>
    ///     全量恢复：加载所有索引数据
    /// </summary>
    public async Task full_restore(CancellationToken ct = default)
    {
        await storage_engine.restore(index_manager, ct);
        is_restored = true;
    }

    /// <summary>
    ///     按需加载指定文件的符号和引用
    /// </summary>
    public async Task load_file(string fileUri, CancellationToken ct = default)
    {
        await storage_engine.load_symbols_for_file(index_manager, fileUri, ct);
        await storage_engine.load_references_for_file(index_manager, fileUri, ct);
    }

    /// <summary>
    ///     更新文件的分析结果（增量更新索引 + 持久化）
    /// </summary>
    public async Task update_file(FileRecord fileRecord, IEnumerable<SymbolRecord> symbols,
        IEnumerable<ReferenceRecord> references, CancellationToken ct = default)
    {
        index_manager.update_file(fileRecord, symbols, references);

        invalidate_queries_for_file(fileRecord.uri);

        var operations = new List<(WalOperationType, object)>
        {
            (WalOperationType.upsert_file, fileRecord)
        };

        foreach (var symbol in symbols) operations.Add((WalOperationType.upsert_symbol, symbol));

        foreach (var reference in references) operations.Add((WalOperationType.upsert_reference, reference));

        await storage_engine.append_wal_batch_async(operations, ct);
    }

    /// <summary>
    ///     移除文件的所有索引数据
    /// </summary>
    public async Task remove_file(string fileUri, CancellationToken ct = default)
    {
        index_manager.remove_file(fileUri);
        invalidate_queries_for_file(fileUri);

        await storage_engine.append_wal_async(WalOperationType.remove_file, fileUri, ct);
    }

    /// <summary>
    ///     保存索引到磁盘
    /// </summary>
    public async Task save(CancellationToken ct = default)
    {
        await storage_engine.save(index_manager, ct);
    }

    /// <summary>
    ///     查找指定符号的所有引用（使用增量查询缓存）
    /// </summary>
    public IReadOnlyList<ReferenceRecord> find_references(SymbolId symbolId)
    {
        if (_registry.try_get<SymbolId, IReadOnlyList<ReferenceRecord>>("references_by_symbol", out var query))
        {
            var result = query_engine.execute(query!, symbolId);
            return result.value ?? [];
        }

        return index_manager.references.get_by_symbol_id(symbolId);
    }

    /// <summary>
    ///     获取指定文件的符号列表（使用增量查询缓存）
    /// </summary>
    public IReadOnlyList<SymbolRecord> get_file_symbols(string fileUri)
    {
        if (_registry.try_get<string, IReadOnlyList<SymbolRecord>>("symbols_by_file", out var query))
        {
            var result = query_engine.execute(query!, fileUri);
            return result.value ?? [];
        }

        return index_manager.symbols.get_by_file_uri(fileUri);
    }

    /// <summary>
    ///     获取指定文件的依赖列表（使用增量查询缓存）
    /// </summary>
    public IReadOnlySet<string> get_file_dependencies(string fileUri)
    {
        if (_registry.try_get<string, IReadOnlySet<string>>("file_dependencies", out var query))
        {
            var result = query_engine.execute(query!, fileUri);
            return result.value ?? new HashSet<string>();
        }

        return index_manager.files.get_dependencies(fileUri);
    }

    /// <summary>
    ///     获取指定文件的所有受影响方（传递反向依赖）
    /// </summary>
    public IReadOnlySet<string> get_file_dependents(string fileUri)
    {
        if (_registry.try_get<string, IReadOnlySet<string>>("file_dependents", out var query))
        {
            var result = query_engine.execute(query!, fileUri);
            return result.value ?? new HashSet<string>();
        }

        return index_manager.files.get_transitive_dependents(fileUri);
    }

    private void register_queries()
    {
        _registry.register(new SymbolsByFileQuery(index_manager));
        _registry.register(new ReferencesBySymbolQuery(index_manager));
        _registry.register(new FileDependenciesQuery(index_manager));
        _registry.register(new FileDependentsQuery(index_manager));
    }

    private void invalidate_queries_for_file(string fileUri)
    {
        if (_registry.try_get<string, IReadOnlyList<SymbolRecord>>("symbols_by_file", out var symbolQuery))
            query_engine.invalidate(symbolQuery!, fileUri);

        if (_registry.try_get<string, IReadOnlySet<string>>("file_dependencies", out var depQuery))
            query_engine.invalidate(depQuery!, fileUri);

        if (_registry.try_get<string, IReadOnlySet<string>>("file_dependents", out var depQuery2))
            query_engine.invalidate(depQuery2!, fileUri);
    }
}