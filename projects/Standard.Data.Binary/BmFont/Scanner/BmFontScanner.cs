using Std.Data.Binary.BmFont.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.BmFont.Scanner;

/// <summary>
///     BMFont 文件扫描器，基于 <see cref="SpanScanner" /> 提供的BMFont 位图字体文件的快速元信息扫描的
/// </summary>
public ref struct BmFontScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="BmFontScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 BMFont 字节数据的/param>
    public BmFontScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 BMFont 文件头，提取基本字体信息的
    /// </summary>
    public BmFontScanHeader scan_header()
    {
        if (_scanner.length < 4) throw new InvalidDataException("BMFont 文件数据过短，无法读取头部。");

        if (!_scanner.match_magic(BmFontConstants.binary_magic)) throw new InvalidDataException("BMFont 文件魔数不匹配。");

        _scanner.consume_magic(BmFontConstants.binary_magic);

        var version = _scanner.buffer.read_u8();

        short fontSize = 0;
        var bold = false;
        var italic = false;
        var unicode = false;

        while (!_scanner.is_end)
        {
            if (_scanner.remaining_bytes < 5) break;

            var blockType = _scanner.buffer.read_u8();
            var blockSize = (int)_scanner.buffer.read_u32_le();

            if (blockType == BmFontConstants.block_info && _scanner.remaining_bytes >= 15)
            {
                fontSize = _scanner.buffer.read_i16_le();
                var flags = _scanner.buffer.read_u8();
                bold = (flags & 0x01) != 0;
                italic = (flags & 0x02) != 0;
                unicode = (flags & 0x04) != 0;
                break;
            }

            _scanner.advance(blockSize);
        }

        return new BmFontScanHeader
        {
            version = version,
            is_binary = true,
            font_size = fontSize,
            bold = bold,
            italic = italic,
            unicode = unicode
        };
    }

    /// <summary>
    ///     快速判断数据是否为 BMFont 二进制格式的
    /// </summary>
    public bool is_bm_font_binary()
    {
        if (_scanner.length < 3) return false;

        return _scanner.match_magic(BmFontConstants.binary_magic);
    }
}

/// <summary>
///     BMFont 扫描头部信息的
/// </summary>
public sealed class BmFontScanHeader
{
    /// <summary>
    ///     版本号的
    /// </summary>
    public byte version { get; init; }

    /// <summary>
    ///     是否为二进制格式的
    /// </summary>
    public bool is_binary { get; init; }

    /// <summary>
    ///     字体大小的
    /// </summary>
    public short font_size { get; init; }

    /// <summary>
    ///     是否粗体的
    /// </summary>
    public bool bold { get; init; }

    /// <summary>
    ///     是否斜体的
    /// </summary>
    public bool italic { get; init; }

    /// <summary>
    ///     是否 Unicode的
    /// </summary>
    public bool unicode { get; init; }
}