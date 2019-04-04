using Std.Database.Core;

namespace Std.Database.Index;

/// <summary>
///     B+ 树索引接口
/// </summary>
internal interface IBTreeIndex
{
    /// <summary>
    ///     索引名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     插入键值对
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="sequence">写入序列号，用于 MVCC 可见性判断</param>
    ValueTask insert(DatabaseKey key, DatabaseValue value, SequenceNumber sequence = default);

    /// <summary>
    ///     查找键
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="asOfSequence">快照序列号，为 null 时读取最新版本</param>
    /// <returns>值，若不存在则返回 null</returns>
    ValueTask<DatabaseValue?> search(DatabaseKey key, SequenceNumber? asOfSequence = null);

    /// <summary>
    ///     删除键
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="sequence">删除序列号</param>
    /// <returns>是否成功删除</returns>
    ValueTask<bool> delete(DatabaseKey key, SequenceNumber sequence = default);

    /// <summary>
    ///     范围扫描
    /// </summary>
    /// <param name="start">起始键</param>
    /// <param name="end">结束键</param>
    /// <param name="asOfSequence">快照序列号，为 null 时读取最新版本</param>
    /// <returns>键值对异步枚举</returns>
    IAsyncEnumerable<DatabaseEntry> range_scan(DatabaseKey start, DatabaseKey end,
        SequenceNumber? asOfSequence = null);

    /// <summary>
    ///     前缀扫描
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <param name="asOfSequence">快照序列号，为 null 时读取最新版本</param>
    /// <returns>键值对异步枚举</returns>
    IAsyncEnumerable<DatabaseEntry> prefix_scan(DatabaseKey prefix, SequenceNumber? asOfSequence = null);

    /// <summary>
    ///     压缩索引，物理删除墓碑标记的键值对并回收空间
    /// </summary>
    /// <param name="minSequence">最小保留序列号，仅清理早于此序列号的墓碑</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的墓碑数量</returns>
    ValueTask<int> compact(SequenceNumber minSequence, CancellationToken cancellationToken = default);
}