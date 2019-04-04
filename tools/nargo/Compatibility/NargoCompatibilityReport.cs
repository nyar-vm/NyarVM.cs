namespace Nargo.Cli.Compatibility;

/// <summary>
///     兼容问题严重级别
/// </summary>
public enum CompatibilitySeverity
{
    /// <summary>
    ///     信息：无需处理
    /// </summary>
    Info,

    /// <summary>
    ///     警告：建议处理
    /// </summary>
    Warning,

    /// <summary>
    ///     错误：必须处理
    /// </summary>
    Error,
}

/// <summary>
///     兼容问题条目
/// </summary>
public sealed class CompatibilityIssue
{
    /// <summary>
    ///     问题严重级别
    /// </summary>
    public CompatibilitySeverity Severity { get; init; } = CompatibilitySeverity.Info;

    /// <summary>
    ///     问题来源（如 "package.json"、"pnpm-workspace.yaml" 等）
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    ///     问题描述
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///     修复建议
    /// </summary>
    public string? Suggestion { get; init; }
}

/// <summary>
///     Nargo 兼容性报告：统一输出兼容来源与迁移问题
/// </summary>
public sealed class NargoCompatibilityReport
{
    /// <summary>
    ///     兼容问题列表
    /// </summary>
    public List<CompatibilityIssue> Issues { get; init; } = [];

    /// <summary>
    ///     已识别的兼容来源列表
    /// </summary>
    public List<string> DetectedSources { get; init; } = [];

    /// <summary>
    ///     是否存在错误级别的问题
    /// </summary>
    public bool HasErrors => Issues.Any(i => i.Severity == CompatibilitySeverity.Error);

    /// <summary>
    ///     是否存在警告级别及以上问题
    /// </summary>
    public bool HasWarnings => Issues.Any(i => i.Severity >= CompatibilitySeverity.Warning);

    /// <summary>
    ///     添加兼容问题
    /// </summary>
    /// <param name="severity">严重级别</param>
    /// <param name="source">来源</param>
    /// <param name="message">描述</param>
    /// <param name="suggestion">修复建议</param>
    public void add_issue(CompatibilitySeverity severity, string source, string message, string? suggestion = null)
    {
        Issues.Add(new CompatibilityIssue
        {
            Severity = severity,
            Source = source,
            Message = message,
            Suggestion = suggestion
        });
    }

    /// <summary>
    ///     生成报告文本
    /// </summary>
    /// <returns>格式化的报告字符串</returns>
    public string to_report_text()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Nargo 兼容性报告 ===");
        sb.AppendLine();

        if (DetectedSources.Count > 0)
        {
            sb.AppendLine("已识别的兼容来源：");
            foreach (var source in DetectedSources)
            {
                sb.AppendLine($"  - {source}");
            }

            sb.AppendLine();
        }

        if (Issues.Count == 0)
        {
            sb.AppendLine("无兼容问题。");
            return sb.ToString();
        }

        sb.AppendLine($"共 {Issues.Count} 个问题：");
        sb.AppendLine();

        foreach (var issue in Issues)
        {
            var severityTag = issue.Severity switch
            {
                CompatibilitySeverity.Info => "[INFO]",
                CompatibilitySeverity.Warning => "[WARN]",
                CompatibilitySeverity.Error => "[ERROR]",
                _ => "[????]"
            };

            sb.AppendLine($"  {severityTag} ({issue.Source}) {issue.Message}");

            if (issue.Suggestion is not null)
            {
                sb.AppendLine($"         建议: {issue.Suggestion}");
            }
        }

        return sb.ToString();
    }
}