using System.Text;
using Std.Data.Binary.Wasm.Data;

namespace Std.Data.Binary.Wasm.Encode;

/// <summary>
///     WebAssembly 二进制编码器，将 C# 数据结构编码的Wasm 二进制格式的
/// </summary>
/// <remarks>
///     WebAssembly 二进制格式使用小端序的LEB128 变长整数编码的
///     编码器按的Wasm MVP（版的1）规范将模块数据写入二进制缓冲区的
///     注意：BinaryWriter.Write(byte[]) 会写入长度前缀，因此所有字节数组写的
///     必须使用 WriteRaw 扩展方法以避免多余的长度前缀的
/// </remarks>
public static class WasmEncoder
{
    /// <summary>
    ///     将完整的 Wasm 模块编码写入缓冲区的
    /// </summary>
    public static int encode_module(Span<byte> buffer, WasmModuleData module)
    {
        var bytes = encode_module(module);
        bytes.CopyTo(buffer);
        return bytes.Length;
    }

    /// <summary>
    ///     将完整的 Wasm 模块编码为字节数组的
    /// </summary>
    public static byte[] encode_module(WasmModuleData module)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        #region Header

        writer.Write(WasmConstants.magic_number);
        writer.write_le(module.version);

        #endregion

        #region Type Section

