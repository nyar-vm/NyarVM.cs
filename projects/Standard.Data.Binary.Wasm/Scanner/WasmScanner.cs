using Std.Data.Binary.Frame;
using Std.Data.Binary.Wasm.Data;

namespace Std.Data.Binary.Wasm.Scanner;

/// <summary>
///     WebAssembly 二进制格式扫描器，基于 <see cref="SpanScanner" /> 提供对 WASM 模块的快速元信息扫描
/// </summary>
public ref struct WasmScanner : IWasmScanner, IDetector
{
    /// <inheritdoc />
    public bool detect(ReadOnlySpan<byte> header)
    {
        return header.Length >= 4
               && header[0] == WasmConstants.magic_number[0]
               && header[1] == WasmConstants.magic_number[1]
               && header[2] == WasmConstants.magic_number[2]
               && header[3] == WasmConstants.magic_number[3];
    }

    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="WasmScanner" /> 结构的新实例
    /// </summary>
    /// <param name="data">要扫描的 WASM 字节数据</param>
    public WasmScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <inheritdoc />
    public uint read_version()
    {
        return _scanner.buffer.read_u32_le();
    }

    /// <inheritdoc />
    public string read_name()
    {
        var length = (int)_scanner.buffer.read_leb128_u32();
        return _scanner.buffer.read_string(length);
    }

    /// <inheritdoc />
    public uint read_leb128_u_int32()
    {
        return _scanner.buffer.read_leb128_u32();
    }

    /// <summary>
    ///     扫描 WASM 文件头，提取版本信息
    /// </summary>
    public WasmScanHeader scan_header()
    {
        if (_scanner.length < 8) throw new InvalidDataException("WASM 文件数据过短，无法读取文件头");

        if (!_scanner.match_magic(WasmConstants.magic_number)) throw new InvalidDataException("WASM 文件魔数不匹配");

        _scanner.consume_magic(WasmConstants.magic_number);
        var version = read_version();

        return new WasmScanHeader
        {
            version = version
        };
    }

    /// <summary>
    ///     扫描 WASM 模块，提取统计信息
    /// </summary>
    public WasmStatistics scan_statistics()
    {
        var header = scan_header();

        var stats = new WasmStatistics
        {
            version = header.version
        };

        while (!_scanner.is_end)
        {
            var sectionId = _scanner.buffer.read_u8();

            if (sectionId > 12) break;

            var sectionSize = (int)_scanner.buffer.read_leb128_u32();
            var sectionEnd = _scanner.position + sectionSize;

            switch ((WasmSectionId)sectionId)
            {
                case WasmSectionId.type:
                    stats.type_section_size = sectionSize;
                    break;
                case WasmSectionId.import:
                    stats.import_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.function:
                    stats.function_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.table:
                    stats.table_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.memory:
                    stats.memory_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.global:
                    stats.global_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.export:
                    stats.export_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.start:
                    stats.has_start_function = true;
                    break;
                case WasmSectionId.element:
                    stats.element_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.code:
                    stats.code_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case WasmSectionId.data:
                    stats.data_count = (int)_scanner.buffer.read_leb128_u32();
                    break;
                case (WasmSectionId)12:
                    stats.data_count_section = (int)_scanner.buffer.read_leb128_u32();
                    break;
            }

            _scanner.position = sectionEnd;
        }

        return stats;
    }

    /// <summary>
    ///     扫描 WASM 模块，提取导出名称列表
    /// </summary>
    public List<(WasmExternalKind Kind, string Name)> scan_exports()
    {
        var exports = new List<(WasmExternalKind, string)>();
        var header = scan_header();

        while (!_scanner.is_end)
        {
            var sectionId = _scanner.buffer.read_u8();

            if (sectionId > 12) break;

            var sectionSize = (int)_scanner.buffer.read_leb128_u32();
            var sectionEnd = _scanner.position + sectionSize;

            if ((WasmSectionId)sectionId == WasmSectionId.export)
            {
                var count = (int)_scanner.buffer.read_leb128_u32();

                for (var i = 0; i < count; i++)
                {
                    var name = read_name();
                    var kind = (WasmExternalKind)_scanner.buffer.read_u8();
                    _scanner.buffer.read_leb128_u32();
                    exports.Add((kind, name));
                }

                break;
            }

            _scanner.position = sectionEnd;
        }

        return exports;
    }
}

/// <summary>
///     WASM 扫描头部信息
/// </summary>
public sealed class WasmScanHeader
{
    /// <summary>
    ///     WASM 版本号
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     版本名称
    /// </summary>
    public string version_name => version switch
    {
        1 => "MVP",
        2 => "Feature Test",
        _ => $"0x{version:X8}"
    };
}

/// <summary>
///     WASM 模块统计信息
/// </summary>
public sealed class WasmStatistics
{
    /// <summary>
    ///     WASM 版本号
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     类型段大小
    /// </summary>
    public int type_section_size { get; set; }

    /// <summary>
    ///     导入数量
    /// </summary>
    public int import_count { get; set; }

    /// <summary>
    ///     函数数量
    /// </summary>
    public int function_count { get; set; }

    /// <summary>
    ///     表数量
    /// </summary>
    public int table_count { get; set; }

    /// <summary>
    ///     内存数量
    /// </summary>
    public int memory_count { get; set; }

    /// <summary>
    ///     全局变量数量
    /// </summary>
    public int global_count { get; set; }

    /// <summary>
    ///     导出数量
    /// </summary>
    public int export_count { get; set; }

    /// <summary>
    ///     是否有起始函数
    /// </summary>
    public bool has_start_function { get; set; }

    /// <summary>
    ///     元素段数量
    /// </summary>
    public int element_count { get; set; }

    /// <summary>
    ///     代码段数量
    /// </summary>
    public int code_count { get; set; }

    /// <summary>
    ///     数据段数量
    /// </summary>
    public int data_count { get; set; }

    /// <summary>
    ///     DataCount 段值
    /// </summary>
    public int data_count_section { get; set; }
}