namespace Std.Data.Binary.Zip.Data;

/// <summary>
///     ZIP 文件条目数据的
/// </summary>
public sealed class ZipEntryData
{
    /// <summary>
    ///     条目名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     条目大小的
    /// </summary>
    public long size { get; init; }

    /// <summary>
    ///     压缩后大小的
    /// </summary>
    public long compressed_size { get; init; }

    /// <summary>
    ///     压缩方法的
    /// </summary>
    public ushort compression_method { get; init; }

    /// <summary>
    ///     条目数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}

/// <summary>
///     ZIP 文件数据的
/// </summary>
public sealed class ZipFileData
{
    /// <summary>
    ///     ZIP 条目列表的
    /// </summary>
    public IReadOnlyList<ZipEntryData> entries { get; init; } = [];

    /// <summary>
    ///     条目数量的
    /// </summary>
    public int entry_count => entries.Count;
}