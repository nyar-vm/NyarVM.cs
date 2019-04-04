using Core.Database;

namespace Std.Database;

/// <summary>
///     模拟快照实现，基于创建时的字典副本
/// </summary>
internal sealed class MockSnapshot : ISnapshot
{
    private readonly Dictionary<byte[], byte[]> _snapshot_data;
    private bool _disposed;

    public MockSnapshot(Dictionary<byte[], byte[]> data)
    {
        _snapshot_data = new Dictionary<byte[], byte[]>(data, new MockDb.ByteArrayComparer());
    }

    public ReadOnlyMemory<byte>? Get(ReadOnlyMemory<byte> key)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MockSnapshot));

        var keyArray = key.ToArray();
        if (_snapshot_data.TryGetValue(keyArray, out var value)) return value;
        return null;
    }

    public ICursor CreateCursor()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MockSnapshot));

        return new MockCursor(_snapshot_data);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _snapshot_data.Clear();
    }
}