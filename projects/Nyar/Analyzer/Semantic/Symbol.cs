using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Semantic;

public sealed class Symbol : ISymbol
{
    public Symbol(
        string name,
        SymbolKind kind,
        SymbolAccessibility accessibility = SymbolAccessibility.@private,
        IType? type = null,
        IScope? containingScope = null,
        bool isStatic = false,
        bool isReadOnly = false,
        bool isAbstract = false,
        bool isSealed = false,
        TextSpan definitionSpan = default,
        SourceSpan definitionSourceSpan = default,
        string? filePath = null)
    {
        this.name = name;
        this.kind = kind;
        this.accessibility = accessibility;
        this.type = type;
        containing_scope = containingScope;
        is_static = isStatic;
        is_read_only = isReadOnly;
        is_abstract = isAbstract;
        is_sealed = isSealed;
        definition_span = definitionSpan;
        definition_source_span = definitionSourceSpan;
        file_path = filePath;
    }

    public TextSpan definition_span { get; }
    public SourceSpan definition_source_span { get; }
    public string? file_path { get; }

    public bool has_source_position => definition_source_span.start_line > 0;
    public string name { get; }
    public SymbolKind kind { get; }
    public SymbolAccessibility accessibility { get; }
    public IType? type { get; internal set; }
    public IScope? containing_scope { get; internal set; }
    public bool is_static { get; }
    public bool is_read_only { get; }
    public bool is_abstract { get; }
    public bool is_sealed { get; }

    public override string ToString()
    {
        return $"{kind} {name}: {type?.name ?? "?"}";
    }
}