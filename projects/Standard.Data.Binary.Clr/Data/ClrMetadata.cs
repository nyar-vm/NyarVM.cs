namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     CLR 元数据的
/// </summary>
public sealed class ClrMetadata
{
    /// <summary>
    ///     元数据头的
    /// </summary>
    public ClrMetadataHeader header { get; init; } = new();

    /// <summary>
    ///     流头列表的
    /// </summary>
    public IReadOnlyList<ClrStreamHeader> stream_headers { get; init; } = [];

    /// <summary>
    ///     表流的~ 的#-）的
    /// </summary>
    public ClrTableStream? table_stream { get; init; }

    /// <summary>
    ///     字符串堆的Strings）的
    /// </summary>
    public ClrStringHeap string_heap { get; init; } = new();

    /// <summary>
    ///     Blob 堆（#Blob）的
    /// </summary>
    public ClrBlobHeap blob_heap { get; init; } = new();

    /// <summary>
    ///     GUID 堆（#GUID）的
    /// </summary>
    public ClrGuidHeap guid_heap { get; init; } = new();

    /// <summary>
    ///     用户字符串堆的US）的
    /// </summary>
    public ClrUserStringHeap user_string_heap { get; init; } = new();
}