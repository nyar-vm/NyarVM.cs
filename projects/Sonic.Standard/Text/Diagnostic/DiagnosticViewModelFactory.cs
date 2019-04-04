using Std.Data.Text.Syntax;
using SourceSpan = Std.Data.Text.Syntax.SourceSpan;

namespace Std.Data.Text.Diagnostics;

/// <summary>
///     将原始诊断归一化为渲染视图模型。
/// </summary>
internal static class DiagnosticViewModelFactory
{
    private const int SnippetContextLineCount = 1;

    private readonly record struct ResolvedRelatedAnnotation(
        DiagnosticLocationViewModel location,
        string label);

    private readonly record struct ResolvedRelatedInformation(
        DiagnosticLocationViewModel location,
        string label);

    #region 创建入口

    /// <summary>
    ///     为 `check` 阶段的诊断创建视图模型。
    /// </summary>
    public static DiagnosticViewModel create_check_view_model(
        Diagnostic diagnostic,
        DiagnosticRenderOptions options,
        string projectDir)
    {
        var message = diagnostic.message.Trim();
        var location = DiagnosticSourceResolver.resolve_check_location(diagnostic, projectDir);
        resolve_related_entries(
            diagnostic.related_spans,
            location,
            projectDir,
            out var relatedAnnotations,
            out var relatedInformation);
        var sourceLines = DiagnosticSourceResolver.resolve_source_lines(
            create_snippet_locations(location, relatedAnnotations),
            null,
            SnippetContextLineCount);
        var annotations = create_annotations(location, message, relatedAnnotations);

        return new DiagnosticViewModel(
            diagnostic.severity.is_error_level(),
            diagnostic.severity,
            format_severity_label(options.language, diagnostic.severity),
            normalize_diagnostic_code(diagnostic.severity, diagnostic.code, options.digits),
            message,
            location,
            sourceLines,
            annotations,
            merge_hints(diagnostic.hints),
            create_related_information(relatedInformation));
    }

    /// <summary>
    ///     为 `lint` 阶段的诊断创建视图模型。
    /// </summary>
    public static DiagnosticViewModel create_lint_view_model(
        string filePath,
        string source,
        Diagnostic diagnostic,
        DiagnosticRenderOptions options,
        string projectDir)
    {
        var message = diagnostic.message.Trim();
        var location = DiagnosticSourceResolver.resolve_lint_location(filePath, source, diagnostic, projectDir);
        resolve_related_entries(
            diagnostic.related_spans,
            location,
            projectDir,
            out var relatedAnnotations,
            out var relatedInformation);
        var sourceLines = DiagnosticSourceResolver.resolve_source_lines(
            create_snippet_locations(location, relatedAnnotations),
            source,
            SnippetContextLineCount);
        var annotations = create_annotations(location, message, relatedAnnotations);

        return new DiagnosticViewModel(
            diagnostic.severity.is_error_level(),
            diagnostic.severity,
            format_severity_label(options.language, diagnostic.severity),
            normalize_diagnostic_code(diagnostic.severity, diagnostic.code, options.digits),
            message,
            location,
            sourceLines,
            annotations,
            merge_hints(diagnostic.hints, "tool: lint"),
            create_related_information(relatedInformation));
    }

    private static DiagnosticAnnotationViewModel[] create_annotations(
        DiagnosticLocationViewModel location,
        string label,
        IReadOnlyList<ResolvedRelatedAnnotation> relatedAnnotations)
    {
        var annotations = new List<DiagnosticAnnotationViewModel>();
        if (location.has_precise_span)
        {
            annotations.Add(create_annotation(
                DiagnosticAnnotationKind.Primary,
                location,
                label));
        }

        foreach (var relatedAnnotation in relatedAnnotations)
        {
            annotations.Add(create_annotation(
                DiagnosticAnnotationKind.Secondary,
                relatedAnnotation.location,
                relatedAnnotation.label));
        }

        return [.. annotations];
    }

    private static DiagnosticLocationViewModel[] create_snippet_locations(
        DiagnosticLocationViewModel primaryLocation,
        IReadOnlyList<ResolvedRelatedAnnotation> relatedAnnotations)
    {
        var locations = new List<DiagnosticLocationViewModel> { primaryLocation };
        foreach (var relatedAnnotation in relatedAnnotations)
        {
            if (locations.Any(existing => locations_equal(existing, relatedAnnotation.location)))
            {
                continue;
            }

            locations.Add(relatedAnnotation.location);
        }

        return [.. locations];
    }

