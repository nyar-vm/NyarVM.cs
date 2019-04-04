using System;

namespace Core.Database;

/// <summary>
///     快照接口，提供时间点一致性读取
/// </summary>
public interface ISnapshot : IDisposable
{
    /// <summary>
    ///     获取键值
    /// </summary>
    ReadOnlyMemory<byte>? Get(ReadOnlyMemory<byte> key);

    /// <summary>
    ///     创建游标
    /// </summary>
    ICursor CreateCursor();
}