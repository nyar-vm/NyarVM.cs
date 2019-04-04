namespace Std.Data.Text.Diagnostics;

/// <summary>
///     通用诊断渲染器，统一输出 `check` / `lint` 等诊断结果。
/// </summary>
public static class DiagnosticRenderer
{
    #region Public API

    /// <summary>
    ///     输出单个编译上下文的起始标题。
    /// </summary>
    public static void write_context_heading(
        DiagnosticRenderOptions options,
        string verb,
        string projectDir,
        string canonicalTriple)
    {
        if (options.format == DiagnosticFormat.Custom)
        {
            get_custom_adapter(options).write_context_heading(options, verb, projectDir, canonicalTriple);
            return;
        }

        if (options.format != DiagnosticFormat.Pretty)
        {
            return;
        }

        DiagnosticTextWriter.write_line(
            System.Console.Out,
            $"{DiagnosticRenderSupport.localize_verb(options, verb)} `{Path.GetFileName(projectDir)}` ({canonicalTriple})",
            options,
            ConsoleColor.Cyan);
    }

    /// <summary>
    ///     输出 `check` 阶段收集到的诊断。
    /// </summary>
    public static (int errors, int warnings) write_check_diagnostics(
        IReadOnlyList<Diagnostic> diagnostics,
        DiagnosticRenderOptions options,
        string projectDir)
    {
        var counters = (errors: 0, warnings: 0);
        foreach (var diagnostic in diagnostics)
        {
            if (!should_render(diagnostic, options.minimum_severity))
            {
                continue;
            }

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);
            write_entry(viewModel, options);
            if (viewModel.is_error)
            {
                counters.errors++;
            }
            else
            {
                counters.warnings++;
            }
        }

        return counters;
    }

    /// <summary>
    ///     输出 `lint` 阶段某个文件的诊断。
    /// </summary>
    public static (int errors, int warnings) write_lint_diagnostics(
        string filePath,
        string source,
        IReadOnlyList<Diagnostic> diagnostics,
        DiagnosticRenderOptions options,
        string projectDir)
    {
        var counters = (errors: 0, warnings: 0);
        foreach (var diagnostic in diagnostics)
        {
            if (!should_render(diagnostic, options.minimum_severity))
            {
                continue;
            }

            var viewModel = DiagnosticViewModelFactory.create_lint_view_model(
                filePath,
                source,
                diagnostic,
                options,
                projectDir);
            write_entry(viewModel, options);
            if (viewModel.is_error)
            {
                counters.errors++;
            }
            else
            {
                counters.warnings++;
            }
        }

        return counters;
    }

    /// <summary>
    ///     输出最终汇总。
    /// </summary>
    public static void write_summary(
        DiagnosticRenderOptions options,
        string toolName,
        int errorCount,
        int warningCount)
    {
        if (options.format == DiagnosticFormat.Custom)
        {
            get_custom_adapter(options).write_summary(options, toolName, errorCount, warningCount);
            return;
        }

        if (errorCount > 0)
        {
            DiagnosticTextWriter.write_line(
                System.Console.Error,
                DiagnosticRenderSupport.format_summary(options, succeeded: false, toolName, errorCount, warningCount),
                options,
                ConsoleColor.Red);
            return;
        }

        DiagnosticTextWriter.write_line(
            System.Console.Out,
            DiagnosticRenderSupport.format_summary(options, succeeded: true, toolName, errorCount, warningCount),
            options,
            warningCount > 0 ? ConsoleColor.Yellow : ConsoleColor.Green);
    }

    #endregion

    #region Rendering

    private static void write_entry(DiagnosticViewModel viewModel, DiagnosticRenderOptions options)
    {
        switch (options.format)
        {
            case DiagnosticFormat.Short:
                ShortDiagnosticRenderer.write(viewModel, options);
                break;
            case DiagnosticFormat.Detail:
                DetailDiagnosticRenderer.write(viewModel, options);
                break;
            case DiagnosticFormat.Custom:
                get_custom_adapter(options).write_diagnostic(viewModel, options);
                break;
            default:
                PrettyDiagnosticRenderer.write(viewModel, options);
                break;
        }
    }

    #endregion

    #region 内部模型

    private static bool should_render(Diagnostic diagnostic, DiagnosticSeverity minimumSeverity)
    {
        return diagnostic.severity <= minimumSeverity;
    }

    private static IDiagnosticRenderAdapter get_custom_adapter(DiagnosticRenderOptions options)
    {
        return options.custom_adapter
               ?? throw new InvalidOperationException(
                   $"自定义诊断格式 `{options.custom_format ?? "<unknown>"}` 缺少适配器。");
    }

    #endregion
}