    private static void resolve_related_entries(
        DiagnosticRelatedSpan[]? relatedSpans,
        DiagnosticLocationViewModel primaryLocation,
        string projectDir,
        out List<ResolvedRelatedAnnotation> relatedAnnotations,
        out List<ResolvedRelatedInformation> relatedInformation)
    {
        relatedAnnotations = [];
        relatedInformation = [];
        if (relatedSpans is null || relatedSpans.Length == 0)
        {
            return;
        }

        foreach (var relatedSpan in relatedSpans)
        {
            var sourceSpan = to_source_span(relatedSpan, primaryLocation.source_file_path);
            if (sourceSpan.start_line <= 0 || sourceSpan.start_column <= 0)
            {
                continue;
            }

            var location = DiagnosticSourceResolver.resolve_source_span_location(sourceSpan, projectDir);
            if (!string.Equals(
                    location.source_file_path,
                    primaryLocation.source_file_path,
                    StringComparison.OrdinalIgnoreCase))
            {
                add_related_information(
                    relatedInformation,
                    new ResolvedRelatedInformation(
                        location,
                        normalize_annotation_label(relatedSpan.label)));
                continue;
            }

            if (locations_equal(location, primaryLocation))
            {
                continue;
            }

            add_related_annotation(
                relatedAnnotations,
                new ResolvedRelatedAnnotation(
                    location,
                    normalize_annotation_label(relatedSpan.label)));
        }

        sort_related_annotations(relatedAnnotations);
        sort_related_information(relatedInformation);
    }

    private static DiagnosticRelatedInformationViewModel[]? create_related_information(
        IReadOnlyList<ResolvedRelatedInformation> relatedInformation)
    {
        if (relatedInformation.Count == 0)
        {
            return null;
        }

        return
        [
            .. relatedInformation
                .Select(info => new DiagnosticRelatedInformationViewModel(info.location, info.label))
        ];
    }

    private static SourceSpan to_source_span(
        DiagnosticRelatedSpan relatedSpan,
        string fallbackFilePath)
    {
        if (!string.IsNullOrWhiteSpace(relatedSpan.file_path))
        {
            return relatedSpan.source_span with
            {
                file_path = relatedSpan.file_path
            };
        }

        if (!string.IsNullOrWhiteSpace(relatedSpan.source_span.file_path))
        {
            return relatedSpan.source_span;
        }

        return relatedSpan.source_span with
        {
            file_path = fallbackFilePath
        };
    }

    private static DiagnosticAnnotationViewModel create_annotation(
        DiagnosticAnnotationKind kind,
        DiagnosticLocationViewModel location,
        string label)
    {
        var startLine = System.Math.Max(location.start_line, 1);
        var startColumn = System.Math.Max(location.start_column, 1);
        var endLine = System.Math.Max(location.end_line, startLine);
        var endColumn = System.Math.Max(location.end_column, 1);

        if (endLine == startLine && endColumn <= startColumn)
        {
            endColumn = startColumn + 1;
        }

        return new DiagnosticAnnotationViewModel(
            kind,
            startLine,
            startColumn,
            endLine,
            endColumn,
            normalize_annotation_label(label));
    }

    private static string normalize_annotation_label(string label)
    {
        return label.Trim();
    }

    private static void add_related_annotation(
        List<ResolvedRelatedAnnotation> relatedAnnotations,
        ResolvedRelatedAnnotation annotation)
    {
        if (relatedAnnotations.Any(existing =>
                locations_equal(existing.location, annotation.location) &&
                string.Equals(existing.label, annotation.label, StringComparison.Ordinal)))
        {
            return;
        }

        relatedAnnotations.Add(annotation);
    }

    private static void add_related_information(
        List<ResolvedRelatedInformation> relatedInformation,
        ResolvedRelatedInformation information)
    {
        if (relatedInformation.Any(existing =>
                locations_equal(existing.location, information.location) &&
                string.Equals(existing.label, information.label, StringComparison.Ordinal)))
        {
            return;
        }

        relatedInformation.Add(information);
    }

