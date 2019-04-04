using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Semantic;

public readonly struct SemanticDiagnostic
{
    public DiagnosticSeverity level { get; }
    public string message { get; }
    public TextSpan span { get; }
    public SourceSpan source_span { get; }
    public string? code { get; }
    public string? file_path { get; }

    public SemanticDiagnostic(DiagnosticSeverity level, string message, TextSpan span, string? code = null,
        SourceSpan sourceSpan = default, string? filePath = null)
    {
        this.level = level;
        this.message = message;
        this.span = span;
        this.code = code;
        source_span = sourceSpan;
        file_path = filePath;
    }

    public SemanticDiagnostic(DiagnosticSeverity level, string message, SourceSpan sourceSpan, string? code = null,
        string? filePath = null)
    {
        this.level = level;
        this.message = message;
        span = default;
        this.code = code;
        source_span = sourceSpan;
        file_path = filePath;
    }

    public bool has_source_position => source_span.start_line > 0;

    public override string ToString()
    {
        var location = has_source_position
            ? source_span.file_path is not null
                ? $"{source_span.file_path}:({source_span.start_line},{source_span.start_column})"
                : $"({source_span.start_line},{source_span.start_column})"
            : span.length > 0
                ? span.ToString()
                : "";

        return code is not null
            ? $"[{level}] {code}: {message} @ {location}"
            : $"[{level}] {message} @ {location}";
    }
}