using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Encode;

/// <summary>
///     Nyar 字节码模块编码器，将 C# 数据结构编码的.nyar 字节码格式的
/// </summary>
/// <remarks>
///     .nyar 的NyarVM 的字节码模块格式，采用分段式二进制布局的
///     编码器将模块数据序列化为符合 NyarVM 规范的二进制数据的
///     二进制布局：[Header 16B] 的[Section Headers N*9B] 的[Name Section] 的[Section Data...]
/// </remarks>
public sealed class NyarEncoder
{
    /// <summary>
    ///     的Nyar 模块数据编码的.nyar 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     Nyar 模块数据的/param>
    ///     <returns>.nyar 二进制数据的/returns>
    public byte[] encode(NyarModuleData data)
    {
        var sections = build_sections(data);
        var size = estimate_size(data, sections);
        var writer = new ByteBufferWriter(size);

        write_header(ref writer, data, sections.Count);
        write_section_headers(ref writer, sections, data.name);
        write_name_section(ref writer, data.name);

        foreach (var section in sections) writer.write(section.data);

        return writer.to_array();
    }

    #region 私有编码方法

    private static List<NyarSection> build_sections(NyarModuleData data)
    {
        var sections = new List<NyarSection>();

        if (data.constants.Count > 0) sections.Add(build_constants_section(data.constants));

        if (data.functions.Count > 0) sections.Add(build_functions_section(data.functions));

        if (data.imports.Count > 0) sections.Add(build_imports_section(data.imports));

        if (data.exports.Count > 0) sections.Add(build_exports_section(data.exports));

        if (data.witness_entries.Count > 0) sections.Add(build_witness_entries_section(data.witness_entries));

        if (data.code_bytes is { Length: > 0 }) sections.Add(build_code_section(data.code_bytes));

        return sections;
    }

    private static NyarSection build_constants_section(IReadOnlyList<NyarConstant> constants)
    {
        var size = 4;

        foreach (var constant in constants)
        {
            size += 1;

            switch (constant.kind)
            {
                case NyarConstantKind.integer32:
                    size += 4;
                    break;
                case NyarConstantKind.float64:
                    size += 8;
                    break;
                case NyarConstantKind.boolean:
                    size += 1;
                    break;
                case NyarConstantKind.@null:
                    break;
                case NyarConstantKind.@string:
                    size += 4 + Encoding.UTF8.GetByteCount((string?)constant.payload ?? "");
                    break;
                case NyarConstantKind.big_int:
                    size += 4 + ((byte[]?)constant.payload ?? []).Length;
                    break;
            }
        }

        var writer = new ByteBufferWriter(size);

        writer.write_i32_le(constants.Count);

        foreach (var constant in constants)
        {
            writer.write_u8((byte)constant.kind);

            switch (constant.kind)
            {
                case NyarConstantKind.integer32:
                    writer.write_i32_le(constant.payload is int i ? i : 0);
                    break;
                case NyarConstantKind.float64:
                    writer.write_f64_le(constant.payload is double d ? d : 0.0);
                    break;
                case NyarConstantKind.boolean:
                    writer.write_u8(constant.payload is bool and true ? (byte)1 : (byte)0);
                    break;
                case NyarConstantKind.@null:
                    break;
                case NyarConstantKind.@string:
                    var strBytes = Encoding.UTF8.GetBytes((string?)constant.payload ?? "");
                    writer.write_i32_le(strBytes.Length);
                    writer.write(strBytes);
                    break;
                case NyarConstantKind.big_int:
                    var bigIntBytes = (byte[]?)constant.payload ?? [];
                    writer.write_i32_le(bigIntBytes.Length);
                    writer.write(bigIntBytes);
                    break;
            }
        }

        return new NyarSection
        {
            kind = NyarSectionKind.constants,
            data = writer.to_array()
        };
    }

    private static NyarSection build_functions_section(IReadOnlyList<NyarFunction> functions)
    {
        var size = 4 + functions.Count * (4 + 4 + 4 + 4 + 4);

        foreach (var func in functions) size += Encoding.UTF8.GetByteCount(func.name);

        var writer = new ByteBufferWriter(size);

        writer.write_i32_le(functions.Count);

        foreach (var func in functions)
        {
            write_binary_writer_string(ref writer, func.name);
            writer.write_i32_le(func.arity);
            writer.write_i32_le(func.local_count);
            writer.write_i32_le(func.code_offset);
            writer.write_i32_le(func.code_length);
        }

        return new NyarSection
        {
            kind = NyarSectionKind.functions,
            data = writer.to_array()
        };
    }

