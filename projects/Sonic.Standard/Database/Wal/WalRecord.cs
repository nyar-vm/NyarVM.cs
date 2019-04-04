using Std.Database.Core;

namespace Std.Database.Wal;

/// <summary>
///     WAL 日志记录
/// </summary>
internal sealed class WalRecord
{
    /// <summary>
    ///     序列号
    /// </summary>
    public required SequenceNumber sequence { get; init; }

    /// <summary>
    ///     事务 ID
    /// </summary>
    public required TransactionId transaction_id { get; init; }

    /// <summary>
    ///     操作类型
    /// </summary>
    public required WalOperationType operation_type { get; init; }

    /// <summary>
    ///     键
    /// </summary>
    public required DatabaseKey key { get; init; }

    /// <summary>
    ///     旧值（用于回滚）
    /// </summary>
    public DatabaseValue? old_value { get; init; }

    /// <summary>
    ///     新值
    /// </summary>
    public DatabaseValue? new_value { get; init; }

    /// <summary>
    ///     时间戳
    /// </summary>
    public DateTime timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    ///     校验和
    /// </summary>
    public uint checksum { get; init; }
}