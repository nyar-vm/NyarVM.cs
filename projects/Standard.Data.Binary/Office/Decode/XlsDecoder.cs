using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Office.Data;

namespace Std.Data.Binary.Office.Decode;

/// <summary>
///     XLS 文件解码器，的Microsoft Excel 二进制格式（.xls）解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     XLS 的Microsoft Excel 97-2003 使用的二进制文件格式，基的OLE2 复合文档结构（BIFF8 格式）的
///     解码器解的BIFF 记录流，提取工作簿、工作表和单元格数据的
/// </remarks>
public ref struct XlsDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="XlsDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">XLS 二进制数据的/param>
    public XlsDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 XLS 文件，提取工作簿数据的
    /// </summary>
    /// <returns>Excel 工作簿数据的/returns>
    public ExcelWorkbookData decode()
    {
        var sheets = new List<ExcelSheetData>();
        var strings = new List<string>();

        while (!_buffer.is_end && _buffer.remaining >= 4)
        {
            var recordType = _buffer.read_u16_le();
            var recordLength = _buffer.read_u16_le();

            if (_buffer.remaining < recordLength) break;

            var recordData = _buffer.read_bytes(recordLength);

            switch (recordType)
            {
                case OfficeConstants.XlsRecordType.bound_sheet:
                    parse_bound_sheet(recordData, sheets);
                    break;
                case OfficeConstants.XlsRecordType.sst:
                    parse_sst(recordData, strings);
                    break;
            }
        }

        return new ExcelWorkbookData
        {
            sheets = sheets
        };
    }

    #region 私有解析方法

    private static void parse_bound_sheet(ReadOnlySpan<byte> data, List<ExcelSheetData> sheets)
    {
        if (data.Length < 8) return;

        var reader = new ByteBuffer(data);
        var sheetOffset = reader.read_u32_le();
        var visibility = reader.read_u8();
        var sheetType = reader.read_u8();
        var nameLength = reader.read_u8();
        var flags = reader.read_u8();

        var isUnicode = (flags & 0x01) != 0;

        string sheetName;

        var byteLength = isUnicode ? nameLength * 2 : nameLength;

        if (reader.remaining >= byteLength && byteLength > 0)
        {
            var nameBytes = reader.read_bytes(byteLength).ToArray();
            sheetName = isUnicode
                ? Encoding.Unicode.GetString(nameBytes)
                : Encoding.ASCII.GetString(nameBytes);
        }
        else
        {
            sheetName = $"Sheet{sheets.Count + 1}";
        }

        sheets.Add(new ExcelSheetData
        {
            name = sheetName,
            row_count = 0,
            column_count = 0,
            rows = []
        });
    }

    private static void parse_sst(ReadOnlySpan<byte> data, List<string> strings)
    {
        if (data.Length < 8) return;

        var reader = new ByteBuffer(data);
        var totalStrings = reader.read_u32_le();
        var uniqueStrings = reader.read_u32_le();

        for (var i = 0; i < uniqueStrings && !reader.is_end; i++)
        {
            if (reader.remaining < 3) break;

            var strLength = reader.read_u16_le();
            var flags = reader.read_u8();

            var isUnicode = (flags & 0x01) != 0;
            var hasAsian = (flags & 0x04) != 0;
            var hasRich = (flags & 0x08) != 0;

            if (hasRich && reader.remaining >= 2) reader.advance(2);

            if (hasAsian && reader.remaining >= 4) reader.advance(4);

            var byteLength = isUnicode ? strLength * 2 : strLength;

            if (reader.remaining < byteLength) break;

            var strBytes = reader.read_bytes(byteLength).ToArray();
            var str = isUnicode ? Encoding.Unicode.GetString(strBytes) : Encoding.ASCII.GetString(strBytes);

            strings.Add(str);
        }
    }

    #endregion
}