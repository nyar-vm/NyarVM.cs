using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Semantic;

/// <summary>
///     语义诊断构建器 —— 流式 API 构建诊断列表
/// </summary>
public sealed class SemanticDiagnosticBuilder
{
    private readonly List<SemanticDiagnostic> _diagnostics;

    public SemanticDiagnosticBuilder()
    {
        _diagnostics = [];
    }

    public SemanticDiagnosticBuilder type_error(string message, TextSpan span, string? code = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error, message, span, code ?? "TYPE_ERROR"));
        return this;
    }

    public SemanticDiagnosticBuilder type_mismatch(IType expected, IType actual, TextSpan span)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error,
            $"类型不匹配：期望 {expected.name}，实际 {actual.name}", span, "TYPE_MISMATCH"));
        return this;
    }

    public SemanticDiagnosticBuilder undefined_symbol(string name, TextSpan span)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error,
            $"未定义的符号：{name}", span, "UNDEFINED_SYMBOL"));
        return this;
    }

    public SemanticDiagnosticBuilder duplicate_definition(string name, TextSpan span)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error,
            $"重复定义：{name}", span, "DUPLICATE_DEFINITION"));
        return this;
    }

    public SemanticDiagnosticBuilder unreachable_code(TextSpan span)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.warning,
            "不可达代码", span, "UNREACHABLE_CODE"));
        return this;
    }

    public SemanticDiagnosticBuilder warning(string message, TextSpan span, string? code = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.warning, message, span, code ?? "WARNING"));
        return this;
    }

    public SemanticDiagnosticBuilder info(string message, TextSpan span, string? code = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.info, message, span, code ?? "INFO"));
        return this;
    }

    public SemanticDiagnosticBuilder type_error(string message, SourceSpan span, string? filePath = null,
        string? code = null)
    {
        _diagnostics.Add(
            new SemanticDiagnostic(DiagnosticSeverity.error, message, span, code ?? "TYPE_ERROR", filePath));
        return this;
    }

    public SemanticDiagnosticBuilder type_mismatch(IType expected, IType actual, SourceSpan span,
        string? filePath = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error,
            $"类型不匹配：期望 {expected.name}，实际 {actual.name}", span, "TYPE_MISMATCH", filePath));
        return this;
    }

    public SemanticDiagnosticBuilder undefined_symbol(string name, SourceSpan span, string? filePath = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error,
            $"未定义的符号：{name}", span, "UNDEFINED_SYMBOL", filePath));
        return this;
    }

    public SemanticDiagnosticBuilder duplicate_definition(string name, SourceSpan span, string? filePath = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.error,
            $"重复定义：{name}", span, "DUPLICATE_DEFINITION", filePath));
        return this;
    }

    public SemanticDiagnosticBuilder unreachable_code(SourceSpan span, string? filePath = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.warning,
            "不可达代码", span, "UNREACHABLE_CODE", filePath));
        return this;
    }

    public SemanticDiagnosticBuilder warning(string message, SourceSpan span, string? filePath = null,
        string? code = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.warning, message, span, code ?? "WARNING",
            filePath));
        return this;
    }

    public SemanticDiagnosticBuilder info(string message, SourceSpan span, string? filePath = null, string? code = null)
    {
        _diagnostics.Add(new SemanticDiagnostic(DiagnosticSeverity.info, message, span, code ?? "INFO", filePath));
        return this;
    }

    public IReadOnlyList<SemanticDiagnostic> build()
    {
        return _diagnostics;
    }
}