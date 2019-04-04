using System.IO.Compression;
using Std.Data.Binary.Zip.Data;

namespace Std.Data.Binary.Zip.Encode;

/// <summary>
///     ZIP 文件编码器，的<see cref="ZipFileData" /> 编码的ZIP 二进制格式的
/// </summary>
public sealed class ZipEncoder
{
    private readonly CompressionLevel _compression_level;

    /// <summary>
    ///     初始的<see cref="ZipEncoder" /> 类的新实例，使用最优压缩级别的
    /// </summary>
    public ZipEncoder()
        : this(CompressionLevel.Optimal)
    {
    }

    /// <summary>
    ///     初始的<see cref="ZipEncoder" /> 类的新实例的
    /// </summary>
    /// <param name="compressionLevel">压缩级别的/param>
    public ZipEncoder(CompressionLevel compressionLevel)
    {
        _compression_level = compressionLevel;
    }

    /// <summary>
    ///     的ZIP 文件数据编码为二进制字节数组的
    /// </summary>
    /// <param name="data">
    ///     ZIP 文件数据的/param>
    ///     <returns>ZIP 二进制数据的/returns>
    public byte[] encode(ZipFileData data)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var entry in data.entries)
            {
                var zipEntry = archive.CreateEntry(entry.name, _compression_level);

                using var entryStream = zipEntry.Open();
                entryStream.Write(entry.data, 0, entry.data.Length);
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    ///     将单个条目数据编码为 ZIP 二进制字节数组的
    /// </summary>
    /// <param name="entryName">
    ///     条目名称的/param>
    ///     <param name="entryData">
    ///         条目数据的/param>
    ///         <returns>ZIP 二进制数据的/returns>
    public byte[] encode_single(string entryName, byte[] entryData)
    {
        var data = new ZipFileData
        {
            entries = new List<ZipEntryData>
            {
                new()
                {
                    name = entryName,
                    data = entryData,
                    size = entryData.Length,
                    compressed_size = 0,
                    compression_method = 8
                }
            }
        };

        return encode(data);
    }
}