using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class MvccTests : IDisposable
{
    private readonly LightDatabase _db;

    public MvccTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_mvcc_test_{Guid.NewGuid():N}");
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
    public async Task Snapshot_ShouldSeeCommittedData()
    {
        await _db.put("key1", "value1");

        var snapshot = _db.create_snapshot();
        var result = await snapshot.get<string>("key1");

        Assert.Equal("value1", result);
    }

    [Fact]
    public async Task Snapshot_ShouldNotSeeUncommittedData()
    {
        await _db.put("key1", "committed");

        var snapshot = _db.create_snapshot();

        var tx = await _db.begin_transaction();
        await tx.put("key1", "uncommitted");

        var result = await snapshot.get<string>("key1");

        Assert.Equal("committed", result);

        await tx.rollback();
    }

    [Fact]
    public async Task Snapshot_AfterCommit_ShouldSeeNewValue()
    {
        await _db.put("key1", "old");

        var snapshot1 = _db.create_snapshot();

        var tx = await _db.begin_transaction();
        await tx.put("key1", "new");
        await tx.commit();

        var snapshot2 = _db.create_snapshot();

        var oldResult = await snapshot1.GetAsync<string>("key1");
        var newResult = await snapshot2.GetAsync<string>("key1");

        Assert.Equal("old", oldResult);
        Assert.Equal("new", newResult);
    }

    [Fact]
    public async Task Transaction_ReadOwnWrites_ShouldWork()
    {
        var tx = await _db.begin_transaction();

        await tx.put("key1", "tx_value");
        var result = await tx.get<string>("key1");

        Assert.Equal("tx_value", result);

        await tx.rollback();
    }

    [Fact]
    public async Task Transaction_DeleteInTx_ShouldHideKey()
    {
        await _db.put("key1", "original");

        var tx = await _db.begin_transaction();
        await tx.delete("key1");
        var result = await tx.get<string>("key1");

        Assert.Null(result);

        await tx.rollback();
    }

    [Fact]
    public async Task Transaction_Commit_ShouldBeVisible()
    {
        var tx = await _db.begin_transaction();
        await tx.put("key1", "committed_value");
        await tx.commit();

        var result = await _db.get<string>("key1");

        Assert.Equal("committed_value", result);
    }

    [Fact]
    public async Task Transaction_Rollback_ShouldNotBeVisible()
    {
        await _db.put("key1", "original");

        var tx = await _db.begin_transaction();
        await tx.put("key1", "rolled_back");
        await tx.rollback();

        var result = await _db.get<string>("key1");

        Assert.Equal("original", result);
    }

    [Fact]
    public async Task MultipleSnapshots_ShouldBeIndependent()
    {
        await _db.put("key1", "v1");

        var snapshot1 = _db.create_snapshot();

        await _db.put("key1", "v2");

        var snapshot2 = _db.create_snapshot();

        await _db.put("key1", "v3");

        var snapshot3 = _db.create_snapshot();

        Assert.Equal("v1", await snapshot1.GetAsync<string>("key1"));
        Assert.Equal("v2", await snapshot2.GetAsync<string>("key1"));
        Assert.Equal("v3", await snapshot3.GetAsync<string>("key1"));
    }

    [Fact]
    public async Task Cursor_Snapshot_ShouldSeePointInTimeData()
    {
        for (var i = 0; i < 5; i++) await _db.put($"key{i}", $"value{i}");

        var snapshot = _db.create_snapshot();

        await _db.put("key2", "updated");
        await _db.delete("key3");

        using var cursor = snapshot.seek("key0");
        var results = new List<string>();

        while (await cursor.MoveNextAsync()) results.Add(cursor.Current.Key.ToString());

        Assert.Equal(5, results.Count);
        Assert.Contains("key2", results);
        Assert.Contains("key3", results);
    }
}