using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Database;

/// <summary>
///     数据库缓存接口
/// </summary>
public interface IDatabaseCache
{
    /// <summary>
    ///     读取缓存
    /// </summary>
    ValueTask<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     写入缓存
    /// </summary>
    ValueTask PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value, int ttlSeconds = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除缓存条目
    /// </summary>
    ValueTask RemoveAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     判断键是否存在且未过期
    /// </summary>
    ValueTask<bool> ContainsAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);
}