namespace Std.Database.Wal;

/// <summary>
///     WAL 刷盘策略
/// </summary>
public enum WalFlushPolicy
{
    /// <summary>
    ///     每次写入都刷盘
    /// </summary>
    every_write,

    /// <summary>
    ///     批量刷盘
    /// </summary>
    batch,

    /// <summary>
    ///     定时刷盘
    /// </summary>
    periodic
}