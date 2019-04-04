namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     GUID 堆（#GUID）的
/// </summary>
public sealed class ClrGuidHeap
{
    /// <summary>
    ///     原始数据的
    /// </summary>
    public byte[] data { get; init; } = [];

    /// <summary>
    ///     按索引读的GUID（索引从 1 开始，每个 GUID 16 字节）的
    /// </summary>
    public Guid read_guid(uint index)
    {
        if (index == 0 || data.Length < 16) return Guid.Empty;

        var offset = (int)(index - 1) * 16;

        if (offset + 16 > data.Length) return Guid.Empty;

        return new Guid(data.AsSpan(offset, 16));
    }
}