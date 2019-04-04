using Std.Database.Core;
using Std.Database.Wal;

namespace LightDB.Tests.Wal;

public sealed class WalReaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _walPath;

    public WalReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_walr_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _walPath = Path.Combine(_tempDir, "test.wal");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task ReadAllAsync_WithNoFile_ShouldReturnEmpty()
    {
        using var reader = new WalReader(Path.Combine(_tempDir, "nonexistent.wal"));

        var records = new List<WalRecord>();
        await foreach (var record in reader.ReadAllAsync()) records.Add(record);

        Assert.Empty(records);
    }

    [Fact]
    public async Task ReadAllAsync_AfterWrite_ShouldReturnRecords()
    {
        using (var writer = new WalWriter(_walPath))
        {
            await writer.Append(new WalRecord
            {
                Sequence = new SequenceNumber(1),
                TransactionId = TransactionId.Min,
                OperationType = WalOperationType.Update,
                Key = DatabaseKey.from_string("key1"),
                NewValue = DatabaseValue.from_string("value1")
            });

            await writer.Flush();
        }

        using var reader = new WalReader(_walPath);

        var records = new List<WalRecord>();
        await foreach (var record in reader.ReadAllAsync()) records.Add(record);

        Assert.Single(records);
        Assert.Equal("key1", records[0].Key.ToString());
    }

    [Fact]
    public async Task ReadAllAsync_MultipleRecords_ShouldReturnAll()
    {
        using (var writer = new WalWriter(_walPath))
        {
            for (var i = 0UL; i < 3; i++)
                await writer.Append(new WalRecord
                {
                    Sequence = new SequenceNumber(i + 1),
                    TransactionId = TransactionId.Min,
                    OperationType = WalOperationType.Update,
                    Key = DatabaseKey.from_string($"key{i}"),
                    NewValue = DatabaseValue.from_string($"value{i}")
                });

            await writer.Flush();
        }

        using var reader = new WalReader(_walPath);

        var records = new List<WalRecord>();
        await foreach (var record in reader.ReadAllAsync()) records.Add(record);

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public async Task ReadFromAsync_WithStartSequence_ShouldFilterRecords()
    {
        using (var writer = new WalWriter(_walPath))
        {
            for (var i = 0UL; i < 5; i++)
                await writer.Append(new WalRecord
                {
                    Sequence = new SequenceNumber(i + 1),
                    TransactionId = TransactionId.Min,
                    OperationType = WalOperationType.Update,
                    Key = DatabaseKey.from_string($"key{i}"),
                    NewValue = DatabaseValue.from_string($"value{i}")
                });

            await writer.Flush();
        }

        using var reader = new WalReader(_walPath);

        var records = new List<WalRecord>();
        await foreach (var record in reader.ReadFromAsync(new SequenceNumber(3))) records.Add(record);

        Assert.Equal(3, records.Count);
        Assert.All(records, r => Assert.True(r.Sequence.Value >= 3));
    }

    [Fact]
    public async Task Roundtrip_WithDeleteRecord_ShouldPreserveOperationType()
    {
        using (var writer = new WalWriter(_walPath))
        {
            await writer.Append(new WalRecord
            {
                Sequence = new SequenceNumber(1),
                TransactionId = TransactionId.Min,
                OperationType = WalOperationType.Delete,
                Key = DatabaseKey.from_string("key1"),
                OldValue = DatabaseValue.from_string("old_value")
            });

            await writer.Flush();
        }

        using var reader = new WalReader(_walPath);

        var records = new List<WalRecord>();
        await foreach (var record in reader.ReadAllAsync()) records.Add(record);

        Assert.Single(records);
        Assert.Equal(WalOperationType.Delete, records[0].OperationType);
        Assert.NotNull(records[0].OldValue);
    }
}