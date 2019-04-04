using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.SpirV.Data;

namespace Std.Data.Binary.SpirV.Decode;

/// <summary>
///     SPIR-V 模块解码器，的Khronus SPIR-V 二进制中间语言格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     <para>
///         SPIR-V 的Khronos 定义的着色器二进制中间语言，用的Vulkan、OpenCL 等图形和计算 API的
///         解码器解析完整的 SPIR-V 模块结构，提取文件头、指令流、入口点和装饰信息的
///     </para>
///     <para>
///         调用 <see cref="decode_all" /> 可一次解析并缓存所有数据，后续通过属性访问入口点、装饰、名称和类型信息的
///         避免重复解析。单独调的<see cref="decode_entry_points" /> 等方法会每次重新解析，适用于仅需部分数据的场景的
///     </para>
/// </remarks>
public ref struct SpirvDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="SpirvDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">SPIR-V 二进制数据的/param>
    public SpirvDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取最近一的<see cref="decode_all" /> 调用缓存的模块数据，未调用前的null的
    /// </summary>
    public SpirvModuleData? cached_module { get; private set; }

    /// <summary>
    ///     一次解的SPIR-V 模块的所有数据并缓存，后续可通过 <see cref="cached_module" /> 访问的
    /// </summary>
    /// <returns>解码后的模块数据的/returns>
    public SpirvModuleData decode_all()
    {
        var header = read_header();
        var instructions = read_instructions(header.bound);

        var entryPoints = new List<SpirvEntryPoint>();
        var decorations = new List<SpirvDecorationInfo>();
        var names = new List<SpirvName>();
        var types = new List<SpirvTypeInfo>();

        foreach (var instruction in instructions)
            if (instruction.opcode == SpirvOpCode.op_entry_point)
                entryPoints.Add(parse_entry_point(instruction));
            else if (instruction.opcode == SpirvOpCode.op_decorate)
                decorations.Add(parse_decorate(instruction));
            else if (instruction.opcode == SpirvOpCode.op_member_decorate)
                decorations.Add(parse_member_decorate(instruction));
            else if (instruction.opcode == SpirvOpCode.op_name)
                names.Add(parse_name(instruction));
            else if (is_type_instruction(instruction.opcode)) types.Add(parse_type_info(instruction));

        cached_module = new SpirvModuleData
        {
            magic_number = header.magic_number,
            version = header.version,
            generator_magic = header.generator_magic,
            bound = header.bound,
            schema = header.schema,
            instructions = instructions,
            entry_points = entryPoints,
            decorations = decorations,
            names = names,
            types = types
        };

        return cached_module;
    }

    /// <summary>
    ///     的SPIR-V 二进制数据解码模块数据的
    /// </summary>
    /// <returns>解码后的模块数据的/returns>
    public SpirvModuleData decode()
    {
        var header = read_header();
        var instructions = read_instructions(header.bound);

        return new SpirvModuleData
        {
            magic_number = header.magic_number,
            version = header.version,
            generator_magic = header.generator_magic,
            bound = header.bound,
            schema = header.schema,
            instructions = instructions
        };
    }

    /// <summary>
    ///     的SPIR-V 二进制数据中提取入口点信息的
    /// </summary>
    /// <remarks>
    ///     如果已调的<see cref="decode_all" />，则直接返回缓存数据，避免重复解析的
    /// </remarks>
    /// <returns>入口点信息列表的/returns>
    public IReadOnlyList<SpirvEntryPoint> decode_entry_points()
    {
        if (cached_module != null) return cached_module.entry_points;

        var header = read_header();
        var instructions = read_instructions(header.bound);

        var entryPoints = new List<SpirvEntryPoint>();

        foreach (var instruction in instructions)
            if (instruction.opcode == SpirvOpCode.op_entry_point)
                entryPoints.Add(parse_entry_point(instruction));

        return entryPoints;
    }

    /// <summary>
    ///     的SPIR-V 二进制数据中提取装饰信息的
    /// </summary>
    /// <remarks>
    ///     如果已调的<see cref="decode_all" />，则直接返回缓存数据，避免重复解析的
    /// </remarks>
    /// <returns>装饰信息列表的/returns>
    public IReadOnlyList<SpirvDecorationInfo> decode_decorations()
    {
        if (cached_module != null) return cached_module.decorations;

        var header = read_header();
        var instructions = read_instructions(header.bound);

        var decorations = new List<SpirvDecorationInfo>();

        foreach (var instruction in instructions)
            if (instruction.opcode == SpirvOpCode.op_decorate)
                decorations.Add(parse_decorate(instruction));
            else if (instruction.opcode == SpirvOpCode.op_member_decorate)
                decorations.Add(parse_member_decorate(instruction));

        return decorations;
    }

    /// <summary>
    ///     的SPIR-V 二进制数据中提取名称信息的
    /// </summary>
    /// <remarks>
    ///     如果已调的<see cref="decode_all" />，则直接返回缓存数据，避免重复解析的
    /// </remarks>
    /// <returns>名称信息列表的/returns>
    public IReadOnlyList<SpirvName> decode_names()
    {
        if (cached_module != null) return cached_module.names;

        var header = read_header();
        var instructions = read_instructions(header.bound);

        var names = new List<SpirvName>();

        foreach (var instruction in instructions)
            if (instruction.opcode == SpirvOpCode.op_name)
                names.Add(parse_name(instruction));

        return names;
    }

    /// <summary>
    ///     的SPIR-V 二进制数据中提取类型信息的
    /// </summary>
    /// <remarks>
    ///     如果已调的<see cref="decode_all" />，则直接返回缓存数据，避免重复解析的
    /// </remarks>
    /// <returns>类型信息列表的/returns>
    public IReadOnlyList<SpirvTypeInfo> decode_types()
    {
        if (cached_module != null) return cached_module.types;

        var header = read_header();
        var instructions = read_instructions(header.bound);

        var types = new List<SpirvTypeInfo>();

        foreach (var instruction in instructions)
            if (is_type_instruction(instruction.opcode))
                types.Add(parse_type_info(instruction));

        return types;
    }

    #region 私有解析方法

    private SpirvHeaderInfo read_header()
    {
        if (_buffer.length < 20) throw new InvalidDataException("SPIR-V 文件数据过短，无法读取文件头");

        var magicNumber = _buffer.read_u32_le();

        if (magicNumber != SpirvConstants.magic_number)
            throw new InvalidDataException($"SPIR-V 文件魔数不匹配，期望 0x07230203，实的0x{magicNumber:X8}");

        var version = _buffer.read_u32_le();
        var generatorMagic = _buffer.read_u32_le();
        var bound = _buffer.read_u32_le();
        var schema = _buffer.read_u32_le();

        return new SpirvHeaderInfo
        {
            magic_number = magicNumber,
            version = version,
            generator_magic = generatorMagic,
            bound = bound,
            schema = schema
        };
    }

    private List<SpirvInstruction> read_instructions(uint bound)
    {
        var instructions = new List<SpirvInstruction>();

        while (!_buffer.is_end && _buffer.remaining >= 4)
        {
            var firstWord = _buffer.read_u32_le();
            var wordCount = (ushort)(firstWord >> 16);
            var opcode = (SpirvOpCode)(firstWord & 0xFFFF);

            if (wordCount == 0) throw new InvalidDataException("SPIR-V 指令字数不能的0");

            var operandCount = wordCount - 1;
            var operands = new uint[operandCount];

            for (var i = 0; i < operandCount; i++)
            {
                if (_buffer.remaining < 4) throw new InvalidDataException("SPIR-V 指令操作数超出数据范围。");

                operands[i] = _buffer.read_u32_le();
            }

            instructions.Add(new SpirvInstruction
            {
                opcode = opcode,
                word_count = wordCount,
                operands = operands
            });
        }

        return instructions;
    }

    private static SpirvEntryPoint parse_entry_point(SpirvInstruction instruction)
    {
        if (instruction.operands.Count < 3) throw new InvalidDataException("OpEntryPoint 指令操作数不足。");

        var executionModel = (SpirvExecutionModel)instruction.operands[0];
        var entryPointId = instruction.operands[1];
        var name = read_string_from_operands(instruction.operands, 2, out var nameEndIndex);

        var interfaceIds = new List<uint>();

        for (var i = nameEndIndex; i < instruction.operands.Count; i++) interfaceIds.Add(instruction.operands[i]);

        return new SpirvEntryPoint
        {
            execution_model = executionModel,
            entry_point_id = entryPointId,
            name = name,
            interface_ids = interfaceIds
        };
    }

    private static SpirvDecorationInfo parse_decorate(SpirvInstruction instruction)
    {
        if (instruction.operands.Count < 2) throw new InvalidDataException("OpDecorate 指令操作数不足。");

        var targetId = instruction.operands[0];
        var decoration = (SpirvDecoration)instruction.operands[1];
        var extraOperands = instruction.operands.Skip(2).ToArray();

        return new SpirvDecorationInfo
        {
            target_id = targetId,
            decoration = decoration,
            extra_operands = extraOperands
        };
    }

    private static SpirvDecorationInfo parse_member_decorate(SpirvInstruction instruction)
    {
        if (instruction.operands.Count < 3) throw new InvalidDataException("OpMemberDecorate 指令操作数不足。");

        var targetId = instruction.operands[0];
        var decoration = (SpirvDecoration)instruction.operands[2];
        var extraOperands = instruction.operands.Skip(3).ToArray();

        return new SpirvDecorationInfo
        {
            target_id = targetId,
            decoration = decoration,
            extra_operands = extraOperands
        };
    }

    private static SpirvName parse_name(SpirvInstruction instruction)
    {
        if (instruction.operands.Count < 2) throw new InvalidDataException("OpName 指令操作数不足。");

        var targetId = instruction.operands[0];
        var name = read_string_from_operands(instruction.operands, 1, out _);

        return new SpirvName
        {
            target_id = targetId,
            name = name
        };
    }

    private static SpirvTypeInfo parse_type_info(SpirvInstruction instruction)
    {
        var resultId = instruction.operands.Count > 0 ? instruction.operands[0] : 0;
        var operands = instruction.operands.Skip(1).ToArray();

        return new SpirvTypeInfo
        {
            result_id = resultId,
            opcode = instruction.opcode,
            operands = operands
        };
    }

    private static string read_string_from_operands(IReadOnlyList<uint> operands, int startIndex, out int endIndex)
    {
        var bytes = new List<byte>();

        var i = startIndex;

        while (i < operands.Count)
        {
            var word = operands[i];
            bytes.Add((byte)(word & 0xFF));

            if ((word & 0xFF) == 0) break;

            bytes.Add((byte)((word >> 8) & 0xFF));

            if (((word >> 8) & 0xFF) == 0) break;

            bytes.Add((byte)((word >> 16) & 0xFF));

            if (((word >> 16) & 0xFF) == 0) break;

            bytes.Add((byte)((word >> 24) & 0xFF));

            if (((word >> 24) & 0xFF) == 0) break;

            i++;
        }

        endIndex = i + 1;

        var charCount = bytes.Count;

        for (var j = 0; j < bytes.Count; j++)
            if (bytes[j] == 0)
            {
                charCount = j;
                break;
            }

        return Encoding.UTF8.GetString(bytes.ToArray(), 0, charCount);
    }

    private static bool is_type_instruction(SpirvOpCode opcode)
    {
        return opcode is >= SpirvOpCode.op_type_void and <= SpirvOpCode.op_type_forward_pointer
            or SpirvOpCode.op_type_pipe
            or SpirvOpCode.op_type_acceleration_structure_khr
            or SpirvOpCode.op_type_cooperative_matrix_nv
            or SpirvOpCode.op_type_vme_image_intel
            or SpirvOpCode.op_type_avc_ime_payload_intel
            or SpirvOpCode.op_type_avc_ref_payload_intel
            or SpirvOpCode.op_type_avc_sic_payload_intel
            or SpirvOpCode.op_type_avc_mce_payload_intel
            or SpirvOpCode.op_type_avc_mce_result_intel
            or SpirvOpCode.op_type_avc_ime_result_intel
            or SpirvOpCode.op_type_avc_ime_result_single_reference_streamout_intel
            or SpirvOpCode.op_type_avc_ime_result_dual_reference_streamout_intel
            or SpirvOpCode.op_type_avc_ime_single_reference_streamin_intel
            or SpirvOpCode.op_type_avc_ime_dual_reference_streamin_intel
            or SpirvOpCode.op_type_avc_ref_result_intel
            or SpirvOpCode.op_type_avc_sic_result_intel;
    }

    private sealed class SpirvHeaderInfo
    {
        public uint magic_number { get; init; }
        public uint version { get; init; }
        public uint generator_magic { get; init; }
        public uint bound { get; init; }
        public uint schema { get; init; }
    }

    #endregion
}