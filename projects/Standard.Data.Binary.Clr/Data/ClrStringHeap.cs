using System.Text;

namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     字符串堆的Strings）的
/// </summary>
public sealed class ClrStringHeap
{
    /// <summary>
    ///     原始数据的
    /// </summary>
    public byte[] data { get; init; } = [];

    /// <summary>
    ///     按偏移量读取的null 结尾的UTF-8 字符串的
    /// </summary>
    public string read_string(uint offset)
    {
        if (offset == 0 || offset >= data.Length) return string.Empty;

        var start = (int)offset;
        var end = start;

        while (end < data.Length && data[end] != 0) end++;

        return Encoding.UTF8.GetString(data, start, end - start);
    }
}