namespace Std.Data.Text.Diagnostics;

/// <summary>
///     诊断渲染使用的统一视图模型。
/// </summary>
public readonly record struct DiagnosticViewModel(
    bool is_error,
    DiagnosticSeverity severity,
    string level,
    string? display_code,
    string message,
    DiagnosticLocationViewModel location,
    IReadOnlyList<DiagnosticSourceLineViewModel> source_lines,
    IReadOnlyList<DiagnosticAnnotationViewModel> annotations,
    string[] hints,
    IReadOnlyList<DiagnosticRelatedInformationViewModel>? related_information = null)
{
    /// <summary>
    ///     获取首条源码行，兼容现阶段仍按单行消费的路径。
    /// </summary>
    public DiagnosticSourceLineViewModel? source_line
    {
        get
        {
            foreach (var sourceLine in source_lines)
            {
                if (sourceLine.line_number == location.start_line)
                {
                    return sourceLine;
                }
            }

            if (source_lines.Count > 0)
            {
                return source_lines[0];
            }

            return null;
        }
    }

    /// <summary>
    ///     获取首个主标注，兼容现阶段 renderer 仍按单标注消费的路径。
    /// </summary>
    public DiagnosticAnnotationViewModel? primary_annotation
    {
        get
        {
            foreach (var annotation in annotations)
            {
                if (annotation.kind == DiagnosticAnnotationKind.Primary)
                {
                    return annotation;
                }
            }

            return null;
        }
    }
}

/// <summary>
///     诊断关联位置信息视图模型。
/// </summary>
public readonly record struct DiagnosticRelatedInformationViewModel(
    DiagnosticLocationViewModel location,
    string label);

/// <summary>
///     诊断位置视图模型，同时保留真实路径与展示路径。
/// </summary>
public readonly record struct DiagnosticLocationViewModel(
    string source_file_path,
    string display_file_path,
    int start_line,
    int start_column,
    int end_line,
    int end_column)
{
    /// <summary>
    ///     是否包含精确的行列位置。
    /// </summary>
    public bool has_precise_span => start_line > 0 && start_column > 0;
}

/// <summary>
///     诊断源码行视图模型。
/// </summary>
public readonly record struct DiagnosticSourceLineViewModel(
    int line_number,
    string text);

/// <summary>
///     诊断标注类型。
/// </summary>
public enum DiagnosticAnnotationKind : byte
{
    Primary,
    Secondary
}

/// <summary>
///     诊断标注视图模型。
/// </summary>
public readonly record struct DiagnosticAnnotationViewModel(
    DiagnosticAnnotationKind kind,
    int start_line,
    int start_column,
    int end_line,
    int end_column,
    string label)
{
    /// <summary>
    ///     是否为单行标注。
    /// </summary>
    public bool is_single_line => start_line == end_line;

    /// <summary>
    ///     获取 caret 的起始缩进。
    /// </summary>
    public int caret_indent => System.Math.Max(start_column - 1, 0);

    /// <summary>
    ///     获取 caret 的宽度。
    /// </summary>
    public int caret_width
    {
        get
        {
            if (!is_single_line || end_column <= start_column)
            {
                return 1;
            }

            return System.Math.Max(end_column - start_column, 1);
        }
    }
}
