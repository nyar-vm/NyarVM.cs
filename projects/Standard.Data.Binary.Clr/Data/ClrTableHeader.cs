namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     表流头部的
/// </summary>
public sealed class ClrTableHeader
{
    /// <summary>
    ///     预留字节的
    /// </summary>
    public uint reserved { get; init; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public byte major_version { get; init; }

    /// <summary>
    ///     次版本号的
    /// </summary>
    public byte minor_version { get; init; }

    /// <summary>
    ///     堆偏移大小标志（<see cref="ClrHeapSizeFlags" />）的
    /// </summary>
    public byte heap_sizes { get; init; }

    /// <summary>
    ///     有效表位掩码（哪些表存在）的
    /// </summary>
    public ulong valid_tables { get; init; }

    /// <summary>
    ///     已排序表位掩码的
    /// </summary>
    public ulong sorted_tables { get; init; }

    /// <summary>
    ///     各表行计数（仅包的ValidTables 中标记为存在的表）的
    /// </summary>
    public IReadOnlyList<uint> row_counts { get; init; } = [];

    /// <summary>
    ///     #Strings 堆索引大小（2 的4 字节）的
    /// </summary>
    public int string_index_size => (heap_sizes & (byte)ClrHeapSizeFlags.string_heap_large) != 0 ? 4 : 2;

    /// <summary>
    ///     #GUID 堆索引大小（2 的4 字节）的
    /// </summary>
    public int guid_index_size => (heap_sizes & (byte)ClrHeapSizeFlags.guid_heap_large) != 0 ? 4 : 2;

    /// <summary>
    ///     #Blob 堆索引大小（2 的4 字节）的
    /// </summary>
    public int blob_index_size => (heap_sizes & (byte)ClrHeapSizeFlags.blob_heap_large) != 0 ? 4 : 2;
}