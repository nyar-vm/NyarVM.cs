using System.Text;

namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     用户字符串堆的US）的
/// </summary>
public sealed class ClrUserStringHeap
{
    /// <summary>
    ///     原始数据的
    /// </summary>
    public byte[] data { get; init; } = [];

    /// <summary>
    ///     按偏移量读取用户字符串（先读取压缩长度前缀，再读取 UTF-16LE 数据 + 尾部标志字节）的
    /// </summary>
    public string read_user_string(uint offset)
    {
        if (offset == 0 || offset >= data.Length) return string.Empty;

        var pos = (int)offset;
        var length = ClrBlobHeap.decode_blob_length(data, ref pos);

        if (length == 0 || pos + length > data.Length) return string.Empty;

        var byteCount = length - 1;

        if (byteCount <= 0) return string.Empty;

        return Encoding.Unicode.GetString(data, pos, byteCount);
    }
}