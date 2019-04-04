using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.SpirV.Data;

namespace Std.Data.Binary.SpirV.Scanner;

/// <summary>
///     SPIR-V 格式扫描器的默认实现，基的<see cref="SpanScanner" /> 提供零分配的快速数据扫描的
/// </summary>
/// <remarks>
///     SPIR-V 的Khronos 定义的着色器二进制中间语言，用的Vulkan、OpenCL 等图形和计算 API的
///     扫描器只读取文件头和关键指令，不做完整的指令解码，以实现快速探查的
/// </remarks>
public ref struct SpirvScanner : ISpirvScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="SpirvScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 SPIR-V 二进制数据的/param>
    public SpirvScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <inheritdoc />
    public uint read_spirv_word()
    {
        return _scanner.buffer.read_u32_le();
    }

    /// <inheritdoc />
    public (ushort Opcode, ushort WordCount) read_instruction_header()
    {
        var word = read_spirv_word();
        var opcode = (ushort)(word & 0xFFFF);
        var wordCount = (ushort)(word >> 16);
        return (opcode, wordCount);
    }

    /// <inheritdoc />
    public string read_spirv_string()
    {
        var bytes = new List<byte>();
        var startWordPosition = _scanner.position;

        while (_scanner.position + 4 <= _scanner.length)
        {
            var word = read_spirv_word();

            for (var i = 0; i < 4; i++)
            {
                var b = (byte)((word >> (i * 8)) & 0xFF);
                bytes.Add(b);

                if (b == 0)
                {
                    var consumedWords = (_scanner.position - startWordPosition) / 4;
                    var alignedWords = (bytes.Count + 3) / 4;

                    if (alignedWords > consumedWords)
                    {
                        var extraWords = alignedWords - consumedWords;
                        _scanner.advance(extraWords * 4);
                    }

                    var charCount = bytes.Count - 1;

                    for (var j = 0; j < bytes.Count; j++)
                        if (bytes[j] == 0)
                        {
                            charCount = j;
                            break;
                        }

                    return Encoding.UTF8.GetString(bytes.ToArray(), 0, charCount);
                }
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    ///     扫描 SPIR-V 文件头，提取基本模块信息的
    /// </summary>
    /// <returns>SPIR-V 文件头信息的/returns>
    public SpirvHeader scan_header()
    {
        if (_scanner.length < 20) throw new InvalidDataException("SPIR-V 文件数据过短，无法读取文件头");

        var magicNumber = _scanner.buffer.read_u32_at(0);

        if (magicNumber != SpirvConstants.magic_number)
            throw new InvalidDataException($"SPIR-V 文件魔数不匹配，期望 0x07230203，实的0x{magicNumber:X8}");

        var version = _scanner.buffer.read_u32_at(4);
        var generatorMagic = _scanner.buffer.read_u32_at(8);
        var bound = _scanner.buffer.read_u32_at(12);
        var schema = _scanner.buffer.read_u32_at(16);

        return new SpirvHeader
        {
            version = version,
            generator_magic = generatorMagic,
            bound = bound,
            schema = schema
        };
    }

    /// <summary>
    ///     扫描 SPIR-V 模块，提取统计信息的
    /// </summary>
    /// <returns>SPIR-V 统计信息的/returns>
    public SpirvStatistics scan_statistics()
    {
        var header = scan_header();

        var entryPointCount = 0;
        var capabilityCount = 0;
        var decorationCount = 0;
        var typeCount = 0;
        var nameCount = 0;
        var instructionCount = 0;
        var entryPoints = new List<(SpirvExecutionModel ExecutionModel, string Name)>();
        var capabilities = new List<SpirvCapability>();

        var offset = 20;

        while (offset + 4 <= _scanner.length)
        {
            var firstWord = _scanner.buffer.read_i32_at(offset, false) & 0xFFFFFFFF;
            var wordCount = (ushort)(firstWord >> 16);
            var opcode = (SpirvOpCode)(firstWord & 0xFFFF);

            if (wordCount == 0) break;

            instructionCount++;

            if (opcode == SpirvOpCode.op_entry_point)
            {
                entryPointCount++;

                if (offset + 12 <= _scanner.length && wordCount >= 3)
                {
                    var executionModel = (SpirvExecutionModel)_scanner.buffer.read_u32_at(offset + 4);
                    var nameStart = offset + 12;
                    var name = _scanner.buffer.read_string_at(nameStart);
                    entryPoints.Add((executionModel, name));
                }
            }
            else if (opcode == SpirvOpCode.op_capability)
            {
                capabilityCount++;

                if (offset + 8 <= _scanner.length)
                {
                    var capability = (SpirvCapability)_scanner.buffer.read_u32_at(offset + 4);
                    capabilities.Add(capability);
                }
            }
            else if (opcode is SpirvOpCode.op_decorate or SpirvOpCode.op_member_decorate)
            {
                decorationCount++;
            }
            else if (opcode is >= SpirvOpCode.op_type_void and <= SpirvOpCode.op_type_forward_pointer
                     or SpirvOpCode.op_type_pipe
                     or SpirvOpCode.op_type_acceleration_structure_khr
                     or SpirvOpCode.op_type_cooperative_matrix_nv)
            {
                typeCount++;
            }
            else if (opcode == SpirvOpCode.op_name)
            {
                nameCount++;
            }

            offset += wordCount * 4;
        }

        return new SpirvStatistics
        {
            version = header.version,
            generator_magic = header.generator_magic,
            bound = header.bound,
            instruction_count = instructionCount,
            entry_point_count = entryPointCount,
            capability_count = capabilityCount,
            decoration_count = decorationCount,
            type_count = typeCount,
            name_count = nameCount,
            entry_points = entryPoints,
            capabilities = capabilities
        };
    }

    /// <summary>
    ///     扫描 SPIR-V 模块，提取入口点名称列表的
    /// </summary>
    /// <returns>入口点名称列表的/returns>
    public List<string> scan_entry_point_names()
    {
        var names = new List<string>();
        var offset = 20;

        while (offset + 4 <= _scanner.length)
        {
            var firstWord = _scanner.buffer.read_i32_at(offset, false) & 0xFFFFFFFF;
            var wordCount = (ushort)(firstWord >> 16);
            var opcode = (SpirvOpCode)(firstWord & 0xFFFF);

            if (wordCount == 0) break;

            if (opcode == SpirvOpCode.op_entry_point && wordCount >= 3)
            {
                var nameStart = offset + 12;
                var name = _scanner.buffer.read_string_at(nameStart);
                names.Add(name);
            }

            offset += wordCount * 4;
        }

        return names;
    }
}

/// <summary>
///     SPIR-V 文件头信息的
/// </summary>
public sealed class SpirvHeader
{
    /// <summary>
    ///     SPIR-V 版本号的
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     生成器魔数的
    /// </summary>
    public uint generator_magic { get; init; }

    /// <summary>
    ///     ID 绑定值的
    /// </summary>
    public uint bound { get; init; }

    /// <summary>
    ///     保留字的
    /// </summary>
    public uint schema { get; init; }

    /// <summary>
    ///     版本号字符串表示（如 "1.0"的1.3"的1.5"）的
    /// </summary>
    public string version_string
    {
        get
        {
            var major = (version >> 16) & 0xFF;
            var minor = (version >> 8) & 0xFF;
            return $"{major}.{minor}";
        }
    }
}

/// <summary>
///     SPIR-V 模块统计信息的
/// </summary>
public sealed class SpirvStatistics
{
    /// <summary>
    ///     SPIR-V 版本号的
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     生成器魔数的
    /// </summary>
    public uint generator_magic { get; init; }

    /// <summary>
    ///     ID 绑定值的
    /// </summary>
    public uint bound { get; init; }

    /// <summary>
    ///     指令总数的
    /// </summary>
    public int instruction_count { get; init; }

    /// <summary>
    ///     入口点数量的
    /// </summary>
    public int entry_point_count { get; init; }

    /// <summary>
    ///     能力声明数量的
    /// </summary>
    public int capability_count { get; init; }

    /// <summary>
    ///     装饰指令数量的
    /// </summary>
    public int decoration_count { get; init; }

    /// <summary>
    ///     类型指令数量的
    /// </summary>
    public int type_count { get; init; }

    /// <summary>
    ///     名称指令数量的
    /// </summary>
    public int name_count { get; init; }

    /// <summary>
    ///     入口点列表（执行模型和名称）的
    /// </summary>
    public IReadOnlyList<(SpirvExecutionModel ExecutionModel, string Name)> entry_points { get; init; } = [];

    /// <summary>
    ///     能力声明列表的
    /// </summary>
    public IReadOnlyList<SpirvCapability> capabilities { get; init; } = [];

    /// <summary>
    ///     版本号字符串表示的
    /// </summary>
    public string version_string
    {
        get
        {
            var major = (version >> 16) & 0xFF;
            var minor = (version >> 8) & 0xFF;
            return $"{major}.{minor}";
        }
    }
}