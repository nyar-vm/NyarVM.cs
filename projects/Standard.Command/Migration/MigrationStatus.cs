namespace Std.Command.Migration;

/// <summary>
///     CLI 工具迁移审计状态
/// </summary>
public enum MigrationStatus
{
    /// <summary>
    ///     已完全迁移到 Iris
    /// </summary>
    migrated,

    /// <summary>
    ///     迁移中
    /// </summary>
    in_progress,

    /// <summary>
    ///     已制定迁移计划
    /// </summary>
    planned,

    /// <summary>
    ///     待评估
    /// </summary>
    pending,

    /// <summary>
    ///     豁免（非 CLI 工具或不可迁移）
    /// </summary>
    exempt
}