namespace Std.Command.Migration;

/// <summary>
///     CLI 工具审计条目
/// </summary>
public sealed class CliToolAuditEntry
{
    /// <summary>
    ///     工具名称
    /// </summary>
    public string tool_name { get; init; } = string.Empty;

    /// <summary>
    ///     所属项目路径
    /// </summary>
    public string project_path { get; init; } = string.Empty;

    /// <summary>
    ///     当前使用的 CLI 框架
    /// </summary>
    public string current_framework { get; init; } = string.Empty;

    /// <summary>
    ///     目标框架版本
    /// </summary>
    public string target_framework { get; init; } = string.Empty;

    /// <summary>
    ///     Iris 迁移状态
    /// </summary>
    public MigrationStatus status { get; set; } = MigrationStatus.pending;

    /// <summary>
    ///     备注
    /// </summary>
    public string notes { get; set; } = string.Empty;
}