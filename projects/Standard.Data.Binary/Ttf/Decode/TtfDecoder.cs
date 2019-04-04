using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Ttf.Data;

namespace Std.Data.Binary.Ttf.Decode;

/// <summary>
///     TrueType/OpenType 字体文件解码器，将字体格式解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     TrueType 的Apple 的Microsoft 的字体格式，OpenType 是其扩展版本的
///     解码器解析字体偏移表和表记录，提取基本元信息，不执行完整字形渲染的
/// </remarks>
public ref struct TtfDecoder
{
    private ByteBuffer _buffer;
    private TtfFontType _font_type;

    /// <summary>
    ///     初始的<see cref="TtfDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">TTF 二进制数据的/param>
    public TtfDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
        _font_type = TtfFontType.unknown;
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 TrueType/OpenType 字体文件的
    /// </summary>
    /// <returns>字体数据的/returns>
    public TtfFontData decode()
    {
        var (fontType, tableCount, tables) = read_offset_table();

        if (tableCount == 0) return new TtfFontData { font_type = fontType, table_count = 0, tables = [] };

        var headTable = find_table(tables, TtfTableNames.head);
        var nameTable = find_table(tables, TtfTableNames.name);

        var headInfo = headTable != null ? read_head_table(headTable) : null;
        var nameInfo = nameTable != null ? read_name_table(nameTable) : null;

        return new TtfFontData
        {
            font_type = fontType,
            table_count = tableCount,
            tables = tables,
            head_info = headInfo,
            name_info = nameInfo
        };
    }

    /// <summary>
    ///     仅解码字体头部信息的
    /// </summary>
    public (TtfFontType FontType, ushort TableCount) decode_header()
    {
        var (fontType, tableCount, _) = read_offset_table();
        return (fontType, tableCount);
    }

    #region 私有解析方法

    private (TtfFontType, ushort, List<TtfTableRecord>) read_offset_table()
    {
        if (_buffer.remaining < TtfConstants.offset_table_size) throw new InvalidDataException("TTF 文件数据过短，无法读取偏移表");

        var sfVersion = _buffer.read_u32_be();
        TtfFontType fontType;

        if (sfVersion == TtfConstants.true_type_magic)
            fontType = TtfFontType.true_type;
        else if (_buffer.position >= 4 && sfVersion == 0x4F54544F)
            fontType = TtfFontType.cff;
        else if (sfVersion == 0x74746366)
            fontType = TtfFontType.collection;
        else
            throw new InvalidDataException($"TTF 版本标识无效的x{sfVersion:X8}");

        _font_type = fontType;

        if (fontType == TtfFontType.collection) return (fontType, 0, []);

        var tableCount = _buffer.read_u16_be();

        _buffer.advance(6);

        var tables = new List<TtfTableRecord>(tableCount);

        for (var i = 0; i < tableCount; i++) tables.Add(read_table_record());

        read_table_data(tables);

        return (fontType, tableCount, tables);
    }

    private TtfTableRecord read_table_record()
    {
        var tagBytes = _buffer.read_bytes(4).ToArray();
        var tag = Encoding.ASCII.GetString(tagBytes);
        var checksum = _buffer.read_u32_be();
        var offset = _buffer.read_u32_be();
        var length = _buffer.read_u32_be();

        return new TtfTableRecord
        {
            tag = tag,
            checksum = checksum,
            offset = offset,
            length = length
        };
    }

    private static TtfTableRecord? find_table(IReadOnlyList<TtfTableRecord> tables, string tag)
    {
        foreach (var table in tables)
            if (table.tag == tag)
                return table;

        return null;
    }

    private void read_table_data(List<TtfTableRecord> tables)
    {
        for (var i = 0; i < tables.Count; i++)
        {
            var record = tables[i];

            if (record.offset == 0 || record.length == 0 || record.offset + record.length > _buffer.length) continue;

            var savedPos = _buffer.position;
            _buffer.position = (int)record.offset;
            var data = _buffer.read_bytes((int)record.length).ToArray();
            _buffer.position = savedPos;

            tables[i] = new TtfTableRecord
            {
                tag = record.tag,
                checksum = record.checksum,
                offset = record.offset,
                length = record.length,
                data = data
            };
        }
    }

    private TtfHeadInfo? read_head_table(TtfTableRecord record)
    {
        if ((int)(record.offset + 54) > _buffer.length) return null;

        var savedPos = _buffer.position;
        _buffer.position = (int)record.offset;

        var version = _buffer.read_u32_be();
        var fontRevision = _buffer.read_u32_be();
        _buffer.advance(8);
        var unitsPerEm = _buffer.read_u16_be();
        _buffer.advance(8);
        var created = _buffer.read_i64_be();
        var modified = _buffer.read_i64_be();
        _buffer.advance(12);
        var xMin = _buffer.read_i16_be();
        var yMin = _buffer.read_i16_be();
        var xMax = _buffer.read_i16_be();
        var yMax = _buffer.read_i16_be();

        _buffer.position = savedPos;

        return new TtfHeadInfo
        {
            version = version,
            units_per_em = unitsPerEm,
            created = created,
            modified = modified,
            x_min = xMin,
            y_min = yMin,
            x_max = xMax,
            y_max = yMax
        };
    }

    private TtfNameInfo? read_name_table(TtfTableRecord record)
    {
        if ((int)(record.offset + 6) > _buffer.length) return null;

        var savedPos = _buffer.position;
        _buffer.position = (int)record.offset;

        var format = _buffer.read_u16_be();
        var count = _buffer.read_u16_be();
        var stringOffset = _buffer.read_u16_be();

        var familyName = string.Empty;
        var subFamilyName = string.Empty;
        var fullName = string.Empty;
        var versionStr = string.Empty;

        for (var i = 0; i < count && !_buffer.is_end; i++)
        {
            var platformId = _buffer.read_u16_be();
            var encodingId = _buffer.read_u16_be();
            var languageId = _buffer.read_u16_be();
            var nameId = _buffer.read_u16_be();
            var length = _buffer.read_u16_be();
            var offset = _buffer.read_u16_be();

            if (platformId != 3 || encodingId != 1) continue;

            var strStart = (int)(record.offset + stringOffset + offset);

            if (strStart + length <= _buffer.length)
            {
                var saved = _buffer.position;
                _buffer.position = strStart;
                var str = _buffer.read_string(length / 2).TrimEnd('\0');
                _buffer.position = saved;

                switch (nameId)
                {
                    case 1:
                        familyName = string.IsNullOrEmpty(familyName) ? str : familyName;
                        break;
                    case 2:
                        subFamilyName = string.IsNullOrEmpty(subFamilyName) ? str : subFamilyName;
                        break;
                    case 4:
                        fullName = string.IsNullOrEmpty(fullName) ? str : fullName;
                        break;
                    case 5:
                        versionStr = string.IsNullOrEmpty(versionStr) ? str : versionStr;
                        break;
                }
            }
        }

        _buffer.position = savedPos;

        return new TtfNameInfo
        {
            family_name = familyName,
            sub_family_name = subFamilyName,
            full_name = fullName,
            version = versionStr
        };
    }

    #endregion
}