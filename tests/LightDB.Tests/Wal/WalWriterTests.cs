using Std.Database.Core;
using Std.Database.Wal;

namespace LightDB.Tests.Wal;

public sealed class WalWriterTests : IDisposable
{
    private readonly string _walPath;

    public WalWriterTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"LightDB_wal_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        _walPath = Path.Combine(tempDir, "test.wal");
    }

    public void Dispose()
    {
        try
        {
            var dir = Path.GetDirectoryName(_walPath);
            if (dir is not null && Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task AppendAsync_ShouldWriteRecord()
    {
        using var writer = new WalWriter(_walPath);

        var record = new WalRecord
        {
            Sequence = new SequenceNumber(1),
            TransactionId = TransactionId.Min,
            OperationType = WalOperationType.Update,
            Key = DatabaseKey.from_string("key1"),
            NewValue = DatabaseValue.from_string("value1")
        };

        await writer.Append(record);
        await writer.Flush();

        Assert.True(File.Exists(_walPath));
        Assert.True(new FileInfo(_walPath).Length > 0);
    }

    [Fact]
    public async Task AppendAsync_MultipleRecords_ShouldWriteAll()
    {
        using var writer = new WalWriter(_walPath);

        for (var i = 0UL; i < 5; i++)
        {
            var record = new WalRecord
            {
                Sequence = new SequenceNumber(i + 1),
                TransactionId = TransactionId.Min,
                OperationType = WalOperationType.Update,
                Key = DatabaseKey.from_string($"key{i}"),
                NewValue = DatabaseValue.from_string($"value{i}")
            };

            await writer.Append(record);
        }

        await writer.Flush();

        Assert.True(new FileInfo(_walPath).Length > 0);
    }

    [Fact]
    public async Task CurrentSequence_ShouldIncrement()
    {
        using var writer = new WalWriter(_walPath);

        Assert.Equal(SequenceNumber.Zero, writer.CurrentSequence);

        await writer.Append(new WalRecord
        {
            Sequence = new SequenceNumber(1),
            TransactionId = TransactionId.Min,
            OperationType = WalOperationType.Update,
            Key = DatabaseKey.empty
        });

        Assert.True(writer.CurrentSequence.Value > SequenceNumber.Zero.Value);
    }

    [Fact]
    public async Task FlushAsync_ShouldNotThrow()
    {
        using var writer = new WalWriter(_walPath);

        await writer.Append(new WalRecord
        {
            Sequence = new SequenceNumber(1),
            TransactionId = TransactionId.Min,
            OperationType = WalOperationType.Checkpoint,
            Key = DatabaseKey.empty
        });

        await writer.Flush();
    }
}