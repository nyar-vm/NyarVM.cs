using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Diagnostics;

public readonly record struct Diagnostic(
    TextSpan span,
    string message,
    DiagnosticSeverity severity,
    int? code = null,
    DiagnosticSource source = default)
{
    public Diagnostic(
        TextSpan span,
        string message,
        DiagnosticSeverity severity,
        int? code,
        string? file_path,
        SourceSpan source_span,
        string[]? hints,
        DiagnosticRelatedSpan[]? related_spans = null)
        : this(
            span,
            message,
            severity,
            code,
            new DiagnosticSource(file_path, source_span, hints, related_spans))
    {
    }

    public string? file_path => source.file_path;

    public SourceSpan source_span => source.source_span;

    public string[]? hints => source.hints;

    public DiagnosticRelatedSpan[]? related_spans => source.related_spans;
}

/// <summary>
///     诊断关联标注描述，用于生成 secondary annotation。
/// </summary>
public readonly record struct DiagnosticRelatedSpan(
    SourceSpan source_span,
    string label,
    string? file_path = null);
