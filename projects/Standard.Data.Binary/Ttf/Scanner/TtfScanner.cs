using Std.Data.Binary.Frame;
using Std.Data.Binary.Ttf.Data;

namespace Std.Data.Binary.Ttf.Scanner;

/// <summary>
///     TTF 文件扫描器，基于 <see cref="SpanScanner" /> 提供的TrueType/OpenType 字体文件的快速元信息扫描的
/// </summary>
public ref struct TtfScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="TtfScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 TTF 字节数据的/param>
    public TtfScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 TTF 文件头，提取基本字体信息的
    /// </summary>
    public TtfScanHeader scan_header()
    {
        if (_scanner.length < TtfConstants.offset_table_size) throw new InvalidDataException("TTF 文件数据过短，无法读取偏移表");

        var sfVersion = _scanner.buffer.read_u32_be();
        var fontType = sfVersion switch
        {
            TtfConstants.true_type_magic => TtfFontType.true_type,
            0x4F54544F => TtfFontType.cff,
            0x74746366 => TtfFontType.collection,
            _ => TtfFontType.unknown
        };

        var tableCount = _scanner.buffer.read_u16_be();

        return new TtfScanHeader
        {
            font_type = fontType,
            table_count = tableCount
        };
    }

    /// <summary>
    ///     快速判断数据是否为 TTF/OTF 格式的
    /// </summary>
    public bool is_font()
    {
        if (_scanner.length < 4) return false;

        var sfVersion = _scanner.buffer.read_u32_be();
        _scanner.position = 0;

        return sfVersion is TtfConstants.true_type_magic or 0x4F54544F or 0x74746366;
    }
}

/// <summary>
///     TTF 扫描头部信息的
/// </summary>
public sealed class TtfScanHeader
{
    /// <summary>
    ///     字体类型的
    /// </summary>
    public TtfFontType font_type { get; init; }

    /// <summary>
    ///     表数量的
    /// </summary>
    public ushort table_count { get; init; }

    /// <summary>
    ///     字体类型名称的
    /// </summary>
    public string font_type_name => font_type switch
    {
        TtfFontType.true_type => "TrueType",
        TtfFontType.cff => "OpenType/CFF",
        TtfFontType.collection => "TTC",
        _ => "未知"
    };
}