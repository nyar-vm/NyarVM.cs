using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Pe.Data;

namespace Std.Data.Binary.Pe.Encode;

/// <summary>
///     PE 文件编码器，的PeFileData 编码的PE 二进制格式的
///     PE 格式固定使用小端序的
///     支持节区内容、导入表和重定位表编码的
/// </summary>
public sealed class PeEncoder
{
    private static readonly byte[] _default_dos_stub =
    [
        0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
        0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
        0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72,
        0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
        0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E,
        0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
        0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A,
        0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    /// <summary>
    ///     编码 PE 文件数据为字节数组的
    /// </summary>
    public byte[] encode(PeFileData data)
    {
        var is64 = data.is64_bit;
        var size = estimate_size(data);
        var writer = new ByteBufferWriter(size);

        write_dos_header(ref writer, data.header);
        write_pe_signature(ref writer);
        write_coff_header(ref writer, data.header);
        write_optional_header(ref writer, data.optional_header, is64);
        write_section_headers(ref writer, data.sections);
        write_section_contents(ref writer, data);
        write_import_table(ref writer, data);
        write_relocation_table(ref writer, data);

        return writer.to_array();
    }

    #region DOS 的

    private static void write_dos_header(ref ByteBufferWriter writer, PeHeaderData header)
    {
        writer.write_u16_le(header.dos_magic);
        writer.write_u16_le(0x0090);
        writer.write_u16_le(0x0003);
        writer.write_u16_le(0x0000);
        writer.write_u16_le(0x0004);
        writer.write_u16_le(0x0000);
        writer.write_u16_le(0xFFFF);
        writer.write_u16_le(0x0000);
        writer.write_u16_le(0x00B8);
        writer.write_u16_le(0x0000);
        writer.write_u16_le(0x0000);
        writer.write_u16_le(0x0000);
        writer.write_u32_le(0x00000040);
        writer.write_u32_le(0x00000000);

        for (var i = 0; i < 14; i++) writer.write_u16_le(0x0000);

        writer.write_u32_le(header.pe_header_offset);
        writer.write(_default_dos_stub);

        write_to_offset(ref writer, (int)header.pe_header_offset);
    }

    #endregion

    #region PE 签名

    private static void write_pe_signature(ref ByteBufferWriter writer)
    {
        writer.write_u32_le(0x00004550);
    }

    #endregion

    #region COFF 的

    private static void write_coff_header(ref ByteBufferWriter writer, PeHeaderData header)
    {
        writer.write_u16_le(header.machine);
        writer.write_u16_le(header.number_of_sections);
        writer.write_u32_le(header.time_date_stamp);
        writer.write_u32_le(header.pointer_to_symbol_table);
        writer.write_u32_le(header.number_of_symbols);
        writer.write_u16_le(header.size_of_optional_header);
        writer.write_u16_le(header.characteristics);
    }

    #endregion

    #region 可选头

    private static void write_optional_header(ref ByteBufferWriter writer, PeOptionalHeaderData opt, bool is64)
    {
        writer.write_u16_le(opt.magic);
        writer.write_u8(opt.major_linker_version);
        writer.write_u8(opt.minor_linker_version);
        writer.write_u32_le(opt.size_of_code);
        writer.write_u32_le(opt.size_of_initialized_data);
        writer.write_u32_le(opt.size_of_uninitialized_data);
        writer.write_u32_le(opt.address_of_entry_point);
        writer.write_u32_le(opt.base_of_code);

        if (!is64)
        {
            writer.write_u32_le(opt.base_of_data);
            writer.write_u32_le((uint)opt.image_base);
        }
        else
        {
            writer.write_u64_le(opt.image_base);
        }

        writer.write_u32_le(opt.section_alignment);
        writer.write_u32_le(opt.file_alignment);
        writer.write_u16_le(opt.major_operating_system_version);
        writer.write_u16_le(opt.minor_operating_system_version);
        writer.write_u16_le(opt.major_image_version);
        writer.write_u16_le(opt.minor_image_version);
        writer.write_u16_le(opt.major_subsystem_version);
        writer.write_u16_le(opt.minor_subsystem_version);
        writer.write_u32_le(opt.win32_version_value);
        writer.write_u32_le(opt.size_of_image);
        writer.write_u32_le(opt.size_of_headers);
        writer.write_u32_le(opt.check_sum);
        writer.write_u16_le(opt.subsystem);
        writer.write_u16_le(opt.dll_characteristics);

        if (is64)
        {
            writer.write_u64_le(opt.size_of_stack_reserve);
            writer.write_u64_le(opt.size_of_stack_commit);
            writer.write_u64_le(opt.size_of_heap_reserve);
            writer.write_u64_le(opt.size_of_heap_commit);
        }
        else
        {
            writer.write_u32_le((uint)opt.size_of_stack_reserve);
            writer.write_u32_le((uint)opt.size_of_stack_commit);
            writer.write_u32_le((uint)opt.size_of_heap_reserve);
            writer.write_u32_le((uint)opt.size_of_heap_commit);
        }

        writer.write_u32_le(opt.loader_flags);
        writer.write_u32_le(opt.number_of_rva_and_sizes);

        for (var i = 0; i < opt.number_of_rva_and_sizes; i++)
            if (i < opt.data_directories.Count)
            {
                writer.write_u32_le(opt.data_directories[i].rva);
                writer.write_u32_le(opt.data_directories[i].size);
            }
            else
            {
                writer.write_u32_le(0);
                writer.write_u32_le(0);
            }
    }

    #endregion

    #region 节区的

    private static void write_section_headers(ref ByteBufferWriter writer, IReadOnlyList<PeSectionData> sections)
    {
        foreach (var section in sections)
        {
            writer.write(section.name_bytes.as_span());
            writer.write_u32_le(section.virtual_size);
            writer.write_u32_le(section.virtual_address);
            writer.write_u32_le(section.size_of_raw_data);
            writer.write_u32_le(section.pointer_to_raw_data);
            writer.write_u32_le(section.pointer_to_relocations);
            writer.write_u32_le(section.pointer_to_linenumbers);
            writer.write_u16_le(section.number_of_relocations);
            writer.write_u16_le(section.number_of_linenumbers);
            writer.write_u32_le(section.characteristics);
        }
    }

    #endregion

    #region 节区内容

    private static void write_section_contents(ref ByteBufferWriter writer, PeFileData data)
    {
        for (var i = 0; i < data.sections.Count; i++)
        {
            if (!data.section_contents.TryGetValue(i, out var content)) continue;

            if (content.Length == 0) continue;

            var section = data.sections[i];
            var targetOffset = (int)section.pointer_to_raw_data;

            write_to_offset(ref writer, targetOffset);
            writer.write(content);
        }
    }

    #endregion

    #region 重定位表

    private static void write_relocation_table(ref ByteBufferWriter writer, PeFileData data)
    {
        if (data.relocations.Count == 0) return;

        var relocDir = data.get_data_directory(PeDirectoryDataIndex.base_relocation_table);

        if (relocDir.is_empty) return;

        var relocFileOffset = data.rva_to_offset(relocDir.rva);
        write_to_offset(ref writer, relocFileOffset);

        foreach (var block in data.relocations)
        {
            var blockSize = (uint)(PeConstants.relocation_block_header_size +
                                   block.entries.Count * PeConstants.relocation_entry_size);

            writer.write_u32_le(block.virtual_address);
            writer.write_u32_le(blockSize);

            foreach (var entry in block.entries)
            {
                var encoded = (ushort)(((uint)entry.type << 12) | ((uint)entry.offset & 0xFFF));
                writer.write_u16_le(encoded);
            }
        }
    }

    #endregion

    #region 辅助方法

    private static void write_to_offset(ref ByteBufferWriter writer, int targetOffset)
    {
        while (writer.position < targetOffset) writer.write_u8(0);
    }

    #endregion

    #region 导入的

    private static void write_import_table(ref ByteBufferWriter writer, PeFileData data)
    {
        if (data.imports.Count == 0) return;

        var is64 = data.is64_bit;
        var importDir = data.get_data_directory(PeDirectoryDataIndex.import_table);

        if (importDir.is_empty) return;

        var importFileOffset = data.rva_to_offset(importDir.rva);
        write_to_offset(ref writer, importFileOffset);

        var thunkSize = is64 ? PeConstants.import_thunk_size64 : PeConstants.import_thunk_size32;
        var ordinalFlag = is64 ? PeConstants.import_ordinal_flag64 : PeConstants.import_ordinal_flag32;

        var descriptorStartRva = importDir.rva;
        var descriptorEndRva =
            descriptorStartRva + (uint)((data.imports.Count + 1) * PeConstants.import_descriptor_size);
        var currentRva = descriptorEndRva;

        var dllLayouts = new List<(uint iltRva, uint iatRva, uint nameRva, List<uint> hintNameRvas)>();

        foreach (var dll in data.imports)
        {
            var iltRva = currentRva;
            var iltSize = (uint)((dll.thunks.Count + 1) * thunkSize);
            currentRva += iltSize;

            var iatRva = currentRva;
            currentRva += iltSize;

            var nameRva = currentRva;
            currentRva += (uint)(Encoding.ASCII.GetByteCount(dll.name) + 1);

            var hintNameRvas = new List<uint>();

            foreach (var thunk in dll.thunks)
                if (!thunk.is_ordinal)
                {
                    hintNameRvas.Add(currentRva);
                    currentRva += (uint)(2 + Encoding.ASCII.GetByteCount(thunk.name) + 1);
                }

            dllLayouts.Add((iltRva, iatRva, nameRva, hintNameRvas));
        }

        for (var d = 0; d < data.imports.Count; d++)
        {
            var layout = dllLayouts[d];
            writer.write_u32_le(layout.iltRva);
            writer.write_u32_le(0);
            writer.write_u32_le(0);
            writer.write_u32_le(layout.nameRva);
            writer.write_u32_le(layout.iatRva);
        }

        for (var i = 0; i < PeConstants.import_descriptor_size; i++) writer.write_u8(0);

        for (var d = 0; d < data.imports.Count; d++)
        {
            var dll = data.imports[d];
            var layout = dllLayouts[d];
            var hintIdx = 0;

            write_thunk_array(dll.thunks, layout.hintNameRvas, ref hintIdx, is64, ordinalFlag, ref writer);

            hintIdx = 0;

            write_thunk_array(dll.thunks, layout.hintNameRvas, ref hintIdx, is64, ordinalFlag, ref writer);

            hintIdx = 0;

            writer.write(Encoding.ASCII.GetBytes(dll.name));
            writer.write_u8(0);

            foreach (var thunk in dll.thunks)
                if (!thunk.is_ordinal)
                {
                    writer.write_u16_le(0);
                    writer.write(Encoding.ASCII.GetBytes(thunk.name));
                    writer.write_u8(0);
                }
        }
    }

    private static void write_thunk_array(IReadOnlyList<PeImportThunk> thunks, List<uint> hintNameRvas, ref int hintIdx,
        bool is64, ulong ordinalFlag, ref ByteBufferWriter writer)
    {
        foreach (var thunk in thunks)
            if (thunk.is_ordinal)
            {
                if (is64)
                    writer.write_u64_le(ordinalFlag | thunk.ordinal);
                else
                    writer.write_u32_le((uint)(ordinalFlag | thunk.ordinal));
            }
            else
            {
                var hintNameRva = hintNameRvas[hintIdx++];

                if (is64)
                    writer.write_u64_le(hintNameRva);
                else
                    writer.write_u32_le(hintNameRva);
            }

        if (is64)
            writer.write_u64_le(0);
        else
            writer.write_u32_le(0);
    }

    #endregion

    #region 大小预估

    private static int estimate_size(PeFileData data)
    {
        var is64 = data.is64_bit;
        var dosHeaderSize = 64;
        var peSignatureSize = 4;
        var coffHeaderSize = 20;
        var optionalHeaderSize = is64 ? 240 : 224;
        var dataDirSize = (int)data.optional_header.number_of_rva_and_sizes * 8;
        var sectionHeaderSize = data.sections.Count * 40;

        var sectionContentSize = 0;

        foreach (var content in data.section_contents.Values) sectionContentSize += content.Length;

        var importSize = estimate_import_size(data);
        var relocationSize = estimate_relocation_size(data);

        return dosHeaderSize + peSignatureSize + coffHeaderSize + optionalHeaderSize + dataDirSize + sectionHeaderSize +
               sectionContentSize + importSize + relocationSize + 4096;
    }

    private static int estimate_import_size(PeFileData data)
    {
        if (data.imports.Count == 0) return 0;

        var is64 = data.is64_bit;
        var thunkSize = is64 ? PeConstants.import_thunk_size64 : PeConstants.import_thunk_size32;
        var total = 0;

        foreach (var dll in data.imports)
        {
            total += (dll.thunks.Count + 1) * thunkSize * 2;
            total += dll.name.Length + 1;

            foreach (var thunk in dll.thunks)
                if (!thunk.is_ordinal)
                    total += 2 + thunk.name.Length + 1;
        }

        total += (data.imports.Count + 1) * PeConstants.import_descriptor_size;
        total += 256;

        return total;
    }

    private static int estimate_relocation_size(PeFileData data)
    {
        if (data.relocations.Count == 0) return 0;

        var total = 0;

        foreach (var block in data.relocations)
        {
            var blockSize = PeConstants.relocation_block_header_size +
                            block.entries.Count * PeConstants.relocation_entry_size;
            blockSize = (blockSize + 3) & ~3;
            total += blockSize;
        }

        total += 256;

        return total;
    }

    #endregion
}