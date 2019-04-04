using Std.Data.Binary.Frame;
using Std.Data.Binary.Zip.Data;

namespace Std.Data.Binary.Zip.Scanner;

/// <summary>
///     ZIP 文件扫描器，基于 <see cref="SpanScanner" /> 提供的ZIP 归档文件的快速元信息扫描的
/// </summary>
public ref struct ZipScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="ZipScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 ZIP 字节数据的/param>
    public ZipScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     验证 ZIP 文件头的
    /// </summary>
    public bool validate_header()
    {
        if (_scanner.length < 4) return false;

        return _scanner.buffer.read_u32_at(0) == ZipConstants.local_file_header_magic;
    }

    /// <summary>
    ///     扫描 ZIP 文件，提取统计信息的
    /// </summary>
    public ZipScanStatistics scan_statistics()
    {
        var stats = new ZipScanStatistics();
        var entryCount = 0;
        var compressedSize = 0L;
        var uncompressedSize = 0L;
        var entryNames = new List<string>();

        var eocdOffset = find_end_of_central_directory();

        if (eocdOffset < 0) return stats;

        _scanner.position = eocdOffset;

        _scanner.advance(4);
        _scanner.advance(4);
        _scanner.advance(2);

        var centralDirEntryCount = _scanner.buffer.read_u16_le();
        var centralDirSize = _scanner.buffer.read_u32_le();
        var centralDirOffset = _scanner.buffer.read_u32_le();

        _scanner.position = (int)centralDirOffset;

        for (var i = 0; i < centralDirEntryCount; i++)
        {
            if (_scanner.position + 46 > _scanner.length) break;

            var sig = _scanner.buffer.read_u32_le();

            if (sig != ZipConstants.central_directory_header_magic) break;

            _scanner.advance(6);

            var compressionMethod = _scanner.buffer.read_u16_le();
            _scanner.advance(8);

            var cSize = _scanner.buffer.read_u32_le();
            var uSize = _scanner.buffer.read_u32_le();
            var nameLength = _scanner.buffer.read_u16_le();
            var extraLength = _scanner.buffer.read_u16_le();
            var commentLength = _scanner.buffer.read_u16_le();

            _scanner.advance(8);

            var localHeaderOffset = _scanner.buffer.read_u32_le();

            var name = _scanner.buffer.read_string(nameLength);
            entryNames.Add(name);

            compressedSize += cSize;
            uncompressedSize += uSize;
            entryCount++;

            _scanner.advance(extraLength + commentLength);
        }

        stats.entry_count = entryCount;
        stats.compressed_size = compressedSize;
        stats.uncompressed_size = uncompressedSize;
        stats.entry_names = entryNames;

        return stats;
    }

    private int find_end_of_central_directory()
    {
        var searchStart = System.Math.Max(0, _scanner.length - ZipConstants.max_eocd_search_size);

        for (var pos = _scanner.length - 22; pos >= searchStart; pos--)
            if (_scanner.buffer.read_u32_at(pos) == ZipConstants.end_of_central_directory_magic)
                return pos;

        return -1;
    }
}

/// <summary>
///     ZIP 扫描统计信息的
/// </summary>
public sealed class ZipScanStatistics
{
    /// <summary>
    ///     条目数量的
    /// </summary>
    public int entry_count { get; set; }

    /// <summary>
    ///     压缩总大小的
    /// </summary>
    public long compressed_size { get; set; }

    /// <summary>
    ///     未压缩总大小的
    /// </summary>
    public long uncompressed_size { get; set; }

    /// <summary>
    ///     条目名称列表的
    /// </summary>
    public IReadOnlyList<string> entry_names { get; set; } = [];
}