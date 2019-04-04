namespace Std.Data.Text.Diagnostics;

/// <summary>
///     负责输出 short 格式的诊断信息。
/// </summary>
internal static class ShortDiagnosticRenderer
{
    /// <summary>
    ///     输出单条 short 诊断。
    /// </summary>
    public static void write(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        var writer = viewModel.is_error ? System.Console.Error : System.Console.Out;
        var messagePrefix = DiagnosticRenderSupport.format_short_prefix(viewModel, options);
        var locationSuffix = DiagnosticRenderSupport.format_location_suffix(viewModel.location);
        var trailingGroups = new[]
            {
                DiagnosticRenderSupport.format_short_secondary_group(viewModel),
                DiagnosticRenderSupport.format_short_related_group(viewModel)
            }
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => $" ({group})");
        DiagnosticTextWriter.write_line(
            writer,
            $"{messagePrefix}{viewModel.message}{locationSuffix}{string.Concat(trailingGroups)}",
            options,
            DiagnosticRenderSupport.resolve_severity_color(viewModel.severity),
            viewModel.is_error);
    }
}
