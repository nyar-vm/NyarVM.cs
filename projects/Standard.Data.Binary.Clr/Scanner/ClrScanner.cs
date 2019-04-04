using Std.Data.Binary.Frame;
using Std.Data.Binary.Pe.Data;

namespace Std.Data.Binary.Clr.Scanner;

/// <summary>
///     CLR 模块扫描器，基于 <see cref="SpanScanner" /> 提供的.NET 程序集的快速元信息扫描的
/// </summary>
/// <remarks>
///     .NET 程序集本质上是包的CLR 元数据目录的 PE 文件的
///     扫描器通过检的PE 头中的CLR 目录标志快速判断是否为 .NET 程序集，
///     并提取基本的 CLR 元信息（版本号、模块名称等）的
/// </remarks>
public ref struct ClrScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="ClrScanner" /> 结构的新实例的    ///。
    /// </summary>
    /// <param name="data">要扫描的 .NET 程序集字节数据的/param>
    public ClrScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器的    ///。
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 .NET 程序集头部，提取基本 CLR 信息的
    /// </summary>
    public ClrScanHeader scan_header()
    {
        var header = new ClrScanHeader();

        if (_scanner.length < 64) throw new InvalidDataException("数据过短，不是有效的 PE 文件");

        if (!_scanner.match_magic(PeConstants.dos_magic_bytes)) throw new InvalidDataException("MZ 魔数不匹配，不是有效的 PE 文件");

        var peOffset = _scanner.buffer.position;
        _scanner.buffer.position = PeConstants.pe_offset_position;

        if (_scanner.buffer.remaining < 4) throw new InvalidDataException("PE 偏移量数据不足");

        var peHeaderOffset = _scanner.buffer.read_i32_le();

        if (peHeaderOffset <= 0 || peHeaderOffset + 24 > _scanner.length) throw new InvalidDataException("PE 头偏移量无效");

        _scanner.buffer.position = peHeaderOffset;

        if (_scanner.buffer.remaining < 24) throw new InvalidDataException("PE 头数据不足");

        var peMagic = _scanner.buffer.read_u32_le();

        if (peMagic != PeConstants.pe_magic) throw new InvalidDataException("PE 签名不匹配");

        _scanner.buffer.position = peHeaderOffset + 4;
        var machine = _scanner.buffer.read_u16_le();
        var numberOfSections = _scanner.buffer.read_u16_le();
        _scanner.buffer.position += 8;
        var sizeOfOptionalHeader = _scanner.buffer.read_u16_le();
        var characteristics = _scanner.buffer.read_u16_le();

        header.is_pe32_plus = false;
        header.has_clr_directory = false;

        var optionalHeaderOffset = peHeaderOffset + 24;

        if (optionalHeaderOffset + 2 <= _scanner.length)
        {
            _scanner.buffer.position = optionalHeaderOffset;
            var optionalMagic = _scanner.buffer.read_u16_le();
            header.is_pe32_plus = optionalMagic == PeConstants.optional_magic_pe32_plus;
        }

        var clrDirectoryDataIndex = header.is_pe32_plus ? 14 : 14;

        if (header.is_pe32_plus)
        {
            var dataDirOffset = optionalHeaderOffset + 112 + clrDirectoryDataIndex * 8;

            if (dataDirOffset + 8 <= _scanner.length)
            {
                _scanner.buffer.position = dataDirOffset;
                var clrRva = _scanner.buffer.read_u32_le();
                var clrSize = _scanner.buffer.read_u32_le();
                header.has_clr_directory = clrRva != 0 && clrSize != 0;
                header.clr_metadata_rva = clrRva;
                header.clr_metadata_size = clrSize;
            }
        }
        else
        {
            var dataDirOffset = optionalHeaderOffset + 96 + clrDirectoryDataIndex * 8;

            if (dataDirOffset + 8 <= _scanner.length)
            {
                _scanner.buffer.position = dataDirOffset;
                var clrRva = _scanner.buffer.read_u32_le();
                var clrSize = _scanner.buffer.read_u32_le();
                header.has_clr_directory = clrRva != 0 && clrSize != 0;
                header.clr_metadata_rva = clrRva;
                header.clr_metadata_size = clrSize;
            }
        }

        header.machine = machine;
        header.number_of_sections = numberOfSections;
        header.is_executable = (characteristics & PeConstants.characteristics_executable) != 0;
        header.is_dll = (characteristics & PeConstants.characteristics_dll) != 0;

        return header;
    }

    /// <summary>
    ///     快速判断数据是否为 .NET 程序集的
    /// </summary>
    public bool is_clr_assembly()
    {
        if (_scanner.length < 64) return false;

        if (!_scanner.match_magic(PeConstants.dos_magic_bytes)) return false;

        try
        {
            var header = scan_header();
            return header.has_clr_directory;
        }
        catch
        {
            return false;
        }
    }
}