using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class LightCollectionTests : IDisposable
{
    private readonly LightDatabase _db;

    public LightCollectionTests()
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
    public async Task InsertAsync_ShouldReturnKey()
    {
        var collection = _db.get_collection<TestPerson>("people");
        var person = new TestPerson { Name = "Alice", Age = 30 };

        var key = await collection.InsertAsync(person);

        Assert.False(key.is_empty);
    }

    [Fact]
    public async Task FindAsync_AfterInsert_ShouldReturnDocument()
    {
        var collection = _db.get_collection<TestPerson>("people");
        var person = new TestPerson { Name = "Bob", Age = 25 };
        var key = await collection.InsertAsync(person);

        var result = await collection.FindAsync(key);

        Assert.NotNull(result);
        Assert.Equal("Bob", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public async Task FindAsync_WithNonexistentKey_ShouldReturnNull()
    {
        var collection = _db.get_collection<TestPerson>("people");

        var result = await collection.FindAsync(DatabaseKey.from_guid(Guid.NewGuid()));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyDocument()
    {
        var collection = _db.get_collection<TestPerson>("people");
        var person = new TestPerson { Name = "Charlie", Age = 20 };
        var key = await collection.InsertAsync(person);

        var updated = new TestPerson { Name = "Charlie Updated", Age = 21 };
        var success = await collection.UpdateAsync(key, updated);

        Assert.True(success);
        var result = await collection.FindAsync(key);
        Assert.NotNull(result);
        Assert.Equal("Charlie Updated", result.Name);
        Assert.Equal(21, result.Age);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDocument()
    {
        var collection = _db.get_collection<TestPerson>("people");
        var person = new TestPerson { Name = "David", Age = 40 };
        var key = await collection.InsertAsync(person);

        var deleted = await collection.DeleteAsync(key);

        Assert.True(deleted);
        var result = await collection.FindAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonexistentKey_ShouldReturnFalse()
    {
        var collection = _db.get_collection<TestPerson>("people");

        var deleted = await collection.DeleteAsync(DatabaseKey.from_guid(Guid.NewGuid()));

        Assert.False(deleted);
    }

    [Fact]
    public async Task Name_ShouldReturnCollectionName()
    {
        var collection = _db.get_collection<TestPerson>("people");

        Assert.Equal("people", collection.Name);
    }

    [Fact]
    public async Task CreateIndexAsync_ShouldNotThrow()
    {
        var collection = _db.get_collection<TestPerson>("people");

        await collection.CreateIndexAsync(p => p.Name, "name_idx");
    }

    private sealed class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}