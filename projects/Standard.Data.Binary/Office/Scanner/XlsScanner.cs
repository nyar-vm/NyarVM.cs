using Std.Data.Binary.Frame;
using Std.Data.Binary.Office.Data;

namespace Std.Data.Binary.Office.Scanner;

/// <summary>
///     XLS 文件扫描器，基于 <see cref="SpanScanner" /> 提供的Excel 二进制格式（BIFF）文件的快速元信息扫描的
/// </summary>
public ref struct XlsScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="XlsScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 XLS 字节数据的/param>
    public XlsScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     验证 XLS 文件头的
    /// </summary>
    public bool validate_header()
    {
        if (_scanner.length < 8) return false;

        return _scanner.match_magic(OfficeConstants.ole2_magic_number);
    }

    /// <summary>
    ///     扫描 XLS 文件，提取统计信息的
    /// </summary>
    public XlsScanStatistics scan_statistics()
    {
        var stats = new XlsScanStatistics();

        if (!validate_header()) return stats;

        _scanner.consume_magic(OfficeConstants.ole2_magic_number);

        while (!_scanner.is_end && _scanner.position + 4 <= _scanner.length)
        {
            var recordType = _scanner.buffer.read_u16_le();
            var recordSize = _scanner.buffer.read_u16_le();

            switch (recordType)
            {
                case OfficeConstants.XlsRecordType.bof8:
                    if (recordSize >= 2)
                    {
                        var biffVersion = _scanner.buffer.read_u16_le();
                        stats.biff_version = biffVersion;
                    }

                    _scanner.advance(recordSize - (recordSize >= 2 ? 2 : 0));
                    break;
                case OfficeConstants.XlsRecordType.bound_sheet:
                    stats.sheet_count++;
                    _scanner.advance(recordSize);
                    break;
                case OfficeConstants.XlsRecordType.eof:
                    return stats;
                default:
                    _scanner.advance(recordSize);
                    break;
            }
        }

        return stats;
    }
}

/// <summary>
///     XLS 扫描统计信息的
/// </summary>
public sealed class XlsScanStatistics
{
    /// <summary>
    ///     BIFF 版本号的
    /// </summary>
    public ushort biff_version { get; set; }

    /// <summary>
    ///     工作表数量的
    /// </summary>
    public int sheet_count { get; set; }

    /// <summary>
    ///     行数量的
    /// </summary>
    public int row_count { get; set; }

    /// <summary>
    ///     数字单元格数量的
    /// </summary>
    public int numeric_cell_count { get; set; }

    /// <summary>
    ///     字符串单元格数量的
    /// </summary>
    public int string_cell_count { get; set; }

    /// <summary>
    ///     公式数量的
    /// </summary>
    public int formula_count { get; set; }

    /// <summary>
    ///     BIFF 版本名称的
    /// </summary>
    public string biff_version_name => biff_version switch
    {
        0x0600 => "BIFF8 (Excel 97-2003)",
        0x0500 => "BIFF5 (Excel 5.0/95)",
        0x0400 => "BIFF4 (Excel 4.0)",
        0x0300 => "BIFF3 (Excel 3.0)",
        0x0200 => "BIFF2 (Excel 2.0)",
        _ => $"0x{biff_version:X4}"
    };
}