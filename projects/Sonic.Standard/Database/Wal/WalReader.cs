using Std.Database.Core;

namespace Std.Database.Wal;

/// <summary>
///     WAL 读取器实现，支持校验和验证与压缩记录自动解压
/// </summary>
internal sealed class WalReader : IWalReader, IDisposable
{
    private static readonly int _compression_flag = unchecked((int)0x80000000);
    private static readonly int _length_mask = 0x7FFFFFFF;
    private readonly bool _verify_checksums;

    private readonly string _wal_path;

    /// <summary>
    ///     创建 WAL 读取器
    /// </summary>
    /// <param name="walPath">WAL 文件路径</param>
    /// <param name="verifyChecksums">是否验证校验和</param>
    public WalReader(string walPath, bool verifyChecksums = true)
    {
        _wal_path = walPath;
        _verify_checksums = verifyChecksums;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<WalRecord> read_from(SequenceNumber startSequence)
    {
        if (!File.Exists(_wal_path)) yield break;

        await using var stream = new FileStream(_wal_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);

        while (stream.Position < stream.Length)
        {
            var rawLength = reader.ReadInt32();
            if (rawLength is <= 0 or > 16 * 1024 * 1024) break;

            var isCompressed = (rawLength & _compression_flag) != 0;
            var actualLength = rawLength & _length_mask;
            var rawBuffer = reader.ReadBytes(actualLength);
            if (rawBuffer.Length < actualLength) break;

            byte[] decompressedBuffer;
            if (isCompressed)
                decompressedBuffer = WalRecordSerializer.decompress(rawBuffer);
            else
                decompressedBuffer = rawBuffer;

            if (_verify_checksums && !WalRecordSerializer.verify_checksum(decompressedBuffer)) break;

            var record = WalRecordSerializer.deserialize(decompressedBuffer);
            if (record.sequence.value >= startSequence.value) yield return record;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<WalRecord> read_all()
    {
        await foreach (var record in read_from(SequenceNumber.zero)) yield return record;
    }
}