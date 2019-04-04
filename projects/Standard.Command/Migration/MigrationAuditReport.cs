using System.Text;

namespace Std.Command.Migration;

/// <summary>
///     迁移审计报告
/// </summary>
public sealed class MigrationAuditReport
{
    /// <summary>
    ///     审计条目列表
    /// </summary>
    public IReadOnlyList<CliToolAuditEntry> entries { get; init; } = [];

    /// <summary>
    ///     报告生成时间
    /// </summary>
    public DateTimeOffset generated_at { get; init; }

    /// <summary>
    ///     CLI 工具总数（不含豁免）
    /// </summary>
    public int total_cli_tools { get; init; }

    /// <summary>
    ///     已迁移数
    /// </summary>
    public int migrated_count { get; init; }

    /// <summary>
    ///     迁移中数
    /// </summary>
    public int in_progress_count { get; init; }

    /// <summary>
    ///     已计划数
    /// </summary>
    public int planned_count { get; init; }

    /// <summary>
    ///     待评估数
    /// </summary>
    public int pending_count { get; init; }

    /// <summary>
    ///     Iris 采用率（已迁移 / 总数）
    /// </summary>
    public double adoption_rate => total_cli_tools > 0 ? (double)migrated_count / total_cli_tools : 0;

    /// <summary>
    ///     Iris 目标采用率（含已计划 / 总数）
    /// </summary>
    public double target_adoption_rate =>
        total_cli_tools > 0 ? (double)(migrated_count + planned_count) / total_cli_tools : 0;

    /// <summary>
    ///     生成格式化的迁移状态报告文本
    /// </summary>
    public string format_report()
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Iris CLI 迁移审计报告 ===");
        sb.AppendLine($"生成时间: {generated_at:yyyy-MM-dd HH:mm:ss}Z");
        sb.AppendLine($"CLI 工具总数: {total_cli_tools}");
        sb.AppendLine(
            $"已迁移: {migrated_count} | 迁移中: {in_progress_count} | 已计划: {planned_count} | 待评估: {pending_count}");
        sb.AppendLine($"当前采用率: {adoption_rate:P0} | 目标采用率: {target_adoption_rate:P0}");
        sb.AppendLine();

        sb.AppendLine("--- 详细条目 ---");
        foreach (var entry in entries)
        {
            var statusIcon = entry.status switch
            {
                MigrationStatus.migrated => "✅",
                MigrationStatus.in_progress => "🔄",
                MigrationStatus.planned => "📋",
                MigrationStatus.pending => "⏳",
                MigrationStatus.exempt => "➖",
                _ => "❓"
            };

            sb.AppendLine($"{statusIcon} {entry.tool_name}");
            sb.AppendLine($"   框架: {entry.current_framework} → Iris | 目标: {entry.target_framework}");
            sb.AppendLine($"   路径: {entry.project_path}");
            if (!string.IsNullOrEmpty(entry.notes)) sb.AppendLine($"   备注: {entry.notes}");

            sb.AppendLine();
        }

        return sb.ToString();
    }
}