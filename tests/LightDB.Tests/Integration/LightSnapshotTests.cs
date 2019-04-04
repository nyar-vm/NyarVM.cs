using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class LightSnapshotTests : IDisposable
{
    private readonly LightDatabase _db;

    public LightSnapshotTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_test_{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions { path = tempPath });
    }

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            if (Directory.Exists(_db.options.path)) Directory.Delete(_db.options.path, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task CreateSnapshot_ShouldReturnSnapshot()
    {
        var snapshot = _db.create_snapshot();

        Assert.NotNull(snapshot);
        Assert.True(snapshot.sequence.value >= 0);

        snapshot.Dispose();
    }

    [Fact]
    public async Task GetAsync_ShouldReadCommittedData()
    {
        await _db.put(DatabaseKey.from_string("key1"), "value1");

        var snapshot = _db.create_snapshot();
        var result = await snapshot.get<string>(DatabaseKey.from_string("key1"));

        Assert.Equal("value1", result);

        snapshot.Dispose();
    }

    [Fact]
    public async Task GetAsync_WithMissingKey_ShouldReturnDefault()
    {
        var snapshot = _db.create_snapshot();
        var result = await snapshot.get<string>(DatabaseKey.from_string("nonexistent"));

        Assert.Null(result);

        snapshot.Dispose();
    }

    [Fact]
    public async Task Seek_ShouldReturnCursor()
    {
        await _db.put(DatabaseKey.from_string("key1"), "value1");

        var snapshot = _db.create_snapshot();
        var cursor = snapshot.seek(DatabaseKey.from_string("key1"));

        Assert.NotNull(cursor);

        cursor.Dispose();
        snapshot.Dispose();
    }

    [Fact]
    public async Task CreateChild_ShouldReturnNewSnapshot()
    {
        var snapshot = _db.create_snapshot();
        var child = snapshot.create_child();

        Assert.NotNull(child);
        Assert.Equal(snapshot.sequence, child.Sequence);

        child.Dispose();
        snapshot.Dispose();
    }
}