using System.IO.Compression;
using Std.Data.Binary.Zip.Data;

namespace Std.Data.Binary.Zip.Decode;

/// <summary>
///     ZIP 文件解码器，解析 ZIP 压缩包结构的
/// </summary>
public sealed class ZipDecoder
{
    /// <summary>
    ///     的ZIP 二进制数据解码压缩包的
    /// </summary>
    /// <param name="data">
    ///     ZIP 二进制数据的/param>
    ///     <returns>解码后的 ZIP 文件数据的/returns>
    public ZipFileData decode(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entries = new List<ZipEntryData>();

        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);

            entries.Add(new ZipEntryData
            {
                name = entry.FullName,
                size = entry.Length,
                compressed_size = entry.CompressedLength,
                compression_method = 0,
                data = ms.ToArray()
            });
        }

        return new ZipFileData
        {
            entries = entries
        };
    }

    /// <summary>
    ///     获取 ZIP 中的指定条目的
    /// </summary>
    /// <param name="data">
    ///     ZIP 二进制数据的/param>
    ///     <param name="entryName">
    ///         条目名称的/param>
    ///         <returns>条目数据，如果不存在则返的null的/returns>
    public byte[]? get_entry(byte[] data, string entryName)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entry = archive.GetEntry(entryName);
        if (entry == null) return null;

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);

        return ms.ToArray();
    }

    /// <summary>
    ///     获取 ZIP 中的所有条目名称的
    /// </summary>
    /// <param name="data">
    ///     ZIP 二进制数据的/param>
    ///     <returns>条目名称列表的/returns>
    public List<string> get_entry_names(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        return archive.Entries.Select(e => e.FullName).ToList();
    }
}