    private static void sort_related_annotations(List<ResolvedRelatedAnnotation> relatedAnnotations)
    {
        relatedAnnotations.Sort((left, right) =>
        {
            var locationComparison = compare_locations(left.location, right.location);
            if (locationComparison != 0)
            {
                return locationComparison;
            }

            return StringComparer.Ordinal.Compare(left.label, right.label);
        });
    }

    private static void sort_related_information(List<ResolvedRelatedInformation> relatedInformation)
    {
        relatedInformation.Sort((left, right) =>
        {
            var locationComparison = compare_locations(left.location, right.location);
            if (locationComparison != 0)
            {
                return locationComparison;
            }

            return StringComparer.Ordinal.Compare(left.label, right.label);
        });
    }

    private static bool locations_equal(
        DiagnosticLocationViewModel left,
        DiagnosticLocationViewModel right)
    {
        return string.Equals(left.source_file_path, right.source_file_path, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.display_file_path, right.display_file_path, StringComparison.Ordinal)
               && left.start_line == right.start_line
               && left.start_column == right.start_column
               && left.end_line == right.end_line
               && left.end_column == right.end_column;
    }

    private static int compare_locations(
        DiagnosticLocationViewModel left,
        DiagnosticLocationViewModel right)
    {
        var filePathComparison = StringComparer.OrdinalIgnoreCase.Compare(left.source_file_path, right.source_file_path);
        if (filePathComparison != 0)
        {
            return filePathComparison;
        }

        var displayPathComparison = StringComparer.Ordinal.Compare(left.display_file_path, right.display_file_path);
        if (displayPathComparison != 0)
        {
            return displayPathComparison;
        }

        var startLineComparison = left.start_line.CompareTo(right.start_line);
        if (startLineComparison != 0)
        {
            return startLineComparison;
        }

        var startColumnComparison = left.start_column.CompareTo(right.start_column);
        if (startColumnComparison != 0)
        {
            return startColumnComparison;
        }

        var endLineComparison = left.end_line.CompareTo(right.end_line);
        if (endLineComparison != 0)
        {
            return endLineComparison;
        }

        return left.end_column.CompareTo(right.end_column);
    }

    #endregion

    #region 文案与编码

    /// <summary>
    ///     合并诊断自带的 hints 数组与额外补充的提示文本，过滤空白项。
    /// </summary>
    private static string[] merge_hints(string[]? existing, params string?[] additional)
    {
        var result = new List<string>();
        if (existing is not null)
        {
            result.AddRange(existing.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        result.AddRange(additional.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>());
        return [.. result];
    }

    private static string format_severity_label(
        DiagnosticLanguage language,
        DiagnosticSeverity severity)
    {
        return (language, severity) switch
        {
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.fatal) => "致命错误",
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.error) => "错误",
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.warning) => "警告",
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.info) => "信息",
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.hint) => "信息",
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.debug) => "调试",
            (DiagnosticLanguage.ZhHans, DiagnosticSeverity.trace) => "跟踪",
            (_, DiagnosticSeverity.fatal) => "fatal",
            (_, DiagnosticSeverity.error) => "error",
            (_, DiagnosticSeverity.warning) => "warning",
            (_, DiagnosticSeverity.info) => "info",
            (_, DiagnosticSeverity.hint) => "info",
            (_, DiagnosticSeverity.debug) => "debug",
            (_, DiagnosticSeverity.trace) => "trace",
            _ => "info"
        };
    }

    private static string? normalize_diagnostic_code(
        DiagnosticSeverity severity,
        int? code,
        int digits)
    {
        if (code is null)
        {
            return null;
        }

        var prefix = severity switch
        {
            DiagnosticSeverity.fatal => "F",
            DiagnosticSeverity.error => "E",
            DiagnosticSeverity.warning => "W",
            DiagnosticSeverity.info => "I",
            DiagnosticSeverity.hint => "H",
            DiagnosticSeverity.debug => "D",
            DiagnosticSeverity.trace => "T",
            _ => "I"
        };

        var normalizedDigits = System.Math.Max(digits, 1);
        return $"{prefix}{code.Value.ToString($"D{normalizedDigits}")}";
    }

    #endregion
}
