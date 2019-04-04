using Std.Data.Text.Diagnostics;

namespace Nyar.Analyzer.Semantic;

public class SemanticModel
{
    private readonly Dictionary<SemanticNamePath, DataAttributeInfo> _data_types;
    private readonly List<SemanticDiagnostic> _diagnostics;
    private readonly Dictionary<int, DataAttributeInfo> _node_to_data_type;
    private readonly Dictionary<int, ISymbol> _node_to_symbol;
    private readonly Dictionary<int, IType> _node_to_type;

    public SemanticModel(string filePath, ISymbolTable symbolTable)
    {
        file_path = filePath;
        symbols = symbolTable;
        _diagnostics = [];
        _data_types = [];
        _node_to_data_type = new Dictionary<int, DataAttributeInfo>();
        _node_to_symbol = new Dictionary<int, ISymbol>();
        _node_to_type = new Dictionary<int, IType>();
        has_errors = false;
    }

    public string file_path { get; }
    public ISymbolTable symbols { get; }

    public IReadOnlyList<SemanticDiagnostic> diagnostics => _diagnostics;
    public bool has_errors { get; private set; }

    #region 符号解析

    public ISymbol? resolve_symbol(string name)
    {
        return symbols.resolve(name);
    }

    public IType? get_symbol_type(ISymbol symbol)
    {
        return symbol.type;
    }

    public IReadOnlyList<ISymbol> find_references(ISymbol symbol)
    {
        return symbols.find_references(symbol);
    }

    #endregion

    #region AST 节点绑定

    public ISymbol? get_declared_symbol(int nodeId)
    {
        return _node_to_symbol.GetValueOrDefault(nodeId);
    }

    public IType? get_type_info(int nodeId)
    {
        return _node_to_type.GetValueOrDefault(nodeId);
    }

    public void bind_symbol(int nodeId, ISymbol symbol)
    {
        _node_to_symbol[nodeId] = symbol;
    }

    public void bind_type(int nodeId, IType type)
    {
        _node_to_type[nodeId] = type;
    }

    public ISymbol? find_symbol_at_position(int position)
    {
        foreach (var symbol in _node_to_symbol.Values)
            if (symbol is Symbol concreteSymbol)
            {
                var span = concreteSymbol.definition_span;
                if (position >= span.start && position <= span.end) return symbol;
            }

        return null;
    }

    public ISymbol? find_symbol_by_name(string name)
    {
        return symbols.resolve(name);
    }

    internal IEnumerable<KeyValuePair<int, ISymbol>> enumerate_symbol_bindings()
    {
        return _node_to_symbol;
    }

    internal IEnumerable<KeyValuePair<int, IType>> enumerate_type_bindings()
    {
        return _node_to_type;
    }

    public IReadOnlyList<ISymbol> get_all_declared_symbols()
    {
        return new List<ISymbol>(_node_to_symbol.Values);
    }

    #endregion

    #region 数据元信息

    public void bind_data_type(int nodeId, DataAttributeInfo dataType)
    {
        bind_data_type(nodeId, dataType, SemanticNamePath.empty);
    }

    /// <summary>
    ///     绑定结构化数据类型名称。
    /// </summary>
    public void bind_data_type(int nodeId, DataAttributeInfo dataType, SemanticNamePath qualifiedName)
    {
        _node_to_data_type[nodeId] = dataType;
        if (!qualifiedName.is_empty) _data_types[qualifiedName] = dataType;
    }

    public DataAttributeInfo? get_data_type(int nodeId)
    {
        return _node_to_data_type.GetValueOrDefault(nodeId);
    }

    public DataAttributeInfo? find_data_type(SemanticNamePath qualifiedName)
    {
        return _data_types.GetValueOrDefault(qualifiedName);
    }

    public IReadOnlyList<DataAttributeInfo> get_all_data_types()
    {
        return new List<DataAttributeInfo>(_data_types.Values);
    }

    #endregion

    #region 诊断管理

    public void add_diagnostic(SemanticDiagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
        if (diagnostic.level == DiagnosticSeverity.error) has_errors = true;
    }

    public void add_diagnostics(IEnumerable<SemanticDiagnostic> diagnostics)
    {
        foreach (var diag in diagnostics) add_diagnostic(diag);
    }

    public void clear_diagnostics()
    {
        _diagnostics.Clear();
        has_errors = false;
    }

    #endregion
}