namespace Std.Data.Text.Diagnostics;

/// <summary>
///     负责输出 detail 格式的诊断信息。
/// </summary>
internal static class DetailDiagnosticRenderer
{
    /// <summary>
    ///     输出单条 detail 诊断。
    /// </summary>
    public static void write(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        var writer = viewModel.is_error ? System.Console.Error : System.Console.Out;
        var detailPrefix = DiagnosticRenderSupport.format_detail_prefix(viewModel, options);
        var firstSegment = string.IsNullOrWhiteSpace(detailPrefix)
            ? viewModel.message
            : $"{detailPrefix} {viewModel.message}";
        var parts = new List<string> { firstSegment };

        var locationSuffix = DiagnosticRenderSupport.format_location_suffix(viewModel.location);
        if (!string.IsNullOrWhiteSpace(locationSuffix))
        {
            parts[^1] += locationSuffix;
        }

        foreach (var hint in DiagnosticRenderSupport.enumerate_hints(viewModel, options))
        {
            parts.Add(hint);
        }

        parts.AddRange(DiagnosticRenderSupport.enumerate_secondary_annotations(viewModel)
            .Select(annotation => DiagnosticRenderSupport.format_annotation_information(viewModel, annotation)));

        parts.AddRange(DiagnosticRenderSupport.enumerate_related_information(viewModel)
            .Select(DiagnosticRenderSupport.format_related_information));

        DiagnosticTextWriter.write_line(
            writer,
            string.Join(", ", parts),
            options,
            DiagnosticRenderSupport.resolve_severity_color(viewModel.severity),
            viewModel.is_error);
    }
}
