using Std.Data.Text.Syntax;

namespace Std.Data.Text.Diagnostics;

/// <summary>
///     诊断来源信息。
/// </summary>
public readonly record struct DiagnosticSource
{
    public string? file_path { get; init; }
    public SourceSpan source_span { get; init; }
    public string[] hints { get; init; }
    public DiagnosticRelatedSpan[] related_spans { get; init; }
    
    /// <summary>
    ///     诊断来源信息。
    /// </summary>
    public DiagnosticSource(string? file_path = null,
        SourceSpan source_span = default,
        string[]? hints = null,
        DiagnosticRelatedSpan[]? related_spans = null)
    {
        this.file_path = file_path;
        this.source_span = source_span;
        this.hints = hints ?? [];
        this.related_spans = related_spans??[];
    }
}