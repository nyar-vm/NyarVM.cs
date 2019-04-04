using Nyar.Analyzer.Semantic;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Language.Valkyrie.TypeChecker;

/// <summary>
///     类型检查诊断信息（委托给 Nyar.Semantic.SemanticDiagnostic）
/// </summary>
public sealed class TypeDiagnostic
{
    private readonly SemanticDiagnostic _inner;

    public TypeDiagnostic(string code, string message, DiagnosticSeverity severity,
        int line = 0, int column = 0, string? filePath = null)
    {
        this.code = code;
        this.message = message;
        this.severity = severity;
        this.line = line;
        this.column = column;
        suggestion = null;
        documentation_url = null;
        related_diagnostic = null;
    }

    /// <summary>
    ///     错误代码（如 VALK2001）
    /// </summary>
    public string code { get; }

    /// <summary>
    ///     错误消息（中文描述）
    /// </summary>
    public string message { get; }

    /// <summary>
    ///     诊断严重级别
    /// </summary>
    public DiagnosticSeverity severity { get; }

    /// <summary>
    ///     行号
    /// </summary>
    public int line { get; }

    /// <summary>
    ///     列号
    /// </summary>
    public int column { get; }

    /// <summary>
    ///     文件路径
    /// </summary>
    public string? file_path { get; init; }

    /// <summary>
    ///     修复建议（可选，用于 IDE 快速修复）
    /// </summary>
    public string? suggestion { get; init; }

    /// <summary>
    ///     文档链接（可选，指向在线帮助页）
    /// </summary>
    public string? documentation_url { get; init; }

    /// <summary>
    ///     相关诊断（可选，链式错误的上下文）
    /// </summary>
    public TypeDiagnostic? related_diagnostic { get; init; }

    /// <summary>
    ///     创建带修复建议的诊断
    /// </summary>
    public static TypeDiagnostic with_suggestion(string code, string message, string suggestion,
        int line = 0, int column = 0, string? filePath = null)
    {
        return new TypeDiagnostic(code, message, DiagnosticSeverity.error, line, column, filePath)
        {
            suggestion = suggestion
        };
    }

    /// <summary>
    ///     创建带文档链接的诊断
    /// </summary>
    public static TypeDiagnostic with_doc(string code, string message, string docUrl,
        int line = 0, int column = 0, string? filePath = null)
    {
        return new TypeDiagnostic(code, message, DiagnosticSeverity.error, line, column, filePath)
        {
            documentation_url = docUrl
        };
    }

    /// <summary>
    ///     转换为 Nyar SemanticDiagnostic
    /// </summary>
    public SemanticDiagnostic to_semantic_diagnostic()
    {
        return new SemanticDiagnostic(severity, message, new TextSpan(0, 0), code,
            filePath: file_path);
    }

    public override string ToString()
    {
        var location = line > 0 ? $"({line}:{column}) " : "";
        var severityStr = severity.ToString().ToLower();
        var result = $"{location}{severityStr}: {code} {message}";

        if (suggestion != null) result += $"  💡 建议：{suggestion}";

        if (documentation_url != null) result += $"  📖 文档：{documentation_url}";

        return result;
    }
}
