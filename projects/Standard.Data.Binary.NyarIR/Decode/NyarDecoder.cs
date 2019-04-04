using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Decode;

public sealed class NyarDecoder
{
    public NyarModuleData decode(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        return decode_from_buffer(ref buffer);
    }

    private NyarModuleData decode_from_buffer(ref ByteBuffer buffer)
    {
        var header = read_header(ref buffer);
        if (!header.is_valid)
            throw new InvalidNyarDataException(
                $"无效的.nyar 文件头：magic=0x{header.magic:X8}, version={header.version}");

        var sections = read_section_headers(ref buffer, header.section_count);
        var moduleName = read_module_name(ref buffer, header.name_offset);

        var constants = new List<NyarConstant>();
        var functions = new List<NyarFunction>();
        var imports = new List<NyarImport>();
        var exports = new List<NyarExport>();
        var witnessEntries = new List<NyarWitnessDispatchEntry>();
        byte[]? codeBytes = null;

        foreach (var section in sections)
        {
            buffer.position = section.offset;
            codeBytes = decode_section(ref buffer, section, constants, functions, imports, exports, witnessEntries,
                codeBytes);
        }

        return new NyarModuleData
        {
            name = moduleName,
            version = header.version,
            constants = constants,
            functions = functions,
            imports = imports,
            exports = exports,
            witness_entries = witnessEntries,
            code_bytes = codeBytes
        };
    }

    #region 头部读取

    private static NyarFileHeader read_header(ref ByteBuffer buffer)
    {
        var header = new NyarFileHeader
        {
            magic = buffer.read_u32_be(),
            version = buffer.read_u32_le(),
            section_count = buffer.read_i32_le(),
            name_offset = buffer.read_i32_le()
        };
        return header;
    }

    private static List<NyarSectionHeader> read_section_headers(ref ByteBuffer buffer, int count)
    {
        var sections = new List<NyarSectionHeader>(count);
        for (var i = 0; i < count; i++)
        {
            var item = new NyarSectionHeader
            {
                kind = (NyarSectionKind)buffer.read_u8(),
                offset = buffer.read_i32_le(),
                size = buffer.read_i32_le()
            };
            sections.Add(item);
        }

        return sections;
    }

    private static string read_module_name(ref ByteBuffer buffer, int nameOffset)
    {
        if (nameOffset <= 0) return "<unknown>";

        var savedPosition = buffer.position;
        buffer.position = nameOffset;
        var nameLength = buffer.read_i32_le();
        var name = buffer.read_string(nameLength);
        buffer.position = savedPosition;
        return name;
    }

    #endregion

    #region 段解的

    private static byte[]? decode_section(ref ByteBuffer buffer, NyarSectionHeader section,
        List<NyarConstant> constants, List<NyarFunction> functions,
        List<NyarImport> imports, List<NyarExport> exports, List<NyarWitnessDispatchEntry> witnessEntries,
        byte[]? currentCodeBytes)
    {
        switch (section.kind)
        {
            case NyarSectionKind.constants:
                decode_constants(ref buffer, constants);
                break;
            case NyarSectionKind.functions:
                decode_functions(ref buffer, functions);
                break;
            case NyarSectionKind.code:
                return [.. buffer.read_bytes(section.size)];
            case NyarSectionKind.imports:
                decode_imports(ref buffer, imports);
                break;
            case NyarSectionKind.exports:
                decode_exports(ref buffer, exports);
                break;
            case NyarSectionKind.witness_entries:
                decode_witness_entries(ref buffer, witnessEntries);
                break;
        }

        return currentCodeBytes;
    }

    private static void decode_constants(ref ByteBuffer buffer, List<NyarConstant> constants)
    {
        var count = buffer.read_i32_le();
        constants.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var kind = (NyarConstantKind)buffer.read_u8();
            constants.Add(decode_constant(ref buffer, kind));
        }
    }

    private static NyarConstant decode_constant(ref ByteBuffer buffer, NyarConstantKind kind)
    {
        return kind switch
        {
            NyarConstantKind.integer32 => new NyarConstant(kind, buffer.read_i32_le()),
            NyarConstantKind.float64 => new NyarConstant(kind, buffer.read_f64_le()),
            NyarConstantKind.boolean => new NyarConstant(kind, buffer.read_u8() != 0),
            NyarConstantKind.@null => new NyarConstant(kind, null),
            NyarConstantKind.@string => new NyarConstant(kind, read_length_prefixed_string(ref buffer)),
            NyarConstantKind.big_int => new NyarConstant(kind, decode_big_int_bytes(ref buffer)),
            _ => throw new InvalidNyarDataException($"未知的常量类型：{kind}")
        };
    }

    private static byte[] decode_big_int_bytes(ref ByteBuffer buffer)
    {
        var byteCount = buffer.read_i32_le();
        var bytes = buffer.read_bytes(byteCount);
        return [.. bytes];
    }

    private static void decode_functions(ref ByteBuffer buffer, List<NyarFunction> functions)
    {
        var count = buffer.read_i32_le();
        functions.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var name = read_length_prefixed_string(ref buffer);
            var arity = buffer.read_i32_le();
            var localCount = buffer.read_i32_le();
            var codeOffset = buffer.read_i32_le();
            var codeLength = buffer.read_i32_le();
            functions.Add(new NyarFunction(name, arity, localCount, codeOffset,
                codeLength));
        }
    }

    private static void decode_imports(ref ByteBuffer buffer, List<NyarImport> imports)
    {
        var count = buffer.read_i32_le();
        imports.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var kind = (NyarImportKind)buffer.read_u8();
            var moduleName = read_length_prefixed_string(ref buffer);
            var symbolName = read_length_prefixed_string(ref buffer);
            imports.Add(new NyarImport(kind, moduleName, symbolName));
        }
    }

    private static void decode_exports(ref ByteBuffer buffer, List<NyarExport> exports)
    {
        var count = buffer.read_i32_le();
        exports.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var kind = (NyarExportKind)buffer.read_u8();
            var symbolName = read_length_prefixed_string(ref buffer);
            var functionIndex = buffer.read_i32_le();
            exports.Add(new NyarExport(kind, symbolName, functionIndex));
        }
    }

    private static void decode_witness_entries(ref ByteBuffer buffer, List<NyarWitnessDispatchEntry> witnessEntries)
    {
        var count = buffer.read_i32_le();
        witnessEntries.Capacity = count;
        for (var i = 0; i < count; i++)
            witnessEntries.Add(new NyarWitnessDispatchEntry
            {
                method_id = buffer.read_i32_le(),
                type_id = buffer.read_i32_le(),
                method_name = read_length_prefixed_string(ref buffer),
                function_index = buffer.read_i32_le(),
                interface_id = buffer.read_i32_le(),
                interface_method_index = buffer.read_i32_le()
            });
    }

    private static string read_length_prefixed_string(ref ByteBuffer buffer)
    {
        var length = buffer.read_i32_le();
        return buffer.read_string(length);
    }

    #endregion

    #region 内部结构

    private sealed class NyarFileHeader
    {
        public uint magic;
        public int name_offset;
        public int section_count;
        public uint version;

        public bool is_valid => magic == NyarConstants.magic_value;
    }

    private sealed class NyarSectionHeader
    {
        public NyarSectionKind kind;
        public int offset;
        public int size;
    }

    #endregion
}