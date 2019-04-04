using Std.Data.Binary.Frame;
using Std.Data.Binary.Wasm.Data;

namespace Std.Data.Binary.Wasm.Decode;

/// <summary>
///     WebAssembly 二进制解码器，将 Wasm 二进制格式解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     WebAssembly 二进制格式使用小端序的LEB128 变长整数编码的
///     解码器按的Wasm MVP（版的1）规范将模块数据解析的C# 数据结构的
/// </remarks>
public static class WasmDecoder
{
    /// <summary>
    ///     解码完整的Wasm 模块的
    /// </summary>
    /// <param name="data">
    ///     要解码的二进制数据的/param>
    ///     <returns>解码后的 Wasm 模块数据的/returns>
    public static WasmModuleData decode_module(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        var version = read_and_validate_header(ref buffer);

        var types = new List<WasmFunctionType>();
        var imports = new List<WasmImport>();
        var functionTypeIndices = new List<uint>();
        var tables = new List<WasmTable>();
        var memories = new List<WasmMemory>();
        var globals = new List<WasmGlobal>();
        var exports = new List<WasmExport>();
        uint? startFunctionIndex = null;
        var elements = new List<WasmElement>();
        var codes = new List<WasmCode>();
        var dataSegments = new List<WasmData>();
        var customSections = new List<WasmCustomSection>();

        while (!buffer.is_end)
        {
            var sectionId = buffer.read_u8();
            var sectionSize = buffer.read_leb128_u32();
            var sectionEnd = buffer.position + (int)sectionSize;

            switch (sectionId)
            {
                case 0:
                    customSections.Add(read_custom_section(ref buffer, sectionSize));
                    break;
                case 1:
                    types = read_type_section(ref buffer);
                    break;
                case 2:
                    imports = read_import_section(ref buffer);
                    break;
                case 3:
                    functionTypeIndices = read_function_section(ref buffer);
                    break;
                case 4:
                    tables = read_table_section(ref buffer);
                    break;
                case 5:
                    memories = read_memory_section(ref buffer);
                    break;
                case 6:
                    globals = read_global_section(ref buffer);
                    break;
                case 7:
                    exports = read_export_section(ref buffer);
                    break;
                case 8:
                    startFunctionIndex = read_start_section(ref buffer);
                    break;
                case 9:
                    elements = read_element_section(ref buffer);
                    break;
                case 10:
                    codes = read_code_section(ref buffer);
                    break;
                case 11:
                    dataSegments = read_data_section(ref buffer);
                    break;
                default:
                    buffer.position = sectionEnd;
                    break;
            }

            if (buffer.position < sectionEnd) buffer.position = sectionEnd;
        }

        return new WasmModuleData
        {
            version = version,
            types = types,
            imports = imports,
            function_type_indices = functionTypeIndices,
            tables = tables,
            memories = memories,
            globals = globals,
            exports = exports,
            start_function_index = startFunctionIndex,
            elements = elements,
            codes = codes,
            data_segments = dataSegments,
            custom_sections = customSections
        };
    }

    /// <summary>
    ///     读取并验的Wasm 文件头的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <returns>Wasm 版本号的/returns>
    public static uint read_and_validate_header(ref ByteBuffer buffer)
    {
        var magic = buffer.read_bytes(4);
        if (!magic.SequenceEqual(WasmConstants.magic_number)) throw new InvalidDataException("Wasm 文件魔数不匹配，期望 \\0asm");

        var version = buffer.read_u32_le();
        if (version != WasmConstants.version)
            throw new InvalidDataException($"不支持的 Wasm 版本：{version}，仅支持版本 {WasmConstants.version}");

        return version;
    }

    #region 辅助方法

    private static byte[] read_init_expression(ref ByteBuffer buffer)
    {
        using var ms = new MemoryStream();

        while (true)
        {
            var opcode = buffer.read_u8();
            ms.WriteByte(opcode);

            if (opcode == (byte)WasmInitOpCode.end) break;

            switch ((WasmInitOpCode)opcode)
            {
                case WasmInitOpCode.i32_const:
                    var i32Value = buffer.read_leb128_i32();
                    var i32Writer = new ByteBufferWriter(16);
                    i32Writer.write_leb128_i32(i32Value);
                    ms.Write([.. i32Writer.written_data]);
                    break;
                case WasmInitOpCode.i64_const:
                    var i64Value = buffer.read_leb128_i64();
                    var i64Writer = new ByteBufferWriter(16);
                    i64Writer.write_leb128_i64(i64Value);
                    ms.Write([.. i64Writer.written_data]);
                    break;
                case WasmInitOpCode.f32_const:
                    var f32Bytes = buffer.read_bytes(4).ToArray();
                    ms.Write(f32Bytes);
                    break;
                case WasmInitOpCode.f64_const:
                    var f64Bytes = buffer.read_bytes(8).ToArray();
                    ms.Write(f64Bytes);
                    break;
                case WasmInitOpCode.global_get:
                    var globalIdx = buffer.read_leb128_u32();
                    var globalIdxWriter = new ByteBufferWriter(16);
                    globalIdxWriter.write_leb128_u32(globalIdx);
                    ms.Write([.. globalIdxWriter.written_data]);
                    break;
                default:
                    throw new InvalidDataException($"初始化表达式中不支持的操作码的x{opcode:X2}");
            }
        }

        return ms.ToArray();
    }

