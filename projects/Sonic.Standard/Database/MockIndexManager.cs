using Core.Database;

namespace Std.Database;

/// <summary>
///     模拟索引管理器实现
/// </summary>
internal sealed class MockIndexManager : IIndexManager
{
    private readonly HashSet<string> _indexes = [];

    public Task CreateIndexAsync(string name, ReadOnlyMemory<byte> prefix, IndexOptions options)
    {
        _indexes.Add(name);
        return Task.CompletedTask;
    }

    public Task DropIndexAsync(string name)
    {
        _indexes.Remove(name);
        return Task.CompletedTask;
    }

    public Task<bool> IndexExistsAsync(string name)
    {
        return Task.FromResult(_indexes.Contains(name));
    }
}