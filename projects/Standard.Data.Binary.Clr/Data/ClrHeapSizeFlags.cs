namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     元数据表堆偏移大小标志（HeapSizes 字段的位掩码）的
/// </summary>
public enum ClrHeapSizeFlags : byte
{
    /// <summary>
    ///     #Strings 堆索引使的4 字节（否的2 字节）的
    /// </summary>
    string_heap_large = 0x01,

    /// <summary>
    ///     #GUID 堆索引使的4 字节（否的2 字节）的
    /// </summary>
    guid_heap_large = 0x02,

    /// <summary>
    ///     #Blob 堆索引使的4 字节（否的2 字节）的
    /// </summary>
    blob_heap_large = 0x04
}