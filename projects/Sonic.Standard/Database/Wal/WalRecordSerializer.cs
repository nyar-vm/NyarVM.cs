using System.IO.Compression;
using Std.Database.Core;

namespace Std.Database.Wal;

/// <summary>
///     WAL 记录序列化/反序列化工具，支持 Brotli 压缩
/// </summary>
internal static class WalRecordSerializer
{
    /// <summary>
    ///     压缩标志位，存储在长度字段的最高位（bit 31）
    /// </summary>
    public const int compression_flag = unchecked((int)0x80000000);

    /// <summary>
    ///     长度掩码，去掉压缩标志位
    /// </summary>
    public const int length_mask = 0x7FFFFFFF;

    /// <summary>
    ///     序列化 WAL 记录
    /// </summary>
    public static byte[] serialize(WalRecord record)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(record.sequence.value);
        writer.Write(record.transaction_id.value);
        writer.Write((byte)record.operation_type);
        writer.Write(record.key.length);
        writer.Write(record.key.bytes.ToArray());

        if (record.old_value is not null)
        {
            writer.Write(true);
            writer.Write(record.old_value.Value.length);
            writer.Write(record.old_value.Value.bytes.ToArray());
        }
        else
        {
            writer.Write(false);
        }

        if (record.new_value is not null)
        {
            writer.Write(true);
            writer.Write(record.new_value.Value.length);
            writer.Write(record.new_value.Value.bytes.ToArray());
        }
        else
        {
            writer.Write(false);
        }

        writer.Write(record.timestamp.ToBinary());

        var data = ms.ToArray();
        var checksum = compute_crc32(data);
        writer.Write(checksum);

        return ms.ToArray();
    }

    /// <summary>
    ///     使用 Brotli 压缩数据
    /// </summary>
    public static byte[] compress(byte[] data)
    {
        using var output = new MemoryStream();
        using var compressor = new BrotliStream(output, CompressionLevel.Fastest);
        compressor.Write(data, 0, data.Length);
        compressor.Flush();
        return output.ToArray();
    }

    /// <summary>
    ///     解压 Brotli 压缩的数据
    /// </summary>
    public static byte[] decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using var decompressor = new BrotliStream(input, CompressionMode.Decompress);
        decompressor.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    ///     反序列化 WAL 记录
    /// </summary>
    public static WalRecord deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var sequence = new SequenceNumber(reader.ReadUInt64());
        var transactionId = new TransactionId(reader.ReadUInt64());
        var operationType = (WalOperationType)reader.ReadByte();

        var keyLength = reader.ReadInt32();
        var keyBytes = reader.ReadBytes(keyLength);
        var key = new DatabaseKey(keyBytes);

        DatabaseValue? oldValue = null;
        if (reader.ReadBoolean())
        {
            var oldLength = reader.ReadInt32();
            var oldBytes = reader.ReadBytes(oldLength);
            oldValue = new DatabaseValue(oldBytes);
        }

        DatabaseValue? newValue = null;
        if (reader.ReadBoolean())
        {
            var newLength = reader.ReadInt32();
            var newBytes = reader.ReadBytes(newLength);
            newValue = new DatabaseValue(newBytes);
        }

        var timestamp = DateTime.FromBinary(reader.ReadInt64());
        var checksum = reader.ReadUInt32();

        return new WalRecord
        {
            sequence = sequence,
            transaction_id = transactionId,
            operation_type = operationType,
            key = key,
            old_value = oldValue,
            new_value = newValue,
            timestamp = timestamp,
            checksum = checksum
        };
    }

    /// <summary>
    ///     验证 WAL 记录的校验和
    /// </summary>
    /// <param name="data">原始数据（含校验和）</param>
    /// <returns>校验和是否正确</returns>
    public static bool verify_checksum(byte[] data)
    {
        if (data.Length < sizeof(uint)) return false;

        var payloadLength = data.Length - sizeof(uint);
        var payload = data.AsSpan(..payloadLength);
        var storedChecksum = BitConverter.ToUInt32(data, payloadLength);
        var computedChecksum = compute_crc32(payload);

        return storedChecksum == computedChecksum;
    }

    /// <summary>
    ///     计算 CRC-32 校验和（ISO 3309 多项式 0xEDB88320）
    /// </summary>
    private static uint compute_crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFF;

        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) * 0xEDB88320u);
        }

        return ~crc;
    }
}