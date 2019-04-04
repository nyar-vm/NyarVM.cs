using System.Collections.Immutable;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Diagnostics;

public class DiagnosticSink
{
    private readonly List<Diagnostic> _diagnostics = [];

    public IReadOnlyList<Diagnostic> messages => _diagnostics;

    public bool has_errors => _diagnostics.Any(d => d.severity.is_error_level());

    public void report(
        TextSpan span,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.error,
        int? code = null,
        string? filePath = null,
        SourceSpan sourceSpan = default,
        params string[] hints)
    {
        _diagnostics.Add(new Diagnostic(
            span,
            message,
            severity,
            code,
            new DiagnosticSource(
                filePath,
                sourceSpan,
                hints)));
    }

    public void report_error(TextSpan span, string message)
    {
        report(span, message);
    }

    /// <summary>
    ///     报告错误（兼容旧签名，忽略 filePath 和 errorCode）
    /// </summary>
    public void report_error(string filePath, TextSpan span, string errorCode, string message)
    {
        report(span, message, code: parse_legacy_code(errorCode), filePath: filePath);
    }

    /// <summary>
    ///     报告错误。
    /// </summary>
    public void report_error(string filePath, TextSpan span, int? code, string message)
    {
        report(span, message, code: code, filePath: filePath);
    }

    public void report_warning(TextSpan span, string message)
    {
        report(span, message, DiagnosticSeverity.warning);
    }

    /// <summary>
    ///     报告警告（兼容旧签名，忽略 filePath 和 errorCode）
    /// </summary>
    public void report_warning(string filePath, TextSpan span, string errorCode, string message)
    {
        report(span, message, DiagnosticSeverity.warning, parse_legacy_code(errorCode), filePath);
    }

    /// <summary>
    ///     报告警告。
    /// </summary>
    public void report_warning(string filePath, TextSpan span, int? code, string message)
    {
        report(span, message, DiagnosticSeverity.warning, code, filePath);
    }

    public ImmutableArray<Diagnostic> get_diagnostics()
    {
        return [.. _diagnostics];
    }

    public void clear()
    {
        _diagnostics.Clear();
    }

    /// <summary>
    ///     将字符串形式的历史错误代码（如 VALK2001）解析为 <see cref="int" /> 形式的数字代码。
    /// </summary>
    /// <param name="code">字符串错误代码，可能包含字母和数字。</param>
    /// <returns>提取出的数字代码；若无法解析则返回 <c>null</c>。</returns>
    public static int? parse_legacy_code(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var digits = new System.Text.StringBuilder();
        foreach (var ch in code)
        {
            if (char.IsDigit(ch))
            {
                digits.Append(ch);
            }
        }

        if (digits.Length == 0)
        {
            return null;
        }

        return int.TryParse(digits.ToString(), out var parsedCode)
            ? parsedCode
            : null;
    }
}
