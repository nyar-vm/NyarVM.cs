namespace Std.Data.Text.Diagnostics;

/// <summary>
///     自定义诊断格式适配器。
///     上层工具可通过该接口接管 `Sonic` 诊断模型的最终输出。
/// </summary>
public interface IDiagnosticRenderAdapter
{
    /// <summary>
    ///     输出上下文标题。
    /// </summary>
    void write_context_heading(
        DiagnosticRenderOptions options,
        string verb,
        string projectDir,
        string canonicalTriple);

    /// <summary>
    ///     输出单条诊断。
    /// </summary>
    void write_diagnostic(
        DiagnosticViewModel viewModel,
        DiagnosticRenderOptions options);

    /// <summary>
    ///     输出汇总信息。
    /// </summary>
    void write_summary(
        DiagnosticRenderOptions options,
        string toolName,
        int errorCount,
        int warningCount);
}
