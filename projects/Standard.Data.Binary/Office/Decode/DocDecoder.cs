using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Office.Data;

namespace Std.Data.Binary.Office.Decode;

/// <summary>
///     DOC 文件解码器，的Microsoft Word 二进制格式（.doc）解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     DOC 的Microsoft Word 97-2003 使用的二进制文件格式，基的OLE2 复合文档结构的
///     解码器解的Word 二进制文件头和文档流，提取文本和格式信息的
/// </remarks>
public ref struct DocDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="DocDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">DOC 二进制数据的/param>
    public DocDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 DOC 文件，提取文本内容的
    /// </summary>
    /// <returns>Word 文档数据的/returns>
    public WordDocumentData decode()
    {
        var text = extract_text();

        return new WordDocumentData
        {
            text = text
        };
    }

    #region 私有解析方法

    private string extract_text()
    {
        var sb = new StringBuilder();

        if (_buffer.length < 12) return string.Empty;

        _buffer.position = 0;
        var wIdent = _buffer.read_u16_le();

        if (wIdent != OfficeConstants.WordDoc.file_magic) return string.Empty;

        var nFib = _buffer.read_u16_le();

        _buffer.position = (int)OfficeConstants.WordDoc.clx_offset_position;
        var clxOffset = _buffer.read_u32_le();

        if (clxOffset == 0 || clxOffset >= _buffer.length) return string.Empty;

        _buffer.position = (int)clxOffset;

        while (!_buffer.is_end && _buffer.remaining >= 1)
        {
            var type = _buffer.read_u8();

            if (type == OfficeConstants.WordStreamType.grpprl)
            {
                _buffer.advance(1);
                var cbGrpprl = _buffer.read_u16_le();
                _buffer.advance(cbGrpprl);
            }
            else if (type == OfficeConstants.WordStreamType.piece_table)
            {
                var cb = _buffer.read_u32_le();

                if (cb > _buffer.remaining) break;

                var pieceTableData = _buffer.read_bytes((int)cb).ToArray();
                var pieces = parse_piece_table(pieceTableData);

                foreach (var piece in pieces) sb.Append(piece);
            }
            else
            {
                break;
            }
        }

        return sb.ToString();
    }

    private static List<string> parse_piece_table(byte[] data)
    {
        var pieces = new List<string>();
        var reader = new ByteBuffer(data);

        while (!reader.is_end && reader.remaining >= 2)
        {
            var n = reader.read_u16_be();

            if (n == 0) break;

            var count = (n - 1) / 2;

            var cpOffsets = new uint[count + 1];

            for (var i = 0; i <= count; i++)
                if (reader.remaining >= 4)
                    cpOffsets[i] = reader.read_u32_le();

            for (var i = 0; i < count; i++)
            {
                if (reader.remaining < 8) break;

                var fcValue = reader.read_u32_le();
                var prm = reader.read_u16_le();

                var isUnicode = (fcValue & 0x40000000) == 0;
                var fc = isUnicode ? fcValue >> 1 : (fcValue & ~0x40000000) >> 1;

                var charCount = (int)(cpOffsets[i + 1] - cpOffsets[i]);

                if (charCount > 0)
                {
                    if (isUnicode)
                        pieces.Add($"[文本的{i}: {charCount} Unicode 字符, 偏移 0x{fc:X}]");
                    else
                        pieces.Add($"[文本的{i}: {charCount} ANSI 字符, 偏移 0x{fc:X}]");
                }
            }

            break;
        }

        return pieces;
    }

    #endregion
}