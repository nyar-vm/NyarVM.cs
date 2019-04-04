using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.Office.Data;

namespace Std.Data.Binary.Office.Encode;

/// <summary>
///     XLS 编码器，的<see cref="ExcelWorkbookData" /> 编码的XLS 二进制格式的
/// </summary>
/// <remarks>
///     XLS BIFF 记录格式：RecordType(U16LE,2) + RecordLength(U16LE,2) + Data(N)的
/// </remarks>
public sealed class XlsEncoder
{
    /// <summary>
    ///     的Excel 工作簿数据编码为 XLS 二进制的
    /// </summary>
    /// <param name="data">
    ///     Excel 工作簿数据的/param>
    ///     <returns>XLS 二进制数据的/returns>
    public byte[] encode(ExcelWorkbookData data)
    {
        var records = new List<byte[]>();

        // BOF 记录
        records.Add(build_bof());

        // BoundSheet 记录
        foreach (var sheet in data.sheets) records.Add(build_bound_sheet(sheet));

        // EOF 记录
        records.Add(build_eof());

        var totalSize = records.Sum(r => r.Length);
        var buffer = new byte[totalSize];
        var pos = 0;

        foreach (var record in records)
        {
            record.CopyTo(buffer.AsSpan(pos));
            pos += record.Length;
        }

        return buffer;
    }

    /// <summary>
    ///     构建 BOF 记录的
    /// </summary>
    private static byte[] build_bof()
    {
        // RecordType(2) + RecordLength(2) + data(16) = 20 bytes
        var record = new byte[20];
        var pos = 0;

        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), OfficeConstants.XlsRecordType.bof_workbook);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), 16);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), 0x0600); // BIFF8
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), 0x0005); // Workbook type
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), 0x09CD); // Build year
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), 0x07C9); // Build identifier
        pos += 2;
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(pos), 0x0600); // Required ver
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(pos), 0x0000); // Flags

        return record;
    }

    /// <summary>
    ///     构建 BoundSheet 记录的
    /// </summary>
    private static byte[] build_bound_sheet(ExcelSheetData sheet)
    {
        var name = sheet.name;
        var isAscii = name.All(c => c <= 127);

        int nameLen;
        byte[] encodedName;

        if (isAscii)
        {
            encodedName = Encoding.ASCII.GetBytes(name);
            nameLen = System.Math.Min(encodedName.Length, 31);
        }
        else
        {
            encodedName = Encoding.Unicode.GetBytes(name);
            nameLen = System.Math.Min(name.Length, 31);
        }

        var byteLen = isAscii ? nameLen : nameLen * 2;
        var flags = isAscii ? (byte)0 : (byte)0x01;

        // RecordType(2) + RecordLength(2) + lbPlyPos(4) + hsState(1) + dt(1) + nameLen(1) + flags(1) + name(byteLen)
        var dataSize = 8 + byteLen;
        var record = new byte[4 + dataSize];
        var pos = 0;

        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), OfficeConstants.XlsRecordType.bound_sheet);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(pos), (ushort)dataSize);
        pos += 2;
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(pos), 0x00000000); // lbPlyPos
        pos += 4;
        record[pos++] = OfficeConstants.XlsRecordType.sheet_state_visible; // hsState
        record[pos++] = 0; // dt = worksheet
        record[pos++] = (byte)nameLen; // name length (char count for unicode)
        record[pos++] = flags;

        for (var i = 0; i < byteLen; i++) record[pos++] = encodedName[i];

        return record;
    }

    /// <summary>
    ///     构建 EOF 记录的
    /// </summary>
    private static byte[] build_eof()
    {
        var record = new byte[4];

        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(0), OfficeConstants.XlsRecordType.eof);
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(2), 0);

        return record;
    }
}