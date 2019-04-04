namespace Core.Database;

/// <summary>
///     事务隔离级别
/// </summary>
public enum IsolationLevel
{
    /// <summary>
    ///     读未提交
    /// </summary>
    ReadUncommitted,

    /// <summary>
    ///     读已提交
    /// </summary>
    ReadCommitted,

    /// <summary>
    ///     快照隔离（默认）
    /// </summary>
    Snapshot,

    /// <summary>
    ///     可串行化
    /// </summary>
    Serializable
}