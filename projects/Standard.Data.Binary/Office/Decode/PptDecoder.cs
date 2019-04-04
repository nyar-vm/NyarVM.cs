using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Office.Data;

namespace Std.Data.Binary.Office.Decode;

/// <summary>
///     PPT 文件解码器，的Microsoft PowerPoint 二进制格式（.ppt）解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     PPT 的Microsoft PowerPoint 97-2003 使用的二进制文件格式，基的OLE2 复合文档结构的
///     解码器解的PowerPoint 文档流，提取幻灯片、文本和元信息的
/// </remarks>
public ref struct PptDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="PptDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">PPT 二进制数据的/param>
    public PptDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 PPT 文件，提取演示文稿数据的
    /// </summary>
    /// <returns>PPT 演示文稿数据的/returns>
    public PowerPointData decode()
    {
        var slideTexts = new List<string>();

        while (!_buffer.is_end && _buffer.remaining >= 8)
        {
            var recordType = _buffer.read_u16_le();
            var recordVersion = (byte)(recordType & 0x000F);
            var recordInstance = (ushort)((recordType >> 4) & 0x0FFF);
            recordType = _buffer.read_u16_le();
            var recordLength = _buffer.read_u32_le();

            if (_buffer.remaining < recordLength) break;

            var recordData = _buffer.read_bytes((int)recordLength);

            if (recordType == OfficeConstants.PptRecordType.text_chars_atom)
            {
                var text = parse_text_record(recordData);

                if (!string.IsNullOrEmpty(text)) slideTexts.Add(text);
            }
        }

        return new PowerPointData
        {
            slides = slideTexts
        };
    }

    #region 私有解析方法

    private static string parse_text_record(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4) return string.Empty;

        var reader = new ByteBuffer(data);
        var textLength = reader.read_i32_le();

        if (textLength <= 0 || reader.remaining < textLength * 2) return string.Empty;

        var textBytes = reader.read_bytes(textLength * 2).ToArray();
        return Encoding.Unicode.GetString(textBytes);
    }

    #endregion
}