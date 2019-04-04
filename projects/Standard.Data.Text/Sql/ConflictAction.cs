namespace Std.Data.Text.Sql;

/// <summary>
///     ON CONFLICT 冲突解决策略
/// </summary>
public enum ConflictAction
{
    rollback,
    abort,
    fail,
    ignore,
    replace
}