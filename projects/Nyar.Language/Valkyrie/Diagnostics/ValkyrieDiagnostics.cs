using Nyar.Analyzer.Semantic;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Language.Valkyrie.Diagnostics;

/// <summary>
///     Valkyrie 诊断发射辅助方法，集中处理 descriptor 查询、严重级别决议与复合 code 拼装，
///     validator 只需提供规则名、消息与 span。
/// </summary>
public static class ValkyrieDiagnostics
{
    /// <summary>
    ///     根据 rule name 查询 descriptor 并按其默认严重级别构造 <see cref="SemanticDiagnostic" />。
    ///     <paramref name="sourceSpan" /> 与 <paramref name="filePath" /> 用于定位诊断来源。
    /// </summary>
    public static SemanticDiagnostic build_semantic_diagnostic(
        string ruleName,
        string message,
        SourceSpan sourceSpan,
        string? filePath)
    {
        if (!ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor))
        {
            return new SemanticDiagnostic(
                DiagnosticSeverity.warning,
                message,
                sourceSpan,
                code: ruleName,
                filePath: filePath);
        }

        var code = ValkyrieDiagnosticRegistry.create_diagnostic_code(descriptor, descriptor.default_severity);
        return new SemanticDiagnostic(
            descriptor.default_severity,
            message,
            sourceSpan,
            code: code,
            filePath: filePath);
    }

    /// <summary>
    ///     根据 rule name 与最终严重级别构造 <see cref="SemanticDiagnostic" />。
    /// </summary>
    public static SemanticDiagnostic build_semantic_diagnostic(
        string ruleName,
        DiagnosticSeverity effectiveSeverity,
        string message,
        SourceSpan sourceSpan,
        string? filePath)
    {
        if (!ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor))
        {
            return new SemanticDiagnostic(
                effectiveSeverity,
                message,
                sourceSpan,
                code: ruleName,
                filePath: filePath);
        }

        var code = ValkyrieDiagnosticRegistry.create_diagnostic_code(descriptor, effectiveSeverity);
        return new SemanticDiagnostic(
            effectiveSeverity,
            message,
            sourceSpan,
            code: code,
            filePath: filePath);
    }

    /// <summary>
    ///     根据 rule name 查询 descriptor 并按其默认严重级别构造 <see cref="Diagnostic" />。
    /// </summary>
    public static Diagnostic build_diagnostic(
        string ruleName,
        TextSpan span,
        string message,
        string? filePath = null,
        SourceSpan sourceSpan = default,
        DiagnosticRelatedSpan[]? relatedSpans = null,
        string[]? hints = null)
    {
        if (!ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor))
        {
            return new Diagnostic(
                span,
                message,
                DiagnosticSeverity.warning,
                code: null,
                source: new DiagnosticSource(
                    filePath,
                    sourceSpan,
                    hints,
                    relatedSpans));
        }

        return new Diagnostic(
            span,
            message,
            descriptor.default_severity,
            code: descriptor.stable_code,
            source: new DiagnosticSource(
                filePath,
                sourceSpan,
                hints,
                relatedSpans));
    }

    /// <summary>
    ///     根据 rule name 与最终严重级别构造 <see cref="Diagnostic" />，并使用指定严重级别生成展示码。
    /// </summary>
    public static Diagnostic build_diagnostic(
        string ruleName,
        DiagnosticSeverity effectiveSeverity,
        TextSpan span,
        string message,
        string? filePath = null,
        SourceSpan sourceSpan = default,
        DiagnosticRelatedSpan[]? relatedSpans = null,
        string[]? hints = null)
    {
        if (!ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor))
        {
            return new Diagnostic(
                span,
                message,
                effectiveSeverity,
                code: null,
                source: new DiagnosticSource(
                    filePath,
                    sourceSpan,
                    hints,
                    relatedSpans));
        }

        return new Diagnostic(
            span,
            message,
            effectiveSeverity,
            code: descriptor.stable_code,
            source: new DiagnosticSource(
                filePath,
                sourceSpan,
                hints,
                relatedSpans));
    }
}
