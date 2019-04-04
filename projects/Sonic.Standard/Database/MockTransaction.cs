using Core.Database;

namespace Std.Database;

/// <summary>
///     模拟事务实现，记录操作并在提交时应用到内部字典
/// </summary>
internal sealed class MockTransaction : ITransaction
{
    private readonly MockDb _db;
    private readonly List<ReadOnlyMemory<byte>> _deletes;
    private readonly List<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)> _puts;
    private bool _committed;
    private bool _disposed;
    private bool _rolled_back;

    public MockTransaction(MockDb db, IsolationLevel isolationLevel)
    {
        _db = db;
        IsolationLevel = isolationLevel;
        _puts = [];
        _deletes = [];
    }

    public IsolationLevel IsolationLevel { get; }

    public Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key)
    {
        return _db.GetAsync(key);
    }

    public Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        _puts.Add((key, value));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ReadOnlyMemory<byte> key)
    {
        _deletes.Add(key);
        return Task.CompletedTask;
    }

    public async Task CommitAsync()
    {
        if (_committed || _rolled_back) throw new InvalidOperationException("事务已经完成");

        foreach (var (key, value) in _puts) await _db.PutAsync(key, value);

        foreach (var key in _deletes) await _db.DeleteAsync(key);

        _committed = true;
    }

    public Task RollbackAsync()
    {
        if (_committed || _rolled_back) throw new InvalidOperationException("事务已经完成");

        _puts.Clear();
        _deletes.Clear();
        _rolled_back = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _puts.Clear();
        _deletes.Clear();
    }
}