namespace Std.Data.Text.Diagnostics;

/// <summary>
///     提供各类诊断渲染器共享的格式化辅助。
/// </summary>
internal static class DiagnosticRenderSupport
{
    /// <summary>
    ///     本地化命令动作标题。
    /// </summary>
    public static string localize_verb(DiagnosticRenderOptions options, string verb)
    {
        if (options.language != DiagnosticLanguage.ZhHans)
        {
            return verb;
        }

        return verb switch
        {
            "Checking" => "检查",
            "Linting" => "分析",
            _ => verb
        };
    }

    /// <summary>
    ///     格式化汇总信息。
    /// </summary>
    public static string format_summary(
        DiagnosticRenderOptions options,
        bool succeeded,
        string toolName,
        int errorCount,
        int warningCount)
    {
        if (options.language == DiagnosticLanguage.ZhHans)
        {
            return succeeded
                ? $"完成 {toolName}：{warningCount} 个警告"
                : $"{toolName} 失败：{errorCount} 个错误，{warningCount} 个警告";
        }

        return succeeded
            ? $"Finished {toolName}: {warningCount} warning(s)"
            : $"Failed {toolName}: {errorCount} error(s), {warningCount} warning(s)";
    }

    /// <summary>
    ///     解析需要输出的提示信息。
    /// </summary>
    public static string[] enumerate_hints(DiagnosticViewModel viewModel, DiagnosticRenderOptions options)
    {
        // 视图模型的 hints 数组已由工厂层基于 diagnostic.hints 预先聚合，
        // 此处仅按选项决定是否整体输出。
        if (!options.show_hints)
        {
            return [];
        }

        return viewModel.hints;
    }

