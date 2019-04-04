using Std.Database.Core;

namespace Std.Database.Wal;

/// <summary>
///     WAL 读取器接口
/// </summary>
internal interface IWalReader : IDisposable
{
    /// <summary>
    ///     从指定序列号开始读取
    /// </summary>
    /// <param name="startSequence">起始序列号</param>
    /// <returns>日志记录枚举</returns>
    IAsyncEnumerable<WalRecord> read_from(SequenceNumber startSequence);

    /// <summary>
    ///     读取所有记录
    /// </summary>
    /// <returns>日志记录枚举</returns>
    IAsyncEnumerable<WalRecord> read_all();
}