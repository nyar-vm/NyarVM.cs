using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Diagnostics;

/// <summary>
///     负责诊断位置与源码行的解析。
/// </summary>
internal static class DiagnosticSourceResolver
{
    private const string UnknownPath = "<unknown>";

    #region 位置解析

    /// <summary>
    ///     解析 `check` 阶段诊断的位置信息。
    /// </summary>
    public static DiagnosticLocationViewModel resolve_check_location(Diagnostic diagnostic, string projectDir)
    {
        if (diagnostic.source_span.start_line > 0)
        {
            return to_location_view_model(diagnostic.source_span, projectDir);
        }

        if (!string.IsNullOrWhiteSpace(diagnostic.file_path))
        {
            var resolved = try_build_location_from_file(diagnostic.file_path, diagnostic.span, projectDir);
            if (resolved is not null)
            {
                return resolved.Value;
            }

            return create_fallback_location(diagnostic.file_path, projectDir);
        }

        return create_fallback_location(null, projectDir);
    }

    /// <summary>
    ///     解析 `lint` 阶段诊断的位置信息。
    /// </summary>
    public static DiagnosticLocationViewModel resolve_lint_location(
        string filePath,
        string source,
        Diagnostic diagnostic,
        string projectDir)
    {
        if (diagnostic.source_span.start_line > 0)
        {
            return to_location_view_model(diagnostic.source_span, projectDir);
        }

        return try_build_location_from_text_span(filePath, source, diagnostic.span, projectDir)
            ?? create_fallback_location(filePath, projectDir);
    }

    /// <summary>
    ///     创建兜底位置，保留真实路径与展示路径。
    /// </summary>
    public static DiagnosticLocationViewModel create_fallback_location(string? filePath, string projectDir)
    {
        var sourceFilePath = string.IsNullOrWhiteSpace(filePath) ? UnknownPath : filePath;
        return new DiagnosticLocationViewModel(
            sourceFilePath,
            normalize_display_path(filePath, projectDir),
            0,
            0,
            0,
            0);
    }

    /// <summary>
    ///     直接将源码区间转换为位置视图模型。
    /// </summary>
    public static DiagnosticLocationViewModel resolve_source_span_location(SourceSpan sourceSpan, string projectDir)
    {
        return to_location_view_model(sourceSpan, projectDir);
    }

