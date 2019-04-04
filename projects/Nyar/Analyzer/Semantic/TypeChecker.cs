using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Semantic;

/// <summary>
///     类型检查器抽象基类 —— 提供公共诊断工厂方法，语言前端通过继承实现具体类型检查
/// </summary>
public abstract class TypeChecker
{
    public abstract IType infer_type(ISymbol symbol);
    public abstract IType infer_type_of_expression(object node);
    public abstract bool check_type(IType expected, IType actual, out SemanticDiagnostic? diagnostic);
    public abstract IReadOnlyList<SemanticDiagnostic> check_all_types(SemanticModel model);

    #region 诊断工厂方法 — TextSpan 版本

    protected SemanticDiagnostic type_mismatch(IType expected, IType actual, TextSpan span)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"类型不匹配：期望 {expected.name}，实际 {actual.name}",
            span,
            "TYPE_MISMATCH");
    }

    protected SemanticDiagnostic undefined_symbol(string name, TextSpan span)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"未定义的符号：{name}",
            span,
            "UNDEFINED_SYMBOL");
    }

    protected SemanticDiagnostic ambiguous_reference(string name, TextSpan span)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"歧义引用：{name}",
            span,
            "AMBIGUOUS_REFERENCE");
    }

    protected SemanticDiagnostic circular_dependency(string name, TextSpan span)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"循环依赖：{name}",
            span,
            "CIRCULAR_DEPENDENCY");
    }

    protected SemanticDiagnostic duplicate_definition(string name, TextSpan span)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"重复定义：{name}",
            span,
            "DUPLICATE_DEFINITION");
    }

    protected SemanticDiagnostic unreachable_code(TextSpan span)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.warning,
            "不可达代码",
            span,
            "UNREACHABLE_CODE");
    }

    #endregion

    #region 诊断工厂方法 — SourceSpan 版本

    protected SemanticDiagnostic type_mismatch(IType expected, IType actual, SourceSpan span, string? filePath = null)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"类型不匹配：期望 {expected.name}，实际 {actual.name}",
            span,
            "TYPE_MISMATCH",
            filePath);
    }

    protected SemanticDiagnostic undefined_symbol(string name, SourceSpan span, string? filePath = null)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"未定义的符号：{name}",
            span,
            "UNDEFINED_SYMBOL",
            filePath);
    }

    protected SemanticDiagnostic ambiguous_reference(string name, SourceSpan span, string? filePath = null)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"歧义引用：{name}",
            span,
            "AMBIGUOUS_REFERENCE",
            filePath);
    }

    protected SemanticDiagnostic circular_dependency(string name, SourceSpan span, string? filePath = null)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"循环依赖：{name}",
            span,
            "CIRCULAR_DEPENDENCY",
            filePath);
    }

    protected SemanticDiagnostic duplicate_definition(string name, SourceSpan span, string? filePath = null)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"重复定义：{name}",
            span,
            "DUPLICATE_DEFINITION",
            filePath);
    }

    protected SemanticDiagnostic unreachable_code(SourceSpan span, string? filePath = null)
    {
        return new SemanticDiagnostic(
            DiagnosticSeverity.warning,
            "不可达代码",
            span,
            "UNREACHABLE_CODE",
            filePath);
    }

    #endregion
}