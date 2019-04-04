using Std.Database.Core;

namespace Std.Database;

/// <summary>
///     MVCC 版本存储，保存键的历史版本以支持快照隔离（线程安全）
/// </summary>
internal sealed class VersionStore
{
    #region 构造函数

    /// <summary>
    ///     创建版本存储
    /// </summary>
    /// <param name="maxVersionsPerKey">每个键的最大版本数</param>
    /// <param name="maxTotalVersions">总版本数上限</param>
    public VersionStore(int maxVersionsPerKey = 64, long maxTotalVersions = 1_000_000)
    {
        _max_versions_per_key = maxVersionsPerKey;
        _max_total_versions = maxTotalVersions;
    }

    #endregion

    #region 属性

    /// <summary>
    ///     当前总版本数
    /// </summary>
    public long total_version_count => Interlocked.Read(ref _total_version_count);

    #endregion

    private readonly record struct VersionEntry(SequenceNumber sequence, DatabaseValue value)
        : IComparable<VersionEntry>
    {
        public int CompareTo(VersionEntry other)
        {
            return sequence.value.CompareTo(other.sequence.value);
        }
    }

    #region 字段

    private readonly Dictionary<DatabaseKey, List<VersionEntry>> _versions = new();
    private readonly int _max_versions_per_key;
    private readonly long _max_total_versions;
    private long _total_version_count;
    private readonly ReaderWriterLockSlim _lock = new();

    #endregion

    #region 版本操作

    /// <summary>
    ///     添加一个历史版本
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="sequence">写入时的序列号</param>
    /// <param name="value">旧值</param>
    public void add_version(DatabaseKey key, SequenceNumber sequence, DatabaseValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_versions.TryGetValue(key, out var list))
            {
                list = [];
                _versions[key] = list;
            }

            list.Add(new VersionEntry(sequence, value));
            Interlocked.Increment(ref _total_version_count);

            if (list.Count > _max_versions_per_key)
            {
                var removed = list.Count - _max_versions_per_key;
                list.RemoveRange(0, removed);
                Interlocked.Add(ref _total_version_count, -removed);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     获取指定键在给定序列号时可见的值（二分查找，O(log n)）
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="asOfSequence">快照序列号</param>
    /// <returns>可见的值，若无可见版本则返回 null</returns>
    public DatabaseValue? get_visible_value(DatabaseKey key, SequenceNumber asOfSequence)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_versions.TryGetValue(key, out var list)) return null;

            var targetEntry = new VersionEntry(asOfSequence, default);
            var index = list.BinarySearch(targetEntry);

            if (index >= 0) return list[index].value;

            var insertionPoint = ~index;
            if (insertionPoint > 0) return list[insertionPoint - 1].value;

            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     清理早于最小活跃序列号的历史版本
    /// </summary>
    /// <param name="minActiveSequence">最小活跃序列号</param>
    public void cleanup(SequenceNumber minActiveSequence)
    {
        _lock.EnterWriteLock();
        try
        {
            var keysToRemove = new List<DatabaseKey>();

            foreach (var kvp in _versions)
            {
                var removed = kvp.Value.RemoveAll(v => v.sequence.value < minActiveSequence.value);
                if (removed > 0) Interlocked.Add(ref _total_version_count, -removed);

                if (kvp.Value.Count == 0) keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove) _versions.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    ///     检查是否需要清理（总版本数超过阈值）
    /// </summary>
    /// <returns>是否需要清理</returns>
    public bool needs_cleanup()
    {
        return Interlocked.Read(ref _total_version_count) > _max_total_versions;
    }

    #endregion
}