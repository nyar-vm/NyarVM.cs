using Std.Data.Text.Syntax;

namespace Nyar.Analyzer.Semantic;

public class SemanticIndex
{
    private readonly Dictionary<string, List<SymbolStub>> _file_stubs;
    private readonly Dictionary<string, Dictionary<string, ISymbol>> _file_symbols;
    private readonly Dictionary<string, ISymbol> _global_symbols;
    private readonly Dictionary<string, List<ReferenceEntry>> _references;

    public SemanticIndex()
    {
        _file_stubs = new Dictionary<string, List<SymbolStub>>();
        _global_symbols = new Dictionary<string, ISymbol>();
        _references = new Dictionary<string, List<ReferenceEntry>>();
        _file_symbols = new Dictionary<string, Dictionary<string, ISymbol>>();
    }

    public void clear()
    {
        _file_stubs.Clear();
        _global_symbols.Clear();
        _references.Clear();
        _file_symbols.Clear();
    }

    #region 文件级存根管理

    public void index_file(string filePath, IReadOnlyList<SymbolStub> stubs)
    {
        _file_stubs[filePath] = [.. stubs];
    }

    public void remove_file(string filePath)
    {
        _file_stubs.Remove(filePath);
        _file_symbols.Remove(filePath);

        var toRemove = new List<ReferenceEntry>();
        foreach (var entry in _references) entry.Value.RemoveAll(r => r.file_path == filePath);
    }

    public IReadOnlyList<SymbolStub> get_file_stubs(string filePath)
    {
        return _file_stubs.TryGetValue(filePath, out var stubs) ? stubs : [];
    }

    #endregion

    #region 全局符号管理

    public void add_global_symbol(ISymbol symbol)
    {
        _global_symbols[symbol.name] = symbol;
    }

    public ISymbol? resolve_global(string name)
    {
        return _global_symbols.GetValueOrDefault(name);
    }

    #endregion

    #region 文件级符号管理

    public void index_file_symbols(string filePath, SemanticModel model)
    {
        var symbols = new Dictionary<string, ISymbol>();
        foreach (var symbol in model.get_all_declared_symbols()) symbols[symbol.name] = symbol;

        _file_symbols[filePath] = symbols;
    }

    public ISymbol? resolve_in_file(string filePath, string name)
    {
        if (_file_symbols.TryGetValue(filePath, out var symbols)) return symbols.GetValueOrDefault(name);

        return null;
    }

    #endregion

    #region 引用追踪

    public void add_reference(string symbolName, string filePath, SourceSpan sourceSpan, bool isDefinition = false)
    {
        if (!_references.TryGetValue(symbolName, out var refs))
        {
            refs = [];
            _references[symbolName] = refs;
        }

        refs.Add(new ReferenceEntry(symbolName, filePath, sourceSpan, isDefinition));
    }

    public void add_definition(string symbolName, string filePath, SourceSpan sourceSpan)
    {
        add_reference(symbolName, filePath, sourceSpan, true);
    }

    public IReadOnlyList<ReferenceEntry> find_all_references(string name)
    {
        var results = new List<ReferenceEntry>();

        if (_references.TryGetValue(name, out var refs)) results.AddRange(refs);

        if (_global_symbols.TryGetValue(name, out var globalSymbol))
            if (globalSymbol is Symbol { has_source_position: true } concreteSymbol)
            {
                var alreadyContains = results.Exists(r =>
                    r.is_definition && r.file_path == concreteSymbol.file_path &&
                    r.source_span.start_line == concreteSymbol.definition_source_span.start_line);

                if (!alreadyContains)
                    results.Add(new ReferenceEntry(name,
                        concreteSymbol.file_path ?? "",
                        concreteSymbol.definition_source_span,
                        true));
            }

        return results;
    }

    public IReadOnlyList<ReferenceEntry> find_definitions(string name)
    {
        var results = new List<ReferenceEntry>();

        if (_references.TryGetValue(name, out var refs))
            foreach (var entry in refs)
                if (entry.is_definition)
                    results.Add(entry);

        return results;
    }

    #endregion
}