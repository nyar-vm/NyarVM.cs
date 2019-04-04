using Std.Data.Text.Syntax;

namespace Nyar.Analyzer.Semantic;

public readonly struct ReferenceEntry
{
    public string symbol_name { get; }
    public string file_path { get; }
    public SourceSpan source_span { get; }
    public bool is_definition { get; }

    public ReferenceEntry(string symbolName, string filePath, SourceSpan sourceSpan, bool isDefinition = false)
    {
        symbol_name = symbolName;
        file_path = filePath;
        source_span = sourceSpan;
        is_definition = isDefinition;
    }
}