using Core.Database;
using Std.Database.Core;
using Std.Database.Index;

namespace Std.Database;

/// <summary>
///     LightDB 游标实现，支持 MVCC 快照可见性
/// </summary>
internal sealed class LightCursor : ICursor
{
    #region 常量

    private const int _max_history_size = 1024;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建游标
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="startKey">起始键</param>
    /// <param name="asOfSequence">快照序列号，为 null 时读取最新版本</param>
    internal LightCursor(IBTreeIndex index, DatabaseKey startKey, SequenceNumber? asOfSequence = null)
    {
        _index = index;
        _start_key = startKey;
        _as_of_sequence = asOfSequence;
        _history_buffer = [];
        _history_position = -1;
    }

    #endregion

    #region 私有方法

    private void initialize_enumerator(DatabaseKey? startKey = null)
    {
        var key = startKey ?? _start_key;
        var entries = _index.range_scan(key, DatabaseKey.max_value, _as_of_sequence);
        _enumerator = entries.GetAsyncEnumerator();
    }

    #endregion

    #region 字段

    private readonly SequenceNumber? _as_of_sequence;
    private readonly List<DatabaseEntry> _history_buffer;
    private readonly IBTreeIndex _index;
    private readonly DatabaseKey _start_key;
    private bool _disposed;
    private IAsyncEnumerator<DatabaseEntry>? _enumerator;
    private int _history_position;

    #endregion

    #region ICursor 实现

    /// <inheritdoc />
    public (ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value) Current
    {
        get
        {
            if (_history_position >= 0 && _history_position < _history_buffer.Count)
            {
                var entry = _history_buffer[_history_position];
                return (entry.key.bytes, entry.value.bytes);
            }

            return (ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty);
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync()
    {
        if (_enumerator is null) initialize_enumerator();

        if (_enumerator != null && _enumerator.MoveNextAsync().GetAwaiter().GetResult())
        {
            var current = _enumerator.Current;
            _history_buffer.Add(current);
            if (_history_buffer.Count > _max_history_size)
                _history_buffer.RemoveAt(0);
            else
                _history_position = _history_buffer.Count - 1;

            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    /// <inheritdoc />
    public ValueTask<bool> MovePreviousAsync()
    {
        if (_history_buffer is not null && _history_position > 0)
        {
            _history_position--;
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    /// <inheritdoc />
    public async ValueTask SeekAsync(ReadOnlyMemory<byte> key)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        _enumerator = null;
        _history_buffer.Clear();
        _history_position = -1;
        initialize_enumerator(dbKey);
        await MoveNextAsync();
    }

    #endregion

    #region 内部 API

    /// <summary>
    ///     当前条目（内部 API）
    /// </summary>
    public DatabaseEntry current { get; private set; }

    /// <summary>
    ///     游标是否有效（内部 API）
    /// </summary>
    public bool is_valid => _enumerator is not null || _history_position >= 0;

    /// <summary>
    ///     向后移动（内部 API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> move_next(CancellationToken cancellationToken = default)
    {
        if (_enumerator is null) initialize_enumerator();

        if (_enumerator != null && await _enumerator.MoveNextAsync())
        {
            current = _enumerator.Current;
            _history_buffer.Add(current);
            if (_history_buffer.Count > _max_history_size)
                _history_buffer.RemoveAt(0);
            else
                _history_position = _history_buffer.Count - 1;

            return true;
        }

        return false;
    }

    /// <summary>
    ///     向前移动（内部 API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public ValueTask<bool> move_prev(CancellationToken cancellationToken = default)
    {
        if (_history_buffer is not null && _history_position > 0)
        {
            _history_position--;
            current = _history_buffer[_history_position];
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    /// <summary>
    ///     定位到第一个条目（内部 API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> seek_to_first(CancellationToken cancellationToken = default)
    {
        _enumerator = null;
        _history_buffer.Clear();
        _history_position = -1;
        initialize_enumerator(DatabaseKey.empty);
        return await move_next(cancellationToken);
    }

    /// <summary>
    ///     定位到最后一个条目（内部 API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> seek_to_last(CancellationToken cancellationToken = default)
    {
        var lastEntry = default(DatabaseEntry);
        var found = false;

        await foreach (var entry in _index.range_scan(DatabaseKey.empty, DatabaseKey.max_value,
                           _as_of_sequence).WithCancellation(cancellationToken))
        {
            lastEntry = entry;
            found = true;
        }

        if (found)
        {
            current = lastEntry;
            _history_buffer.Clear();
            _history_buffer.Add(current);
            _history_position = 0;
        }

        return found;
    }

    /// <summary>
    ///     定位到指定键（内部 API）
    /// </summary>
    /// <param name="key">目标键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> seek(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        _enumerator = null;
        _history_buffer.Clear();
        _history_position = -1;
        initialize_enumerator(key);
        return await move_next(cancellationToken);
    }

    /// <summary>
    ///     获取范围内的条目（内部 API）
    /// </summary>
    /// <param name="start">起始键</param>
    /// <param name="end">结束键</param>
    /// <param name="limit">限制数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>条目列表</returns>
    public async ValueTask<IReadOnlyList<DatabaseEntry>> get_range(DatabaseKey start, DatabaseKey end,
        int limit = 1000, CancellationToken cancellationToken = default)
    {
        var result = new List<DatabaseEntry>();
        await foreach (var entry in _index.range_scan(start, end, _as_of_sequence)
                           .WithCancellation(cancellationToken))
        {
            result.Add(entry);
            if (result.Count >= limit) break;
        }

        return result;
    }

    /// <summary>
    ///     获取具有指定前缀的条目（内部 API）
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <param name="limit">限制数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>条目列表</returns>
    public async ValueTask<IReadOnlyList<DatabaseEntry>> get_prefix(DatabaseKey prefix, int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DatabaseEntry>();
        await foreach (var entry in _index.prefix_scan(prefix, _as_of_sequence)
                           .WithCancellation(cancellationToken))
        {
            result.Add(entry);
            if (result.Count >= limit) break;
        }

        return result;
    }

    #endregion

    #region 资源释放

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        _enumerator?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步释放资源
    /// </summary>
    public async ValueTask dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_enumerator is not null) await _enumerator.DisposeAsync();
    }

    #endregion
}