    #endregion

    #region 段读取方的

    private static WasmCustomSection read_custom_section(ref ByteBuffer buffer, uint sectionSize)
    {
        var posBeforeName = buffer.position;
        var name = buffer.read_leb128_string();
        var nameBytesRead = buffer.position - posBeforeName;
        var remainingSize = (int)sectionSize - nameBytesRead;
        var data = buffer.read_bytes(remainingSize).ToArray();

        return new WasmCustomSection
        {
            name = name,
            data = data
        };
    }

    private static List<WasmFunctionType> read_type_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var types = new List<WasmFunctionType>((int)count);

        for (var i = 0; i < count; i++) types.Add(read_function_type(ref buffer));

        return types;
    }

    private static List<WasmImport> read_import_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var imports = new List<WasmImport>((int)count);

        for (var i = 0; i < count; i++) imports.Add(read_import(ref buffer));

        return imports;
    }

    private static List<uint> read_function_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var indices = new List<uint>((int)count);

        for (var i = 0; i < count; i++) indices.Add(buffer.read_leb128_u32());

        return indices;
    }

    private static List<WasmTable> read_table_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var tables = new List<WasmTable>((int)count);

        for (var i = 0; i < count; i++) tables.Add(read_table(ref buffer));

        return tables;
    }

    private static List<WasmMemory> read_memory_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var memories = new List<WasmMemory>((int)count);

        for (var i = 0; i < count; i++) memories.Add(read_memory(ref buffer));

        return memories;
    }

    private static List<WasmGlobal> read_global_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var globals = new List<WasmGlobal>((int)count);

        for (var i = 0; i < count; i++) globals.Add(read_global(ref buffer));

        return globals;
    }

    private static List<WasmExport> read_export_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var exports = new List<WasmExport>((int)count);

        for (var i = 0; i < count; i++) exports.Add(read_export(ref buffer));

        return exports;
    }

    private static uint read_start_section(ref ByteBuffer buffer)
    {
        return buffer.read_leb128_u32();
    }

    private static List<WasmElement> read_element_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var elements = new List<WasmElement>((int)count);

        for (var i = 0; i < count; i++) elements.Add(read_element(ref buffer));

        return elements;
    }

    private static List<WasmCode> read_code_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var codes = new List<WasmCode>((int)count);

        for (var i = 0; i < count; i++) codes.Add(read_code(ref buffer));

        return codes;
    }

    private static List<WasmData> read_data_section(ref ByteBuffer buffer)
    {
        var count = buffer.read_leb128_u32();
        var dataSegments = new List<WasmData>((int)count);

        for (var i = 0; i < count; i++) dataSegments.Add(read_data_segment(ref buffer));

        return dataSegments;
    }

    #endregion

    #region 类型读取方法

    private static WasmFunctionType read_function_type(ref ByteBuffer buffer)
    {
        var form = buffer.read_u8();

        if (form != WasmConstants.function_type_form)
            throw new InvalidDataException($"无效的函数类型标记：0x{form:X2}，期的0x{WasmConstants.function_type_form:X2}");

        var paramCount = buffer.read_leb128_u32();
        var parameters = new List<WasmValueType>((int)paramCount);

        for (var i = 0; i < paramCount; i++) parameters.Add(read_value_type(ref buffer));

        var resultCount = buffer.read_leb128_u32();
        var results = new List<WasmValueType>((int)resultCount);

        for (var i = 0; i < resultCount; i++) results.Add(read_value_type(ref buffer));

        return new WasmFunctionType
        {
            parameters = parameters,
            results = results
        };
    }

    private static WasmValueType read_value_type(ref ByteBuffer buffer)
    {
        var code = buffer.read_u8();

        return code switch
        {
            0x7F => WasmValueType.int32,
            0x7E => WasmValueType.int64,
            0x7D => WasmValueType.float32,
            0x7C => WasmValueType.float64,
            0x70 => WasmValueType.func_ref,
            0x6F => WasmValueType.extern_ref,
            _ => throw new InvalidDataException($"未知的值类型：0x{code:X2}")
        };
    }

    private static WasmLimits read_limits(ref ByteBuffer buffer)
    {
        var flags = buffer.read_u8();
        var minimum = buffer.read_leb128_u32();
        uint? maximum = null;

        if (flags == 1) maximum = buffer.read_leb128_u32();

        return new WasmLimits
        {
            minimum = minimum,
            maximum = maximum
        };
    }

    private static WasmTableType read_table_type(ref ByteBuffer buffer)
    {
        var elementType = read_value_type(ref buffer);
        var limits = read_limits(ref buffer);

        return new WasmTableType
        {
            element_type = elementType,
            limits = limits
        };
    }

    private static WasmMemoryType read_memory_type(ref ByteBuffer buffer)
    {
        var limits = read_limits(ref buffer);

        return new WasmMemoryType
        {
            limits = limits
        };
    }

    private static WasmGlobalType read_global_type(ref ByteBuffer buffer)
    {
        var valueType = read_value_type(ref buffer);
        var mutable = buffer.read_u8() == WasmConstants.global_mutable;

        return new WasmGlobalType
        {
            value_type = valueType,
            mutable = mutable
        };
    }

    private static WasmImport read_import(ref ByteBuffer buffer)
    {
        var module = buffer.read_leb128_string();
        var field = buffer.read_leb128_string();
        var descriptor = read_import_descriptor(ref buffer);

        return new WasmImport
        {
            module = module,
            field = field,
            descriptor = descriptor
        };
    }

    private static WasmImportDescriptor read_import_descriptor(ref ByteBuffer buffer)
    {
        var kind = (WasmExternalKind)buffer.read_u8();

        switch (kind)
        {
            case WasmExternalKind.function:
                return new WasmImportDescriptor
                {
                    kind = kind,
                    function_type_index = buffer.read_leb128_u32()
                };
            case WasmExternalKind.table:
                return new WasmImportDescriptor
                {
                    kind = kind,
                    table_type = read_table_type(ref buffer)
                };
            case WasmExternalKind.memory:
                return new WasmImportDescriptor
                {
                    kind = kind,
                    memory_type = read_memory_type(ref buffer)
                };
            case WasmExternalKind.global:
                return new WasmImportDescriptor
                {
                    kind = kind,
                    global_type = read_global_type(ref buffer)
                };
            default:
                throw new InvalidDataException($"未知的导入种类：{kind}");
        }
    }

    private static WasmTable read_table(ref ByteBuffer buffer)
    {
        return new WasmTable
        {
            type = read_table_type(ref buffer)
        };
    }

    private static WasmMemory read_memory(ref ByteBuffer buffer)
    {
        return new WasmMemory
        {
            type = read_memory_type(ref buffer)
        };
    }

    private static WasmGlobal read_global(ref ByteBuffer buffer)
    {
        var type = read_global_type(ref buffer);
        var initExpression = read_init_expression(ref buffer);

        return new WasmGlobal
        {
            type = type,
            init_expression = initExpression
        };
    }

    private static WasmExport read_export(ref ByteBuffer buffer)
    {
        var name = buffer.read_leb128_string();
        var kind = (WasmExternalKind)buffer.read_u8();
        var index = buffer.read_leb128_u32();

        return new WasmExport
        {
            name = name,
            kind = kind,
            index = index
        };
    }

    private static WasmElement read_element(ref ByteBuffer buffer)
    {
        var flags = buffer.read_leb128_u32();
        uint tableIndex = 0;
        byte[]? offsetExpression = null;

        switch (flags)
        {
            case 0:
                tableIndex = 0;
                offsetExpression = read_init_expression(ref buffer);
                break;
            case 1:
                offsetExpression = null;
                break;
            case 2:
                tableIndex = buffer.read_leb128_u32();
                offsetExpression = read_init_expression(ref buffer);
                break;
            default:
                throw new InvalidDataException($"不支持的元素段标志：{flags}");
        }

        var count = buffer.read_leb128_u32();
        var initValues = new List<uint>((int)count);

        for (var i = 0; i < count; i++) initValues.Add(buffer.read_leb128_u32());

        return new WasmElement
        {
            table_index = tableIndex,
            offset_expression = offsetExpression ?? [],
            init_values = initValues
        };
    }

    private static WasmCode read_code(ref ByteBuffer buffer)
    {
        var bodySize = buffer.read_leb128_u32();
        var bodyStart = buffer.position;

        var localCount = buffer.read_leb128_u32();
        var locals = new List<WasmLocal>((int)localCount);

        for (var i = 0; i < localCount; i++)
        {
            var count = buffer.read_leb128_u32();
            var type = read_value_type(ref buffer);
            locals.Add(new WasmLocal { count = count, type = type });
        }

        var remainingSize = (int)(bodyStart + bodySize - buffer.position);
        var body = buffer.read_bytes(remainingSize).ToArray();

        return new WasmCode
        {
            locals = locals,
            body = body
        };
    }

    private static WasmData read_data_segment(ref ByteBuffer buffer)
    {
        var flags = buffer.read_leb128_u32();
        uint memoryIndex = 0;
        byte[]? offsetExpression = null;

        switch (flags)
        {
            case 0:
                memoryIndex = 0;
                offsetExpression = read_init_expression(ref buffer);
                break;
            case 1:
                offsetExpression = null;
                break;
            case 2:
                memoryIndex = buffer.read_leb128_u32();
                offsetExpression = read_init_expression(ref buffer);
                break;
            default:
                throw new InvalidDataException($"不支持的数据段标志：{flags}");
        }

        var dataSize = buffer.read_leb128_u32();
        var initializer = buffer.read_bytes((int)dataSize).ToArray();

        return new WasmData
        {
            memory_index = memoryIndex,
            offset_expression = offsetExpression ?? [],
            initializer = initializer
        };
    }

    #endregion
}