    private static NyarSection build_imports_section(IReadOnlyList<NyarImport> imports)
    {
        var size = 4;

        foreach (var import in imports)
            size += 1 + 4 + Encoding.UTF8.GetByteCount(import.module_name) + 4 +
                    Encoding.UTF8.GetByteCount(import.symbol_name);

        var writer = new ByteBufferWriter(size);

        writer.write_i32_le(imports.Count);

        foreach (var import in imports)
        {
            writer.write_u8((byte)import.kind);
            write_binary_writer_string(ref writer, import.module_name);
            write_binary_writer_string(ref writer, import.symbol_name);
        }

        return new NyarSection
        {
            kind = NyarSectionKind.imports,
            data = writer.to_array()
        };
    }

    private static NyarSection build_exports_section(IReadOnlyList<NyarExport> exports)
    {
        var size = 4;

        foreach (var export in exports) size += 1 + 4 + Encoding.UTF8.GetByteCount(export.symbol_name) + 4;

        var writer = new ByteBufferWriter(size);

        writer.write_i32_le(exports.Count);

        foreach (var export in exports)
        {
            writer.write_u8((byte)export.kind);
            write_binary_writer_string(ref writer, export.symbol_name);
            writer.write_i32_le(export.function_index);
        }

        return new NyarSection
        {
            kind = NyarSectionKind.exports,
            data = writer.to_array()
        };
    }

    private static NyarSection build_witness_entries_section(IReadOnlyList<NyarWitnessDispatchEntry> witnessEntries)
    {
        var size = 4;

        foreach (var entry in witnessEntries)
            size += 4 + 4 + 4 + Encoding.UTF8.GetByteCount(entry.method_name) + 4 + 4 + 4;

        var writer = new ByteBufferWriter(size);
        writer.write_i32_le(witnessEntries.Count);

        foreach (var entry in witnessEntries)
        {
            writer.write_i32_le(entry.method_id);
            writer.write_i32_le(entry.type_id);
            write_binary_writer_string(ref writer, entry.method_name);
            writer.write_i32_le(entry.function_index);
            writer.write_i32_le(entry.interface_id);
            writer.write_i32_le(entry.interface_method_index);
        }

        return new NyarSection
        {
            kind = NyarSectionKind.witness_entries,
            data = writer.to_array()
        };
    }

    /// <summary>
    ///     构建代码段，包含所有函数的扁平指令字节码。
    ///     NyarFunction.CodeOffset/CodeLength 指向此段内的偏移量。
    /// </summary>
    private static NyarSection build_code_section(byte[] codeBytes)
    {
        return new NyarSection
        {
            kind = NyarSectionKind.code,
            data = codeBytes
        };
    }

    private static void write_header(ref ByteBufferWriter writer, NyarModuleData data, int sectionCount)
    {
        writer.write_u32_be(NyarConstants.magic_value);
        writer.write_u32_le(data.version);
        writer.write_i32_le(sectionCount);

        var nameOffset = NyarConstants.header_size + sectionCount * NyarConstants.section_header_size;
        writer.write_i32_le(nameOffset);
    }

    private static void write_section_headers(ref ByteBufferWriter writer, List<NyarSection> sections,
        string moduleName)
    {
        var nameByteCount = Encoding.UTF8.GetByteCount(moduleName);
        var dataStart = NyarConstants.header_size + sections.Count * NyarConstants.section_header_size + 4 +
                        nameByteCount;

        var currentOffset = dataStart;

        for (var i = 0; i < sections.Count; i++)
        {
            writer.write_u8((byte)sections[i].kind);
            writer.write_i32_le(currentOffset);
            writer.write_i32_le(sections[i].data.Length);
            currentOffset += sections[i].data.Length;
        }
    }

    private static void write_name_section(ref ByteBufferWriter writer, string name)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        writer.write_i32_le(nameBytes.Length);
        writer.write(nameBytes);
    }

    private static void write_binary_writer_string(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.write_i32_le(bytes.Length);
        writer.write(bytes);
    }

    private static int estimate_size(NyarModuleData data, List<NyarSection> sections)
    {
        var size = NyarConstants.header_size;
        size += sections.Count * NyarConstants.section_header_size;
        size += 4 + Encoding.UTF8.GetByteCount(data.name);

        foreach (var section in sections) size += section.data.Length;

        return size + 256;
    }

    #endregion
}