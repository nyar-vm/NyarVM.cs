namespace Std.Data.Text.Diagnostics;

/// <summary>
///     统一处理终端写出与颜色控制。
/// </summary>
internal static class DiagnosticTextWriter
{
    /// <summary>
    ///     输出单行文本。
    /// </summary>
    public static void write_line(
        TextWriter writer,
        string text,
        DiagnosticRenderOptions options,
        ConsoleColor? color = null,
        bool useErrorStream = false)
    {
        if (!should_use_color(options, useErrorStream) || color is null || string.IsNullOrEmpty(text))
        {
            writer.WriteLine(text);
            return;
        }

        var previousColor = System.Console.ForegroundColor;
        try
        {
            System.Console.ForegroundColor = color.Value;
            writer.WriteLine(text);
        }
        finally
        {
            System.Console.ForegroundColor = previousColor;
        }
    }

    private static bool should_use_color(DiagnosticRenderOptions options, bool useErrorStream)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR")))
        {
            return false;
        }

        return options.color switch
        {
            DiagnosticColorMode.Always => true,
            DiagnosticColorMode.Never => false,
            _ => useErrorStream ? !System.Console.IsErrorRedirected : !System.Console.IsOutputRedirected
        };
    }
}
