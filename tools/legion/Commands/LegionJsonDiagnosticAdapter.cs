using System.Text.Json;
using Std.Data.Text.Diagnostics;

namespace Legion.CLI.Commands;

/// <summary>
///     Legion 自定义 `json` 诊断输出适配器。
/// </summary>
internal sealed class LegionJsonDiagnosticAdapter : IDiagnosticRenderAdapter
{
    /// <summary>
    ///     单例实例。
    /// </summary>
    internal static readonly LegionJsonDiagnosticAdapter instance = new();

    private LegionJsonDiagnosticAdapter()
    {
    }

    /// <summary>
    ///     `json` 模式不输出上下文标题。
    /// </summary>
    public void write_context_heading(
        DiagnosticRenderOptions options,
        string verb,
        string projectDir,
        string canonicalTriple)
    {
    }

    /// <summary>
    ///     输出单条 json 诊断。
    /// </summary>
    public void write_diagnostic(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            type = "diagnostic",
            level = viewModel.level,
            code = options.show_code ? viewModel.display_code : null,
            message = viewModel.message,
            file = viewModel.location.display_file_path,
            line = viewModel.location.start_line,
            column = viewModel.location.start_column,
            endLine = viewModel.location.end_line,
            endColumn = viewModel.location.end_column,
            hints = options.show_hints ? viewModel.hints : []
        }));
    }

    /// <summary>
    ///     输出 json 汇总。
    /// </summary>
    public void write_summary(
        DiagnosticRenderOptions options,
        string toolName,
        int errorCount,
        int warningCount)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            type = "summary",
            tool = toolName,
            errors = errorCount,
            warnings = warningCount
        }));
    }
}
