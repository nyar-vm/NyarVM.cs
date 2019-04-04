using Nyar.Analyzer.Semantic;
using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Database.Integration;
using Std.Data.Text.Syntax;

namespace Nyar.Analyzer.Workspace;

public readonly struct EditTransaction
{
    public string file_path { get; }
    public Edit edit { get; }
    public int version { get; }

    public EditTransaction(string filePath, Edit edit, int version)
    {
        file_path = filePath;
        this.edit = edit;
        this.version = version;
    }
}

public delegate SemanticModel? SemanticModelBuilder(string filePath, ISource source);

public class Workspace : IAsyncDisposable
{
    private readonly List<Action<EditTransaction>> _edit_listeners;
    private readonly Dictionary<string, VirtualFile> _files;
    private readonly List<SemanticModelBuilder> _model_builders;
    private readonly Dictionary<string, Project> _projects;
    private readonly Dictionary<string, SemanticModel> _semantic_models;
    private bool _disposed;

    public Workspace(string rootPath)
    {
        root_path = rootPath;
        _files = new Dictionary<string, VirtualFile>();
        _projects = new Dictionary<string, Project>();
        _semantic_models = new Dictionary<string, SemanticModel>();
        index = new SemanticIndex();
        _edit_listeners = [];
        _model_builders = [];
    }

    // TODO: 待实现 - SymbolAnalysisEngine 单参数构造函数已移除，需要 IndexManager 和 IStorageEngine
    // public Workspace(string rootPath, string databasePath) : this(rootPath)
    // {
    //     _symbolEngine = new SymbolAnalysisEngine(databasePath);
    // }

    public string root_path { get; }
    public IReadOnlyDictionary<string, VirtualFile> files => _files;
    public IReadOnlyDictionary<string, Project> projects => _projects;
    public SemanticIndex index { get; }

    public SymbolAnalysisEngine? symbol_engine { get; }

    public bool has_persistent_index => symbol_engine is not null;

    #region 内部实现

    private void try_build_semantic_model(string filePath, ISource source)
    {
        foreach (var builder in _model_builders)
        {
            var model = builder(filePath, source);
            if (model is not null)
            {
                update_semantic_model(filePath, model);
                return;
            }
        }
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (symbol_engine is not null)
        {
            await symbol_engine.save();
            await symbol_engine.DisposeAsync();
        }
    }

    /// <summary>
    ///     异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await dispose();
    }

    #endregion

    #region 持久化索引

    /// <summary>
    ///     快速冷启动：仅加载文件索引，符号和引用按需加载
    /// </summary>
    public async Task fast_restore(CancellationToken ct = default)
    {
        if (symbol_engine is null) return;

        await symbol_engine.fast_restore(ct);
    }

    /// <summary>
    ///     全量恢复：加载所有索引数据
    /// </summary>
    public async Task full_restore(CancellationToken ct = default)
    {
        if (symbol_engine is null) return;

        await symbol_engine.full_restore(ct);

        foreach (var fileUri in _files.Keys) await symbol_engine.load_file(fileUri, ct);
    }

    /// <summary>
    ///     保存索引到磁盘
    /// </summary>
    public async Task save_index(CancellationToken ct = default)
    {
        if (symbol_engine is null) return;

        await symbol_engine.save(ct);
    }

    /// <summary>
    ///     查找指定符号的所有引用（使用持久化索引）
    /// </summary>
    public IReadOnlyList<ReferenceRecord> find_symbol_references(SymbolId symbolId)
    {
        if (symbol_engine is not null) return symbol_engine.find_references(symbolId);

        return [];
    }

    /// <summary>
    ///     获取指定文件的符号列表（使用持久化索引）
    /// </summary>
    public IReadOnlyList<SymbolRecord> get_file_symbols_from_index(string fileUri)
    {
        if (symbol_engine is not null) return symbol_engine.get_file_symbols(fileUri);

        return [];
    }

    /// <summary>
    ///     获取指定文件的依赖列表（使用持久化索引）
    /// </summary>
    public IReadOnlySet<string> get_file_dependencies_from_index(string fileUri)
    {
        if (symbol_engine is not null) return symbol_engine.get_file_dependencies(fileUri);

        return new HashSet<string>();
    }

    /// <summary>
    ///     获取指定文件的所有受影响方（传递反向依赖）
    /// </summary>
    public IReadOnlySet<string> get_file_dependents_from_index(string fileUri)
    {
        if (symbol_engine is not null) return symbol_engine.get_file_dependents(fileUri);

        return new HashSet<string>();
    }

    #endregion

    #region 文件管理

    public VirtualFile add_file(string filePath, string languageId, ISource source)
    {
        var file = new VirtualFile(filePath, languageId, source);
        _files[filePath] = file;

        try_build_semantic_model(filePath, source);

        return file;
    }

    public void remove_file(string filePath)
    {
        _files.Remove(filePath);
        _semantic_models.Remove(filePath);
        index.remove_file(filePath);
    }

    public VirtualFile? get_file(string filePath)
    {
        return _files.GetValueOrDefault(filePath);
    }

    #endregion

    #region 项目管理

    public Project add_project(string name, string rootPath)
    {
        var project = new Project(name, rootPath);
        _projects[name] = project;
        return project;
    }

    public Project? get_project(string name)
    {
        return _projects.GetValueOrDefault(name);
    }

    #endregion

    #region 编辑管理

    public void apply_change(EditTransaction transaction)
    {
        if (_files.TryGetValue(transaction.file_path, out var file)) file.apply_edit(transaction.edit);

        foreach (var listener in _edit_listeners) listener(transaction);
    }

    public void OnEdit(Action<EditTransaction> listener)
    {
        _edit_listeners.Add(listener);
    }

    #endregion

    #region 语义模型管理

    public SemanticModel? get_semantic_model(string filePath)
    {
        return _semantic_models.GetValueOrDefault(filePath);
    }

    public void update_semantic_model(string filePath, SemanticModel model)
    {
        _semantic_models[filePath] = model;
        index.index_file_symbols(filePath, model);

        foreach (var symbol in model.get_all_declared_symbols())
        {
            if (symbol is Symbol { has_source_position: true } concreteSymbol)
                index.add_definition(symbol.name,
                    concreteSymbol.file_path ?? filePath,
                    concreteSymbol.definition_source_span);

            index.add_global_symbol(symbol);
        }
    }

    public void register_model_builder(SemanticModelBuilder builder)
    {
        _model_builders.Add(builder);
    }

    public void invalidate_file(string filePath)
    {
        _semantic_models.Remove(filePath);
    }

    #endregion
}