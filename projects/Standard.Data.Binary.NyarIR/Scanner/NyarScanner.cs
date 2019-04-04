using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Scanner;

/// <summary>
///     Nyar 字节码模块扫描器，基的<see cref="SpanScanner" /> 提供的.nyar 字节码模块的快速元信息扫描的
/// </summary>
/// <remarks>
///     .nyar 的NyarVM 的字节码模块格式，采用分段式二进制布局的
///     扫描器只读取头部元信息和段表概要，不做完整的数据解码，以实现快速探查的
///     二进制布局：[Header 16B] 的[Section Headers N*9B] 的[Name Section] 的[Section Data...]
/// </remarks>
public ref struct NyarScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="NyarScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 .nyar 字节数据的/param>
    public NyarScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 .nyar 模块头部，提取基本模块信息的
    /// </summary>
    /// <returns>Nyar 模块头部信息的/returns>
    public NyarScanHeader scan_header()
    {
        if (_scanner.length < NyarConstants.header_size)
            throw new InvalidDataException($".nyar 文件数据过短，期望至的{NyarConstants.header_size} 字节");

        if (!_scanner.match_magic(NyarConstants.magic_number))
            throw new InvalidDataException($".nyar 文件魔数不匹配，期望 NYAR(0x{NyarConstants.magic_value:X8})");

        _scanner.consume_magic(NyarConstants.magic_number);

        var version = _scanner.buffer.read_u32_le();
        var sectionCount = _scanner.buffer.read_i32_le();
        var nameOffset = _scanner.buffer.read_i32_le();

        var header = new NyarScanHeader
        {
            version = version,
            section_count = sectionCount,
            sections = scan_section_headers(sectionCount),
            module_name = scan_module_name(nameOffset)
        };

        foreach (var section in header.sections)
            switch (section.kind)
            {
                case NyarSectionKind.constants:
                    header.has_constants_section = true;
                    break;
                case NyarSectionKind.functions:
                    header.has_functions_section = true;
                    break;
                case NyarSectionKind.imports:
                    header.has_imports_section = true;
                    break;
                case NyarSectionKind.exports:
                    header.has_exports_section = true;
                    break;
                case NyarSectionKind.code:
                    header.has_code_section = true;
                    break;
            }

        return header;
    }

    /// <summary>
    ///     快速判断数据是否为 .nyar 格式的
    /// </summary>
    public bool is_nyar()
    {
        if (_scanner.length < 4) return false;

        return _scanner.match_magic(NyarConstants.magic_number);
    }

    #region 私有扫描方法

    private List<NyarSectionScanInfo> scan_section_headers(int count)
    {
        var sections = new List<NyarSectionScanInfo>(count);

        for (var i = 0; i < count && !_scanner.buffer.is_end; i++)
        {
            if (_scanner.buffer.remaining < NyarConstants.section_header_size) break;

            var kind = (NyarSectionKind)_scanner.buffer.read_u8();
            var offset = _scanner.buffer.read_i32_le();
            var size = _scanner.buffer.read_i32_le();

            var item = new NyarSectionScanInfo
            {
                kind = kind,
                offset = offset,
                size = size
            };
            sections.Add(item);
        }

        return sections;
    }

    private string scan_module_name(int nameOffset)
    {
        if (nameOffset <= 0 || nameOffset >= _scanner.length) return string.Empty;

        var savedPosition = _scanner.buffer.position;
        _scanner.buffer.position = nameOffset;

        if (_scanner.buffer.remaining < 4)
        {
            _scanner.buffer.position = savedPosition;
            return string.Empty;
        }

        var nameLength = _scanner.buffer.read_i32_le();

        if (nameLength <= 0 || _scanner.buffer.remaining < nameLength)
        {
            _scanner.buffer.position = savedPosition;
            return string.Empty;
        }

        var name = _scanner.buffer.read_string(nameLength);
        _scanner.buffer.position = savedPosition;
        return name;
    }

    #endregion
}