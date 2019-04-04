using Core.Database;
using Std.Database.Core;
using Std.Database.Index;

namespace Std.Database;

/// <summary>
///     LightDB 快照实现，基于 MVCC 序列号提供时间点一致性读取
/// </summary>
internal sealed class LightSnapshot : IDisposable
{
    #region 字段

    private readonly IBTreeIndex _index;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建快照
    /// </summary>
    /// <param name="sequence">快照序列号</param>
    /// <param name="index">索引</param>
    internal LightSnapshot(SequenceNumber sequence, IBTreeIndex index)
    {
        this.sequence = sequence;
        _index = index;
    }

    #endregion

    #region 属性

    /// <summary>
    ///     快照序列号
    /// </summary>
    public SequenceNumber sequence { get; }

    #endregion

    #region 资源释放

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #endregion

    #region 内部 API

    /// <summary>
    ///     异步获取键值（内部 API）
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>值，若不存在则返回 null</returns>
    public async ValueTask<TValue?> get<TValue>(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        var value = await _index.search(key, sequence);
        if (value is null) return default;

        return value.Value.to_object<TValue>();
    }

    /// <summary>
    ///     定位到指定键的游标（内部 API）
    /// </summary>
    /// <param name="key">起始键</param>
    /// <returns>游标实例</returns>
    public LightCursor seek(DatabaseKey key)
    {
        return new LightCursor(_index, key, sequence);
    }

    /// <summary>
    ///     创建子快照（内部 API）
    /// </summary>
    /// <returns>子快照实例</returns>
    public LightSnapshot create_child()
    {
        return new LightSnapshot(sequence, _index);
    }

    /// <summary>
    ///     创建适配 Sonic.Core ICursor 接口的游标（供 LightSnapshotAdapter 调用）
    /// </summary>
    /// <returns>游标实例</returns>
    public ICursor create_cursor_adapter()
    {
        return new LightCursor(_index, DatabaseKey.empty, sequence);
    }

    #endregion
}