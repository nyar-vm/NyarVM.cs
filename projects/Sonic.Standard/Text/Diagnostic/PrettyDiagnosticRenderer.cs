namespace Std.Data.Text.Diagnostics;

/// <summary>
///     负责输出 pretty 格式的诊断信息。
/// </summary>
internal static class PrettyDiagnosticRenderer
{
    /// <summary>
    ///     输出单条 pretty 诊断。
    /// </summary>
    public static void write(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options)
    {
        var writer = viewModel.is_error ? System.Console.Error : System.Console.Out;
        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        foreach (var block in layout.blocks)
        {
            foreach (var line in block.lines)
            {
                DiagnosticTextWriter.write_line(
                    writer,
                    line.text,
                    options,
                    line.color,
                    viewModel.is_error);
            }
        }
    }
}
