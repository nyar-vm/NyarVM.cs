using Core.Database;

namespace Std.Database;

/// <summary>
///     内存数据库索引管理器实现
/// </summary>
internal sealed class MemoryIndexManager : IIndexManager
{
    private readonly HashSet<string> _indexes = [];

    /// <summary>
    ///     创建索引
    /// </summary>
    /// <param name="name">索引名称</param>
    /// <param name="prefix">键前缀</param>
    /// <param name="options">索引选项</param>
    public Task CreateIndexAsync(string name, ReadOnlyMemory<byte> prefix, IndexOptions options)
    {
        _indexes.Add(name);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     删除索引
    /// </summary>
    /// <param name="name">索引名称</param>
    public Task DropIndexAsync(string name)
    {
        _indexes.Remove(name);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     判断索引是否存在
    /// </summary>
    /// <param name="name">索引名称</param>
    /// <returns>是否存在</returns>
    public Task<bool> IndexExistsAsync(string name)
    {
        return Task.FromResult(_indexes.Contains(name));
    }
}