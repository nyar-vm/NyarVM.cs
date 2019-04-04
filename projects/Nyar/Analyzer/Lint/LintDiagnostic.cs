namespace Nyar.Analyzer.Lint;

/// <summary>
///     Lint 诊断信息：描述一处代码问题
/// </summary>
public sealed class LintDiagnostic
{
    /// <summary>
    ///     构造一个 Lint 诊断
    /// </summary>
    public LintDiagnostic(LintSeverity severity, string message, string file, int line, int column, string? code = null)
    {
        this.severity = severity;
        this.message = message;
        this.file = file;
        this.line = line;
        this.column = column;
        this.code = code;
    }

    /// <summary>
    ///     诊断严重级别
    /// </summary>
    public LintSeverity severity { get; }

    /// <summary>
    ///     诊断消息
    /// </summary>
    public string message { get; }

    /// <summary>
    ///     文件路径
    /// </summary>
    public string file { get; }

    /// <summary>
    ///     行号（从 1 开始）
    /// </summary>
    public int line { get; }

    /// <summary>
    ///     列号（从 1 开始）
    /// </summary>
    public int column { get; }

    /// <summary>
    ///     规则代码（可选）
    /// </summary>
    public string? code { get; }

    /// <summary>
    ///     输出格式：file.v:10:5: warning: 未使用的变量 'x'
    /// </summary>
    public override string ToString()
    {
        var level = severity switch
        {
            LintSeverity.error => "error",
            LintSeverity.warning => "warning",
            LintSeverity.suggestion => "suggestion",
            _ => severity.ToString().ToLowerInvariant()
        };

        return $"{file}:{line}:{column}: {level}: {message}";
    }
}