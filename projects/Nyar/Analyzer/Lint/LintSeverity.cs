namespace Nyar.Analyzer.Lint;

/// <summary>
///     Lint 诊断严重级别
/// </summary>
public enum LintSeverity
{
    /// <summary>
    ///     错误：必须修复
    /// </summary>
    error,

    /// <summary>
    ///     警告：建议修复
    /// </summary>
    warning,

    /// <summary>
    ///     建议：代码风格改进
    /// </summary>
    suggestion
}