using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class LightDatabaseKvTests : IDisposable
{
    private readonly LightDatabase _db;

    public LightDatabaseKvTests()
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
    public async Task PutAndGet_ShouldRoundtrip()
    {
        var key = DatabaseKey.from_string("greeting");

        await _db.put(key, "hello world");
        var result = await _db.get<string>(key);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async Task Get_WithNonexistentKey_ShouldReturnDefault()
    {
        var key = DatabaseKey.from_string("nonexistent");

        var result = await _db.get<string>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task Put_WithIntegerValue_ShouldRoundtrip()
    {
        var key = DatabaseKey.from_string("count");

        await _db.put(key, 42);
        var result = await _db.get<int>(key);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Put_WithComplexObject_ShouldRoundtrip()
    {
        var obj = new TestDoc { Name = "test", Value = 123 };
        var key = DatabaseKey.from_string("doc:1");

        await _db.put(key, obj);
        var result = await _db.get<TestDoc>(key);

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task Put_Overwrite_ShouldUpdateValue()
    {
        var key = DatabaseKey.from_string("key1");

        await _db.put(key, "v1");
        await _db.put(key, "v2");
        var result = await _db.get<string>(key);

        Assert.Equal("v2", result);
    }

    [Fact]
    public async Task Delete_ShouldRemoveKey()
    {
        var key = DatabaseKey.from_string("key1");

        await _db.put(key, "value");
        var deleted = await _db.delete(key);

        Assert.True(deleted);
        var result = await _db.get<string>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_WithNonexistentKey_ShouldReturnFalse()
    {
        var key = DatabaseKey.from_string("nonexistent");

        var deleted = await _db.delete(key);

        Assert.False(deleted);
    }

    [Fact]
    public async Task Exists_WithPresentKey_ShouldReturnTrue()
    {
        var key = DatabaseKey.from_string("key1");

        await _db.put(key, "value");
        var exists = await _db.contains(key);

        Assert.True(exists);
    }

    [Fact]
    public async Task Exists_WithMissingKey_ShouldReturnFalse()
    {
        var key = DatabaseKey.from_string("nonexistent");

        var exists = await _db.contains(key);

        Assert.False(exists);
    }

    [Fact]
    public async Task Exists_AfterDelete_ShouldReturnFalse()
    {
        var key = DatabaseKey.from_string("key1");

        await _db.put(key, "value");
        await _db.delete(key);
        var exists = await _db.contains(key);

        Assert.False(exists);
    }

    [Fact]
    public async Task MultipleKeys_ShouldWorkIndependently()
    {
        await _db.put(DatabaseKey.from_string("a"), 1);
        await _db.put(DatabaseKey.from_string("b"), 2);
        await _db.put(DatabaseKey.from_string("c"), 3);

        Assert.Equal(1, await _db.get<int>(DatabaseKey.from_string("a")));
        Assert.Equal(2, await _db.get<int>(DatabaseKey.from_string("b")));
        Assert.Equal(3, await _db.get<int>(DatabaseKey.from_string("c")));
    }

    [Fact]
    public async Task Statistics_ShouldTrackOperations()
    {
        await _db.put(DatabaseKey.from_string("k1"), "v1");
        await _db.put(DatabaseKey.from_string("k2"), "v2");
        await _db.get<string>(DatabaseKey.from_string("k1"));
        await _db.delete(DatabaseKey.from_string("k2"));

        Assert.Equal(2, _db.statistics.write_count);
        Assert.Equal(1, _db.statistics.read_count);
        Assert.Equal(1, _db.statistics.delete_count);
    }

    [Fact]
    public void Name_ShouldReturnLightDB()
    {
        Assert.Equal("LightDB", _db.name);
    }

    private sealed class TestDoc
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}