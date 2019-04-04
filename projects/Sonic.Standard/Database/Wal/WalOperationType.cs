namespace Std.Database.Wal;

/// <summary>
///     WAL 操作类型
/// </summary>
public enum WalOperationType
{
    /// <summary>
    ///     插入操作
    /// </summary>
    insert,

    /// <summary>
    ///     更新操作
    /// </summary>
    update,

    /// <summary>
    ///     删除操作
    /// </summary>
    delete,

    /// <summary>
    ///     事务开始
    /// </summary>
    begin_transaction,

    /// <summary>
    ///     事务提交
    /// </summary>
    commit_transaction,

    /// <summary>
    ///     事务回滚
    /// </summary>
    rollback_transaction,

    /// <summary>
    ///     检查点
    /// </summary>
    checkpoint
}