        if (module.types.Count > 0 || module.gc_sub_types is { Count: > 0 })
        {
            var funcTypeCount = (uint)module.types.Count;
            var gcSubTypeCount = module.gc_sub_types?.Count ?? 0;
            var hasGcType = gcSubTypeCount > 0;
            // totalTypeCount = 函数类型数 + GC rec group 的 comptype 数
            // rec group（0x4E）是一个 comptype，包含 N 个 sub-type
            // vec(comptype) 的计数是 comptype 个数，不是 type index 个数
            var totalTypeCount = funcTypeCount + (hasGcType ? 1u : 0u);

            var sectionData = build_bytes(w =>
            {
                w.write_leb128(totalTypeCount);

                for (var idx = 0; idx < funcTypeCount; idx++)
                {
                    var type = module.types[idx];
                    w.Write(WasmConstants.function_type_form);
                    w.write_leb128((uint)type.parameters.Count);
                    foreach (var param in type.parameters) w.Write((byte)param);

                    w.write_leb128((uint)type.results.Count);
                    foreach (var result in type.results) w.Write((byte)result);
                }

                if (hasGcType)
                {
                    w.Write(WasmConstants.rec_type_form);
                    w.write_leb128((uint)module.gc_sub_types!.Count);
                    foreach (var subType in module.gc_sub_types) encode_sub_type(w, subType);
                }
            });
            writer.Write((byte)WasmSectionId.type);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Import Section

        if (module.imports.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.imports.Count);
                foreach (var import in module.imports)
                {
                    write_name(w, import.module);
                    write_name(w, import.field);
                    w.Write((byte)import.descriptor.kind);

                    switch (import.descriptor.kind)
                    {
                        case WasmExternalKind.function:
                            w.write_leb128(import.descriptor.function_type_index);
                            break;
                        case WasmExternalKind.table:
                            w.Write((byte)import.descriptor.table_type!.element_type);
                            write_limits(w, import.descriptor.table_type.limits);
                            break;
                        case WasmExternalKind.memory:
                            write_limits(w, import.descriptor.memory_type!.limits);
                            break;
                        case WasmExternalKind.global:
                            w.Write((byte)import.descriptor.global_type!.value_type);
                            w.Write(import.descriptor.global_type.mutable
                                ? WasmConstants.global_mutable
                                : WasmConstants.global_immutable);
                            break;
                    }
                }
            });
            writer.Write((byte)WasmSectionId.import);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Function Section

        if (module.function_type_indices.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.function_type_indices.Count);
                foreach (var index in module.function_type_indices) w.write_leb128(index);
            });
            writer.Write((byte)WasmSectionId.function);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Table Section

        if (module.tables.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.tables.Count);
                foreach (var table in module.tables)
                {
                    w.Write((byte)table.type.element_type);
                    write_limits(w, table.type.limits);
                }
            });
            writer.Write((byte)WasmSectionId.table);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Memory Section

        if (module.memories.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.memories.Count);
                foreach (var memory in module.memories) write_limits(w, memory.type.limits);
            });
            writer.Write((byte)WasmSectionId.memory);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Global Section

        if (module.globals.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.globals.Count);
                foreach (var global in module.globals)
                {
                    w.Write((byte)global.type.value_type);
                    w.Write(global.type.mutable ? WasmConstants.global_mutable : WasmConstants.global_immutable);
                    w.write_raw(global.init_expression);
                }
            });
            writer.Write((byte)WasmSectionId.global);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Export Section

        if (module.exports.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.exports.Count);
                foreach (var export in module.exports)
                {
                    write_name(w, export.name);
                    w.Write((byte)export.kind);
                    w.write_leb128(export.index);
                }
            });
            writer.Write((byte)WasmSectionId.export);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Start Section

        if (module.start_function_index.HasValue)
        {
            var sectionData = build_bytes(w => w.write_leb128(module.start_function_index.Value));
            writer.Write((byte)WasmSectionId.start);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Element Section

        if (module.elements.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.elements.Count);
                foreach (var element in module.elements)
                {
                    if (element.table_index == 0)
                    {
                        w.write_leb128(0u);
                    }
                    else
                    {
                        w.write_leb128(2u);
                        w.write_leb128(element.table_index);
                    }

                    w.write_raw(element.offset_expression);
                    w.write_leb128((uint)element.init_values.Count);
                    foreach (var value in element.init_values) w.write_leb128(value);
                }
            });
            writer.Write((byte)WasmSectionId.element);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Code Section

        if (module.codes.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.codes.Count);
                foreach (var code in module.codes)
                {
                    var bodyData = build_bytes(bw =>
                    {
                        bw.write_leb128((uint)code.locals.Count);
                        foreach (var local in code.locals)
                        {
                            bw.write_leb128(local.count);
                            bw.Write((byte)local.type);
                        }

                        bw.write_raw(code.body);
                    });
                    w.write_leb128((uint)bodyData.Length);
                    w.write_raw(bodyData);
                }
            });
            writer.Write((byte)WasmSectionId.code);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Data Section

        if (module.data_segments.Count > 0)
        {
            var sectionData = build_bytes(w =>
            {
                w.write_leb128((uint)module.data_segments.Count);
                foreach (var data in module.data_segments)
                {
                    if (data.memory_index == 0)
                    {
                        w.write_leb128(0u);
                    }
                    else
                    {
                        w.write_leb128(2u);
                        w.write_leb128(data.memory_index);
                    }

                    w.write_raw(data.offset_expression);
                    w.write_leb128((uint)data.initializer.Length);
                    w.write_raw(data.initializer);
                }
            });
            writer.Write((byte)WasmSectionId.data);
            writer.write_leb128((uint)sectionData.Length);
            writer.write_raw(sectionData);
        }

        #endregion

        #region Custom Sections

        if (module.custom_sections.Count > 0)
            foreach (var custom in module.custom_sections)
            {
                var sectionData = build_bytes(w =>
                {
                    write_name(w, custom.name);
                    w.write_raw(custom.data);
                });
                writer.Write((byte)WasmSectionId.custom);
                writer.write_leb128((uint)sectionData.Length);
                writer.write_raw(sectionData);
            }

        #endregion

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    ///     写入 Wasm 文件头（魔数和版本号）的
    /// </summary>
    public static void write_header(BinaryWriter writer, uint version = WasmConstants.version)
    {
        writer.Write(WasmConstants.magic_number);
        writer.write_le(version);
    }

    private static void write_name(BinaryWriter writer, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        writer.write_leb128((uint)bytes.Length);
        writer.write_raw(bytes);
    }

    private static void write_limits(BinaryWriter writer, WasmLimits limits)
    {
        if (limits.maximum.HasValue)
        {
            writer.Write(WasmConstants.limits_has_min_max);
            writer.write_leb128(limits.minimum);
            writer.write_leb128(limits.maximum.Value);
        }
        else
        {
            writer.Write(WasmConstants.limits_has_only_min);
            writer.write_leb128(limits.minimum);
        }
    }

    private static byte[] build_bytes(Action<BinaryWriter> writeContent)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        writeContent(w);
        w.Flush();
        return ms.ToArray();
    }

    #region Component Model 编码

    /// <summary>
    ///     的WASM 组件数据编码为字节数组（Component Model 二进制格式）的
    ///     当前为骨架实现，将在后续阶段完善完整的组件编码的
    /// </summary>
    public static byte[] encode_component(WasmComponentData component)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        writer.Write(WasmConstants.magic_number);
        writer.write_le(WasmConstants.component_version);

        writer.Flush();
        return stream.ToArray();
    }

    #endregion

    #region GC 类型编码方法

    /// <summary>
    ///     编码 WASM GC 子类型的
    /// </summary>
    private static void encode_sub_type(BinaryWriter writer, WasmSubType subType)
    {
        // WASM GC 规范：sub final = 0x4F，sub（非 final）= 0x50
        // 这两个字节码本身已经编码了 final 性，不需要额外的 final 标记字节
        var subForm = subType.final ? WasmConstants.sub_final_type : WasmConstants.sub_type_form;
        System.Console.Error.WriteLine($"[DEBUG sub] final={subType.final}, superHasValue={subType.super_type_index.HasValue}, superValue={subType.super_type_index}, formByte=0x{subForm:X2}, pos={writer.BaseStream.Position}");
        writer.Write(subForm);

        // WASM GC 规范要求写入 supertypes 数量 n，再写入 n 个 typeidx
        // 当前用例最多只有一个 supertype
        if (subType.super_type_index.HasValue)
        {
            writer.write_leb128(1u);
            writer.write_leb128(subType.super_type_index.Value);
            System.Console.Error.WriteLine($"[DEBUG sub] wrote superCount=1, superIdx={subType.super_type_index.Value}, posAfterSuper={writer.BaseStream.Position}");
        }
        else
        {
            writer.write_leb128(0u);
            System.Console.Error.WriteLine($"[DEBUG sub] wrote superCount=0, posAfterSuper={writer.BaseStream.Position}");
        }

        var beforePos = writer.BaseStream.Position;
        System.Console.Error.WriteLine($"[DEBUG sub] before encode_composite_type, pos={beforePos}");
        encode_composite_type(writer, subType.type);
        var afterPos = writer.BaseStream.Position;
        System.Console.Error.WriteLine($"[DEBUG sub] after encode_composite_type, pos={afterPos}, bytesWritten={afterPos - beforePos}");
    }

    /// <summary>
    ///     编码 WASM GC 复合类型的
    /// </summary>
    private static void encode_composite_type(BinaryWriter writer, WasmCompositeType type)
    {
        switch (type.kind)
        {
            case WasmCompositeTypeKind.@struct:
                writer.Write(WasmConstants.struct_type_form);
                writer.write_leb128((uint)(type.fields?.Count ?? 0));
                if (type.fields is { } fields)
                    foreach (var field in fields)
                    {
                        encode_storage_type(writer, field.storage_type);
                        writer.Write(field.mutable ? WasmConstants.field_mutable : WasmConstants.field_immutable);
                    }

                break;

            case WasmCompositeTypeKind.array:
                writer.Write(WasmConstants.array_type_form);
                if (type.element_type is { } elemType)
                    encode_storage_type(writer, elemType);
                else
                    writer.Write((byte)WasmValueType.int32);

                writer.Write(WasmConstants.field_mutable);
                break;

            case WasmCompositeTypeKind.rec:
                writer.Write(WasmConstants.rec_type_form);
                writer.write_leb128((uint)(type.sub_types?.Count ?? 0));
                if (type.sub_types is { } subTypes)
                    foreach (var sub in subTypes)
                        encode_sub_type(writer, sub);

                break;
        }
    }

    /// <summary>
    ///     编码 WASM GC 存储类型的
    /// </summary>
    private static void encode_storage_type(BinaryWriter writer, WasmStorageType storageType)
    {
        if (storageType.packed_type.HasValue)
            writer.Write((byte)storageType.packed_type.Value);
        else
            writer.Write((byte)storageType.value_type);
    }

    #endregion

    #region BinaryWriter 扩展方法

    /// <summary>
    ///     将字节数组原样写的BinaryWriter，不添加长度前缀的
    /// </summary>
    /// <remarks>
    ///     BinaryWriter.Write(byte[]) 会先写入 7-bit 编码的长度前缀的
    ///     这在 Wasm 二进制格式中是错误的。此方法直接写入原始字节的
    /// </remarks>
    private static void write_raw(this BinaryWriter writer, byte[] data)
    {
        writer.Write(data, 0, data.Length);
    }

    private static void write_le(this BinaryWriter writer, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);

        writer.Write(bytes, 0, bytes.Length);
    }

    private static void write_leb128(this BinaryWriter writer, uint value)
    {
        do
        {
            var byteVal = value & 0x7F;
            value >>= 7;
            if (value != 0) byteVal |= 0x80;

            writer.Write((byte)byteVal);
        } while (value != 0);
    }

    private static void write_leb128(this BinaryWriter writer, int value)
    {
        writer.write_leb128((uint)value);
    }

    #endregion
}