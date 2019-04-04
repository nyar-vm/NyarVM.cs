using System.Text;
using System.Text.Json;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Live2D.Data;

namespace Std.Data.Binary.Live2D.Scanner;

/// <summary>
///     Live2D Cubism 模型扫描器，基于 <see cref="SpanScanner" /> 提供对模的JSON / moc3 二进制文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     扫描器通过流式 JSON 解析或二进制头读取快速提取版本、文件引用、参数数量等元信息，
///     避免完整反序列化带来的内存分配的
///     moc3 文件头格式：4 字节魔数 "MOC3" + 1 字节版本 + 1 字节字节序标的+ 2 字节修订号的
///     段偏移表紧跟文件头，通过 <see cref="Live2DConstants" /> 计算各段偏移的
/// </remarks>
public ref struct Live2DScanner : ILive2DScanner
{
    private SpanScanner _scanner;

    public Live2DScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 model3.json 文件，提取文件引用列表的
    /// </summary>
    public List<string> scan_file_references()
    {
        var references = new List<string>();
        var content = Encoding.UTF8.GetString(_scanner.data);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (!root.TryGetProperty("FileReferences", out var fileRefs)) return references;

        if (fileRefs.TryGetProperty("Moc", out var moc)) references.Add(moc.GetString()!);

        if (fileRefs.TryGetProperty("Textures", out var textures))
            foreach (var texture in textures.EnumerateArray())
                references.Add(texture.GetString()!);

        if (fileRefs.TryGetProperty("Physics", out var physics)) references.Add(physics.GetString()!);

        if (fileRefs.TryGetProperty("Motions", out var motions))
            foreach (var motion in motions.EnumerateArray())
                if (motion.TryGetProperty("File", out var motionFile))
                    references.Add(motionFile.GetString()!);

        if (fileRefs.TryGetProperty("Expressions", out var expressions))
            foreach (var expression in expressions.EnumerateArray())
                if (expression.TryGetProperty("File", out var expressionFile))
                    references.Add(expressionFile.GetString()!);

        return references;
    }

    /// <summary>
    ///     扫描 moc3 文件头，提取版本和字节序信息的
    /// </summary>
    /// <remarks>
    ///     moc3 文件头格式：4 字节魔数 + 1 字节版本的+ 1 字节字节序标的+ 2 字节修订号的
    ///     字节序标志：0 = 小端序，的0 = 大端序的
    /// </remarks>
    public (int Version, bool IsBigEndian, int Revision) scan_moc3_header()
    {
        if (_scanner.length < Live2DConstants.header_size) throw new InvalidDataException("moc3 文件数据过短，无法读取文件头");

        if (!_scanner.match_magic(Live2DConstants.moc3_magic_number)) throw new InvalidDataException("moc3 文件魔数不匹配。");

        _scanner.consume_magic(Live2DConstants.moc3_magic_number);
        var version = _scanner.buffer.read_u8();
        var isBigEndian = _scanner.buffer.read_u8() != 0;
        var revision = _scanner.buffer.read_i16_le();

        return (version, isBigEndian, revision);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取参数数量的
    /// </summary>
    /// <remarks>
    ///     通过段偏移表的CountInfo 段的偏移量定位计数表的
    ///     然后从计数表中读取参数数量（偏移 0，i32）的
    /// </remarks>
    public int scan_moc3_parameter_count()
    {
        return scan_count_info_field(Live2DConstants.count_info_parameter_count_offset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取部件数量的
    /// </summary>
    /// <remarks>
    ///     通过段偏移表的CountInfo 段的偏移量定位计数表的
    ///     然后从计数表中读取部件数量（偏移 4，i32）的
    /// </remarks>
    public int scan_moc3_part_count()
    {
        return scan_count_info_field(Live2DConstants.count_info_part_count_offset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取绘制对象数量的
    /// </summary>
    /// <remarks>
    ///     通过段偏移表的CountInfo 段的偏移量定位计数表的
    ///     然后从计数表中读取绘制对象数量（偏移 8，i32）的
    /// </remarks>
    public int scan_moc3_drawable_count()
    {
        return scan_count_info_field(Live2DConstants.count_info_drawable_count_offset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取变形器数量（v4+）的
    /// </summary>
    public int scan_moc3_deformer_count()
    {
        return scan_count_info_field(Live2DConstants.count_info_deformer_count_offset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取纹理数量的
    /// </summary>
    public int scan_moc3_texture_count()
    {
        return scan_count_info_field(Live2DConstants.count_info_texture_count_offset);
    }

    private int scan_count_info_field(int fieldOffset)
    {
        if (_scanner.length < Live2DConstants.header_size + Live2DConstants.offset_table_entry_size) return 0;

        var (_, isBigEndian, _) = scan_moc3_header();
        var version = _scanner.buffer.read_u8_at(4);

        var countInfoSectionOffset = read_count_info_section_offset(version, isBigEndian);
        if (countInfoSectionOffset == 0) return 0;

        var fieldPosition = countInfoSectionOffset + fieldOffset;
        if (fieldPosition + 4 > _scanner.length) return 0;

        return _scanner.buffer.read_i32_at(fieldPosition, isBigEndian);
    }

    private int read_count_info_section_offset(int version, bool isBigEndian)
    {
        var countInfoEntryPosition = Live2DConstants.header_size +
                                     (int)Moc3Section.count_info * Live2DConstants.offset_table_entry_size;

        if (countInfoEntryPosition + 4 > _scanner.length) return 0;

        return _scanner.buffer.read_i32_at(countInfoEntryPosition, isBigEndian);
    }
}