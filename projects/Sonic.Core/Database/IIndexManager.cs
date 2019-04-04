using System;
using System.Threading.Tasks;

namespace Core.Database;

/// <summary>
///     索引管理器接口
/// </summary>
public interface IIndexManager
{
    /// <summary>
    ///     创建索引
    /// </summary>
    Task CreateIndexAsync(string name, ReadOnlyMemory<byte> prefix, IndexOptions options);

    /// <summary>
    ///     删除索引
    /// </summary>
    Task DropIndexAsync(string name);

    /// <summary>
    ///     判断索引是否存在
    /// </summary>
    Task<bool> IndexExistsAsync(string name);
}