using Std.Database.Core;

namespace Std.Database.Wal;

/// <summary>
///     WAL 写入器接口
/// </summary>
internal interface IWalWriter : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     WAL 追加操作计数器，非数据库级序列号（仅用于诊断）
    /// </summary>
    SequenceNumber current_sequence { get; }

    /// <summary>
    ///     异步追加日志记录
    /// </summary>
    /// <param name="record">日志记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask append(WalRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    ///     同步刷盘
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask flush(CancellationToken cancellationToken = default);

    /// <summary>
    ///     截断 WAL（检查点后调用）
    /// </summary>
    /// <param name="sequence">截断到指定序列号</param>
    ValueTask truncate(SequenceNumber sequence);
}