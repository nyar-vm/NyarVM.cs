using System;
using System.Threading.Tasks;

namespace Core.Database;

/// <summary>
///     双向游标接口，支持键值遍历
/// </summary>
public interface ICursor : IDisposable
{
    /// <summary>
    ///     当前键值对
    /// </summary>
    (ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value) Current { get; }

    /// <summary>
    ///     向后移动
    /// </summary>
    ValueTask<bool> MoveNextAsync();

    /// <summary>
    ///     向前移动
    /// </summary>
    ValueTask<bool> MovePreviousAsync();

    /// <summary>
    ///     定位到指定键
    /// </summary>
    ValueTask SeekAsync(ReadOnlyMemory<byte> key);
}