    /// <summary>
    ///     按稳定顺序枚举同文件次级标注。
    /// </summary>
    public static DiagnosticAnnotationViewModel[] enumerate_secondary_annotations(DiagnosticViewModel viewModel)
    {
        return
        [
            .. viewModel.annotations
                .Where(annotation => annotation.kind == DiagnosticAnnotationKind.Secondary)
                .OrderBy(annotation => annotation.start_line)
                .ThenBy(annotation => annotation.start_column)
                .ThenBy(annotation => annotation.end_line)
                .ThenBy(annotation => annotation.end_column)
                .ThenBy(annotation => annotation.label, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    ///     按稳定顺序枚举跨文件关联信息。
    /// </summary>
    public static DiagnosticRelatedInformationViewModel[] enumerate_related_information(DiagnosticViewModel viewModel)
    {
        if (viewModel.related_information is not { Count: > 0 })
        {
            return [];
        }

        return
        [
            .. viewModel.related_information
                .OrderBy(related => related.location.display_file_path, StringComparer.Ordinal)
                .ThenBy(related => related.location.start_line)
                .ThenBy(related => related.location.start_column)
                .ThenBy(related => related.location.end_line)
                .ThenBy(related => related.location.end_column)
                .ThenBy(related => related.label, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    ///     格式化位置文本。
    /// </summary>
    public static string format_location(DiagnosticLocationViewModel location)
    {
        if (location.has_precise_span)
        {
            return $"{location.display_file_path}:{location.start_line}:{location.start_column}";
        }

        return location.display_file_path;
    }

    /// <summary>
    ///     格式化位置后缀。
    /// </summary>
    public static string format_location_suffix(DiagnosticLocationViewModel location)
    {
        if (!location.has_precise_span)
        {
            return string.Empty;
        }

        return $" at {format_location(location)}";
    }

    /// <summary>
    ///     格式化关联位置信息。
    /// </summary>
    public static string format_related_information(DiagnosticRelatedInformationViewModel relatedInformation)
    {
        return $"{relatedInformation.label} at {format_location(relatedInformation.location)}";
    }

    /// <summary>
    ///     格式化同文件标注位置信息。
    /// </summary>
    public static string format_annotation_information(
        DiagnosticViewModel viewModel,
        DiagnosticAnnotationViewModel annotation)
    {
        var location = new DiagnosticLocationViewModel(
            viewModel.location.source_file_path,
            viewModel.location.display_file_path,
            annotation.start_line,
            annotation.start_column,
            annotation.end_line,
            annotation.end_column);
        return $"{annotation.label} at {format_location(location)}";
    }

    /// <summary>
    ///     格式化 short 模式的同文件次级标注分组。
    /// </summary>
    public static string? format_short_secondary_group(DiagnosticViewModel viewModel)
    {
        var secondaryItems = enumerate_secondary_annotations(viewModel)
            .Select(annotation => format_annotation_information(viewModel, annotation))
            .ToArray();
        return secondaryItems.Length == 0
            ? null
            : $"secondary: {string.Join("; ", secondaryItems)}";
    }

    /// <summary>
    ///     格式化 short 模式的跨文件关联信息分组。
    /// </summary>
    public static string? format_short_related_group(DiagnosticViewModel viewModel)
    {
        var relatedItems = enumerate_related_information(viewModel)
            .Select(format_related_information)
            .ToArray();
        if (relatedItems.Length == 0)
        {
            return null;
        }

        return $"related: {string.Join("; ", relatedItems)}";
    }

    /// <summary>
    ///     格式化短输出前缀。
    /// </summary>
    public static string format_short_prefix(DiagnosticViewModel viewModel, DiagnosticRenderOptions options)
    {
        var bracketedCode = format_bracketed_code(viewModel, options);
        if (!string.IsNullOrWhiteSpace(bracketedCode))
        {
            return $"{bracketedCode} ";
        }

        return $"{viewModel.level}: ";
    }

    /// <summary>
    ///     格式化详情输出前缀。
    /// </summary>
    public static string format_detail_prefix(DiagnosticViewModel viewModel, DiagnosticRenderOptions options)
    {
        var code = options.show_code ? viewModel.display_code : null;
        if (!string.IsNullOrWhiteSpace(code))
        {
            return $"[{code}]";
        }

        return viewModel.level;
    }

    /// <summary>
    ///     解析严重级别对应颜色。
    /// </summary>
    public static ConsoleColor resolve_severity_color(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.fatal => ConsoleColor.Magenta,
            DiagnosticSeverity.error => ConsoleColor.Red,
            DiagnosticSeverity.warning => ConsoleColor.Yellow,
            DiagnosticSeverity.info => ConsoleColor.Cyan,
            DiagnosticSeverity.hint => ConsoleColor.Cyan,
            DiagnosticSeverity.debug => ConsoleColor.Blue,
            DiagnosticSeverity.trace => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };
    }

    private static string format_bracketed_code(DiagnosticViewModel viewModel, DiagnosticRenderOptions options)
    {
        var code = options.show_code ? viewModel.display_code : null;
        return string.IsNullOrWhiteSpace(code) ? string.Empty : $"[{code}]";
    }

    /// <summary>
    ///     格式化 pretty 模式的标题行。
    /// </summary>
    public static string format_pretty_heading(DiagnosticViewModel viewModel, DiagnosticRenderOptions options)
    {
        var bracketedCode = format_bracketed_code(viewModel, options);
        if (!string.IsNullOrWhiteSpace(bracketedCode))
        {
            return $"{bracketedCode}: {viewModel.message}";
        }

        return $"{viewModel.level}: {viewModel.message}";
    }

    /// <summary>
    ///     格式化 pretty 模式的元数据行（如 hint）。
    /// </summary>
    public static string format_pretty_metadata(DiagnosticRenderOptions options, string kind, string value)
    {
        var label = options.language == DiagnosticLanguage.ZhHans
            ? kind switch
            {
                "hint" => "提示",
                "related" => "关联",
                _ => kind
            }
            : kind;

        return $"   = {label}: {value}";
    }

    /// <summary>
    ///     格式化 pretty 模式的关联信息分组标题。
    /// </summary>
    public static string format_pretty_related_heading(DiagnosticRenderOptions options)
    {
        return options.language == DiagnosticLanguage.ZhHans
            ? "   = 关联:"
            : "   = related:";
    }

    /// <summary>
    ///     格式化 pretty 模式的关联信息条目。
    /// </summary>
    public static string format_pretty_related_item(DiagnosticRelatedInformationViewModel relatedInformation)
    {
        return $"     - {relatedInformation.label} at {format_location(relatedInformation.location)}";
    }

    /// <summary>
    ///     获取当前符号集对应的符号字符。
    /// </summary>
    public static DiagnosticSymbolSetView get_symbols(DiagnosticRenderOptions options)
    {
        return options.symbol == DiagnosticSymbolSet.Unicode
            ? new DiagnosticSymbolSetView("┌─", "│")
            : new DiagnosticSymbolSetView("-->", "|");
    }
}

/// <summary>
///     诊断符号字符集视图，提供 pretty 模式所需的前缀与边框字符。
/// </summary>
internal readonly record struct DiagnosticSymbolSetView(
    string location_prefix,
    string gutter);
