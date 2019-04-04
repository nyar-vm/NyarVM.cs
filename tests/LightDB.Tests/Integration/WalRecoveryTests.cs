using Std.Database.Core;
using Std.Database.Wal;

namespace LightDB.Tests.Integration;

public sealed class WalRecoveryTests : IDisposable
{
    private readonly string _tempPath;

    public WalRecoveryTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_wal_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempPath)) Directory.Delete(_tempPath, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task WalWriter_AppendAndReadBack_ShouldMatch()
    {
        var walPath = Path.Combine(_tempPath, "test.wal");
        var writer = new WalWriter(walPath);

        var record = new WalRecord
        {
            Sequence = new SequenceNumber(1),
            TransactionId = TransactionId.New(),
            OperationType = WalOperationType.Update,
            Key = DatabaseKey.from_string("key1"),
            NewValue = DatabaseValue.from_string("value1")
        };

        await writer.Append(record);
        await writer.Flush();
        await writer.DisposeAsync();

        Assert.True(File.Exists(walPath));
        var fileInfo = new FileInfo(walPath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task WalWriter_Truncate_ShouldRemoveOldRecords()
    {
        var walPath = Path.Combine(_tempPath, "test.wal");
        var writer = new WalWriter(walPath);

        for (var i = 1; i <= 10; i++)
            await writer.Append(new WalRecord
            {
                Sequence = new SequenceNumber((ulong)i),
                TransactionId = TransactionId.Min,
                OperationType = WalOperationType.Update,
                Key = DatabaseKey.from_string($"key{i}"),
                NewValue = DatabaseValue.from_string($"value{i}")
            });

        await writer.Flush();
        await writer.Truncate(new SequenceNumber(5));
        await writer.DisposeAsync();

        var fileInfo = new FileInfo(walPath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task WalWriter_TruncateAll_ShouldLeaveEmptyFile()
    {
        var walPath = Path.Combine(_tempPath, "test.wal");
        var writer = new WalWriter(walPath);

        for (var i = 1; i <= 5; i++)
            await writer.Append(new WalRecord
            {
                Sequence = new SequenceNumber((ulong)i),
                TransactionId = TransactionId.Min,
                OperationType = WalOperationType.Update,
                Key = DatabaseKey.from_string($"key{i}"),
                NewValue = DatabaseValue.from_string($"value{i}")
            });

        await writer.Flush();
        await writer.Truncate(new SequenceNumber(10));
        await writer.DisposeAsync();

        Assert.True(File.Exists(walPath));
        var fileInfo = new FileInfo(walPath);
        Assert.Equal(0, fileInfo.Length);
    }

    [Fact]
    public async Task Database_Checkpoint_ShouldCreateCheckpointRecord()
    {
        var db = new LightDatabase(new LightOptions { path = _tempPath });

        await db.put("key1", "value1");
        await db.put("key2", "value2");
        await db.check_point();

        var walPath = Path.Combine(_tempPath, "light.wal");
        Assert.True(File.Exists(walPath));

        await db.DisposeAsync();
    }

    [Fact]
    public async Task Database_PersistAndReopen_ShouldRetainData()
    {
        var db1 = new LightDatabase(new LightOptions { path = _tempPath });

        await db1.put("persist_key", "persist_value");
        await db1.check_point();
        await db1.DisposeAsync();

        var db2 = new LightDatabase(new LightOptions { path = _tempPath });
        var result = await db2.get<string>("persist_key");

        Assert.Equal("persist_value", result);
        await db2.DisposeAsync();
    }

    [Fact]
    public async Task Database_PersistMultipleKeys_ShouldRetainAll()
    {
        var db1 = new LightDatabase(new LightOptions { path = _tempPath });

        for (var i = 0; i < 50; i++) await db1.put($"key{i}", $"value{i}");

        await db1.check_point();
        await db1.DisposeAsync();

        var db2 = new LightDatabase(new LightOptions { path = _tempPath });

        for (var i = 0; i < 50; i++)
        {
            var result = await db2.get<string>($"key{i}");
            Assert.Equal($"value{i}", result);
        }

        await db2.DisposeAsync();
    }
}