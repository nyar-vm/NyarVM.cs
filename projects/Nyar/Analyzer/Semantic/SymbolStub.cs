using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Semantic;

public readonly struct SymbolStub
{
    public string name { get; }
    public SymbolKind kind { get; }
    public SymbolAccessibility accessibility { get; }
    public TextSpan span { get; }
    public SourceSpan source_span { get; }
    public string? type_name { get; }
    public string? file_path { get; }

    public SymbolStub(string name, SymbolKind kind, SymbolAccessibility accessibility, TextSpan span,
        SourceSpan sourceSpan = default, string? typeName = null, string? filePath = null)
    {
        this.name = name;
        this.kind = kind;
        this.accessibility = accessibility;
        this.span = span;
        source_span = sourceSpan;
        type_name = typeName;
        file_path = filePath;
    }
}