    private static DiagnosticLocationViewModel? try_build_location_from_file(
        string? filePath,
        TextSpan span,
        string projectDir)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        return try_build_location_from_text_span(filePath, File.ReadAllText(filePath), span, projectDir);
    }

    /// <summary>
    ///     将偏移区间转换为行列坐标。
    /// </summary>
    public static DiagnosticLocationViewModel? try_build_location_from_text_span(
        string filePath,
        string source,
        TextSpan span,
        string projectDir)
    {
        if (span.start < 0 || span.start > source.Length)
        {
            return null;
        }

        var start = to_line_column(source, span.start);
        var safeEnd = System.Math.Clamp(span.end, span.start, source.Length);
        var end = to_line_column(source, safeEnd);
        return new DiagnosticLocationViewModel(
            filePath,
            normalize_display_path(filePath, projectDir),
            start.line,
            start.column,
            end.line,
            end.column);
    }

    private static DiagnosticLocationViewModel to_location_view_model(SourceSpan sourceSpan, string projectDir)
    {
        return new DiagnosticLocationViewModel(
            string.IsNullOrWhiteSpace(sourceSpan.file_path) ? UnknownPath : sourceSpan.file_path,
            normalize_display_path(sourceSpan.file_path, projectDir),
            sourceSpan.start_line,
            sourceSpan.start_column,
            sourceSpan.end_line,
            sourceSpan.end_column);
    }

    private static (int line, int column) to_line_column(string source, int offset)
    {
        var line = 1;
        var column = 1;
        for (var i = 0; i < offset; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    private static string normalize_display_path(string? filePath, string projectDir)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return UnknownPath;
        }

        try
        {
            return Path.GetRelativePath(projectDir, filePath);
        }
        catch
        {
            return filePath;
        }
    }

    #endregion

    #region 源码解析

    /// <summary>
    ///     解析当前诊断关联的源码行集合。
    /// </summary>
    public static DiagnosticSourceLineViewModel[] resolve_source_lines(
        DiagnosticLocationViewModel location,
        string? inlineSource,
        int contextLineCount = 0)
    {
        return resolve_source_lines([location], inlineSource, contextLineCount);
    }

    /// <summary>
    ///     解析多个诊断位置关联的源码行集合，并按上下文窗口合并。
    /// </summary>
    public static DiagnosticSourceLineViewModel[] resolve_source_lines(
        IReadOnlyList<DiagnosticLocationViewModel> locations,
        string? inlineSource,
        int contextLineCount = 0)
    {
        if (locations.Count == 0)
        {
            return [];
        }

        var preciseLocations = locations
            .Where(location => location.has_precise_span)
            .OrderBy(location => location.start_line)
            .ThenBy(location => location.start_column)
            .ToArray();
        if (preciseLocations.Length == 0)
        {
            return [];
        }

        var sourceFilePath = preciseLocations[0].source_file_path;
        var sameFileLocations = preciseLocations
            .Where(location => string.Equals(location.source_file_path, sourceFilePath, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var windows = merge_line_windows(sameFileLocations, contextLineCount);
        if (windows.Count == 0)
        {
            return [];
        }

        if (!string.IsNullOrEmpty(inlineSource))
        {
            var inlineLines = try_get_lines_from_text(inlineSource, windows);
            if (inlineLines.Length > 0)
            {
                return inlineLines;
            }
        }

        if (!File.Exists(sourceFilePath))
        {
            return [];
        }

        var lines = File.ReadAllLines(sourceFilePath);
        var fileLines = try_get_lines_from_array(lines, windows);
        if (fileLines.Length == 0)
        {
            return [];
        }

        return fileLines;
    }

    /// <summary>
    ///     解析当前诊断所在的首条源码行。
    /// </summary>
    public static DiagnosticSourceLineViewModel? resolve_source_line(
        DiagnosticLocationViewModel location,
        string? inlineSource)
    {
        var lines = resolve_source_lines(location, inlineSource);
        if (lines.Length == 0)
        {
            return null;
        }

        foreach (var line in lines)
        {
            if (line.line_number == location.start_line)
            {
                return line;
            }
        }

        return lines[0];
    }

    private static DiagnosticSourceLineViewModel[] try_get_lines_from_text(
        string source,
        IReadOnlyList<(int startLine, int endLine)> windows)
    {
        if (windows.Count == 0)
        {
            return [];
        }

        var allLines = source.Replace("\r\n", "\n").Split('\n');
        if (allLines.Length > 0 && allLines[^1] == string.Empty)
        {
            Array.Resize(ref allLines, allLines.Length - 1);
        }

        return try_get_lines_from_array(allLines, windows);
    }

    private static DiagnosticSourceLineViewModel[] try_get_lines_from_array(
        string[] lines,
        IReadOnlyList<(int startLine, int endLine)> windows)
    {
        if (lines.Length == 0 || windows.Count == 0)
        {
            return [];
        }

        var result = new List<DiagnosticSourceLineViewModel>();
        foreach (var (startLine, endLine) in windows)
        {
            if (startLine <= 0 || endLine < startLine || startLine > lines.Length)
            {
                continue;
            }

            var safeEndLine = System.Math.Min(endLine, lines.Length);
            for (var lineNumber = startLine; lineNumber <= safeEndLine; lineNumber++)
            {
                result.Add(new DiagnosticSourceLineViewModel(
                    lineNumber,
                    lines[lineNumber - 1].Replace('\t', ' ')));
            }
        }

        return [.. result];
    }

    private static List<(int startLine, int endLine)> merge_line_windows(
        IReadOnlyList<DiagnosticLocationViewModel> locations,
        int contextLineCount)
    {
        var windows = new List<(int startLine, int endLine)>();
        foreach (var location in locations)
        {
            var startLine = System.Math.Max(location.start_line - System.Math.Max(contextLineCount, 0), 1);
            var endLine = System.Math.Max(location.end_line, location.start_line) + System.Math.Max(contextLineCount, 0);
            if (windows.Count == 0)
            {
                windows.Add((startLine, endLine));
                continue;
            }

            var last = windows[^1];
            if (startLine <= last.endLine + 1)
            {
                windows[^1] = (last.startLine, System.Math.Max(last.endLine, endLine));
                continue;
            }

            windows.Add((startLine, endLine));
        }

        return windows;
    }

    #endregion
}
