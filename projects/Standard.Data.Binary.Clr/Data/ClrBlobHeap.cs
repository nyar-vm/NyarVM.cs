namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Blob 堆（#Blob）的
/// </summary>
public sealed class ClrBlobHeap
{
    /// <summary>
    ///     原始数据的
    /// </summary>
    public byte[] data { get; init; } = [];

    /// <summary>
    ///     按偏移量读取 Blob（先读取压缩长度前缀，再读取数据）的
    /// </summary>
    public ReadOnlySpan<byte> read_blob(uint offset)
    {
        if (offset == 0 || offset >= data.Length) return [];

        var pos = (int)offset;
        var length = decode_blob_length(data, ref pos);

        return data.AsSpan(pos, length);
    }

    /// <summary>
    ///     解码 Blob 堆的压缩长度前缀（ECMA-335 §23.2.4）的
    /// </summary>
    internal static int decode_blob_length(byte[] data, ref int pos)
    {
        var first = data[pos];

        if ((first & 0x80) == 0)
        {
            pos++;
            return first;
        }

        if ((first & 0xC0) == 0x80)
        {
            pos += 2;
            return ((first & 0x3F) << 8) | data[pos - 1];
        }

        if ((first & 0xE0) == 0xC0)
        {
            pos += 4;
            return ((first & 0x1F) << 24) | (data[pos - 3] << 16) | (data[pos - 2] << 8) | data[pos - 1];
        }

        pos++;
        return 0;
    }
}