using System.Text;
using Std.Data.Binary.Elf.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Elf.Encode;

/// <summary>
///     ELF 文件编码器，的ELFFileData 编码的ELF 二进制格式的
///     支持 32 的64 位双模式和小的大端双字节序的
///     支持节区内容编码和字符串表自动构建的
/// </summary>
public sealed class ElfEncoder
{
    /// <summary>
    ///     编码 ELF 文件数据为字节数的
    /// </summary>
    public byte[] encode(ElfFileData data)
    {
        var header = data.header;
        var is64 = header.is64_bit;
        var isLe = header.is_little_endian;

        var layout = compute_layout(data, is64);

        var size = layout.total_size;
        var writer = new ByteBufferWriter(size);

        write_elf_header(ref writer, header, is64, isLe, layout);

        write_program_headers(ref writer, data.program_headers, is64, isLe);

        foreach (var section in data.section_headers)
            if (section.content.Length > 0 && layout.section_offsets.TryGetValue(section.name, out var offset))
            {
                write_to_offset(ref writer, offset);
                writer.write(section.content);
            }

        if (data.symbol_table != null)
        {
            write_to_offset(ref writer, layout.symbol_table_offset);
            write_symbol_table(ref writer, data.symbol_table.symbols, is64, isLe);

            write_to_offset(ref writer, layout.symbol_string_table_offset);
            write_symbol_string_table(ref writer, data.symbol_table.symbols);
        }

        if (layout.string_table_offset > 0)
        {
            var currentPos = writer.position;
            if (currentPos < layout.string_table_offset)
                write_padding(ref writer, layout.string_table_offset - currentPos);

            write_string_table(ref writer, layout.string_table);
        }

        write_to_offset(ref writer, layout.section_headers_offset);
        write_section_headers(ref writer, data.section_headers, is64, isLe, layout);

        return writer.to_array();
    }

    #region ELF 的

    private static void write_elf_header(ref ByteBufferWriter writer, ElfHeaderData header, bool is64, bool isLe,
        ElfLayout layout)
    {
        writer.write(header.magic);

        writer.write_u8(header.@class);
        writer.write_u8(header.data_encoding);
        writer.write_u8(header.version);
        writer.write_u8(header.osabi);
        writer.write_u8(header.abi_version);

        for (var i = 0; i < 7; i++) writer.write_u8(0);

        write_u16(ref writer, header.type, isLe);
        write_u16(ref writer, header.machine, isLe);
        write_u32(ref writer, header.object_version, isLe);

        if (is64)
        {
            write_u64(ref writer, header.entry_point, isLe);
            write_u64(ref writer, header.program_header_offset, isLe);
            write_u64(ref writer, (ulong)layout.section_headers_offset, isLe);
        }
        else
        {
            write_u32(ref writer, (uint)header.entry_point, isLe);
            write_u32(ref writer, (uint)header.program_header_offset, isLe);
            write_u32(ref writer, (uint)layout.section_headers_offset, isLe);
        }

        write_u32(ref writer, header.flags, isLe);
        write_u16(ref writer, header.elf_header_size, isLe);
        write_u16(ref writer, header.program_header_size, isLe);
        write_u16(ref writer, header.program_header_count, isLe);
        write_u16(ref writer, header.section_header_size, isLe);
        write_u16(ref writer, (ushort)(header.section_header_count + layout.auto_generated_section_header_count), isLe);
        write_u16(ref writer, header.string_table_index, isLe);
    }

    #endregion

    #region 字符串表

    private static void write_string_table(ref ByteBufferWriter writer, List<string> strings)
    {
        foreach (var s in strings)
            if (s == "\0")
                writer.write_u8(0);
            else
                writer.write_null_terminated_string(s.TrimEnd('\0'));
    }

    #endregion

    #region 布局计算

    /// <summary>
    ///     ELF 文件布局信息
    /// </summary>
    private sealed class ElfLayout
    {
        public readonly Dictionary<string, int> section_offsets = [];
        public int auto_generated_section_header_count;
        public int local_symbol_count;
        public int section_headers_offset;
        public List<string> string_table = [];
        public int string_table_offset;
        public int strtab_name_offset;
        public int strtab_section_index;
        public int symbol_string_table_offset;
        public int symbol_string_table_size;
        public int symbol_table_offset;
        public int symbol_table_size;
        public int symtab_name_offset;
        public int symtab_section_index;
        public int total_size;
    }

    private static ElfLayout compute_layout(ElfFileData data, bool is64)
    {
        var layout = new ElfLayout();

        var headerSize = is64 ? 64 : 52;
        var phSize = is64 ? 56 : 32;
        var shSize = is64 ? 64 : 40;

        var currentOffset = headerSize;
        currentOffset += data.program_headers.Count * phSize;

        layout.string_table = build_string_table(data.section_headers);

        var autoGeneratedCount = 0;
        if (data.symbol_table != null)
        {
            autoGeneratedCount = 2;

            var nameOffset = layout.string_table.Sum(s => s.Length);
            layout.symtab_name_offset = nameOffset;
            layout.string_table.Add(".symtab\0");
            nameOffset += ".symtab\0".Length;
            layout.strtab_name_offset = nameOffset;
            layout.string_table.Add(".strtab\0");

            layout.local_symbol_count = data.symbol_table.symbols.Count(s => s.bind == ElfConstants.symbol_bind_local);
        }

        layout.auto_generated_section_header_count = autoGeneratedCount;

        foreach (var section in data.section_headers)
            if (section.content.Length > 0)
            {
                var alignment = System.Math.Max((int)section.alignment, 1);
                currentOffset = align_up(currentOffset, alignment);
                layout.section_offsets[section.name] = currentOffset;
                currentOffset += section.content.Length;
            }

        if (data.symbol_table != null)
        {
            currentOffset = align_up(currentOffset, 8);
            layout.symbol_table_offset = currentOffset;
            var entrySize = is64 ? ElfConstants.symbol_entry_size64 : ElfConstants.symbol_entry_size32;
            layout.symbol_table_size = data.symbol_table.symbols.Count * entrySize;
            currentOffset += layout.symbol_table_size;

            var strtabLength = 1;
            foreach (var sym in data.symbol_table.symbols)
                if (!string.IsNullOrEmpty(sym.name))
                    strtabLength += Encoding.UTF8.GetByteCount(sym.name) + 1;

            layout.symbol_string_table_size = strtabLength;

            currentOffset = align_up(currentOffset, 8);
            layout.symbol_string_table_offset = currentOffset;
            currentOffset += layout.symbol_string_table_size;
        }

        layout.string_table_offset = currentOffset;
        layout.string_table.Add("\0");
        currentOffset += layout.string_table.Sum(s => s.Length);

        var totalSectionCount = data.section_headers.Count + autoGeneratedCount;
        layout.symtab_section_index = data.section_headers.Count;
        layout.strtab_section_index = data.section_headers.Count + 1;

        currentOffset = align_up(currentOffset, 8);
        layout.section_headers_offset = currentOffset;
        currentOffset += totalSectionCount * shSize;

        layout.total_size = currentOffset;

        return layout;
    }

    private static List<string> build_string_table(IReadOnlyList<ElfSectionHeaderData> sections)
    {
        var strings = new List<string> { "\0" };

        foreach (var section in sections)
            if (!string.IsNullOrEmpty(section.name))
                strings.Add(section.name + "\0");

        return strings;
    }

    #endregion

    #region 程序的

    private static void write_program_headers(ref ByteBufferWriter writer, IReadOnlyList<ElfProgramHeaderData> headers,
        bool is64, bool isLe)
    {
        foreach (var ph in headers) write_program_header(ref writer, ph, is64, isLe);
    }

    private static void write_program_header(ref ByteBufferWriter writer, ElfProgramHeaderData ph, bool is64, bool isLe)
    {
        write_u32(ref writer, ph.type, isLe);

        if (is64)
        {
            write_u32(ref writer, ph.flags, isLe);
            write_u64(ref writer, ph.offset, isLe);
            write_u64(ref writer, ph.virtual_address, isLe);
            write_u64(ref writer, ph.physical_address, isLe);
            write_u64(ref writer, ph.file_size, isLe);
            write_u64(ref writer, ph.memory_size, isLe);
            write_u64(ref writer, ph.alignment, isLe);
        }
        else
        {
            write_u32(ref writer, (uint)ph.offset, isLe);
            write_u32(ref writer, (uint)ph.virtual_address, isLe);
            write_u32(ref writer, (uint)ph.physical_address, isLe);
            write_u32(ref writer, (uint)ph.file_size, isLe);
            write_u32(ref writer, (uint)ph.memory_size, isLe);
            write_u32(ref writer, ph.flags, isLe);
            write_u32(ref writer, (uint)ph.alignment, isLe);
        }
    }

    #endregion

    #region 节区的

    private static void write_section_headers(ref ByteBufferWriter writer, IReadOnlyList<ElfSectionHeaderData> headers,
        bool is64, bool isLe, ElfLayout layout)
    {
        var nameOffset = 1;

        foreach (var sh in headers)
        {
            var nameIdx = sh.name_index;
            if (nameIdx == 0 && !string.IsNullOrEmpty(sh.name))
            {
                nameIdx = (uint)nameOffset;
                nameOffset += sh.name.Length + 1;
            }

            write_section_header(ref writer, sh, nameIdx, is64, isLe, layout);
        }

        if (layout.auto_generated_section_header_count > 0)
        {
            write_symtab_section_header(ref writer, is64, isLe, layout);
            write_strtab_section_header(ref writer, is64, isLe, layout);
        }
    }

    private static void write_section_header(ref ByteBufferWriter writer, ElfSectionHeaderData sh, uint nameIndex,
        bool is64, bool isLe, ElfLayout layout)
    {
        write_u32(ref writer, nameIndex, isLe);
        write_u32(ref writer, sh.type, isLe);

        var offset = sh.offset;
        var size = sh.size;
        if (sh.content.Length > 0 && layout.section_offsets.TryGetValue(sh.name, out var computedOffset))
        {
            offset = (ulong)computedOffset;
            size = (ulong)sh.content.Length;
        }

        if (is64)
        {
            write_u64(ref writer, sh.flags, isLe);
            write_u64(ref writer, sh.address, isLe);
            write_u64(ref writer, offset, isLe);
            write_u64(ref writer, size, isLe);
            write_u32(ref writer, sh.link, isLe);
            write_u32(ref writer, sh.info, isLe);
            write_u64(ref writer, sh.alignment, isLe);
            write_u64(ref writer, sh.entry_size, isLe);
        }
        else
        {
            write_u32(ref writer, (uint)sh.flags, isLe);
            write_u32(ref writer, (uint)sh.address, isLe);
            write_u32(ref writer, (uint)offset, isLe);
            write_u32(ref writer, (uint)size, isLe);
            write_u32(ref writer, sh.link, isLe);
            write_u32(ref writer, sh.info, isLe);
            write_u32(ref writer, (uint)sh.alignment, isLe);
            write_u32(ref writer, (uint)sh.entry_size, isLe);
        }
    }

    private static void write_symtab_section_header(ref ByteBufferWriter writer, bool is64, bool isLe, ElfLayout layout)
    {
        write_u32(ref writer, (uint)layout.symtab_name_offset, isLe);
        write_u32(ref writer, 2, isLe);

        if (is64)
        {
            write_u64(ref writer, 0, isLe);
            write_u64(ref writer, 0, isLe);
            write_u64(ref writer, (ulong)layout.symbol_table_offset, isLe);
            write_u64(ref writer, (ulong)layout.symbol_table_size, isLe);
            write_u32(ref writer, (uint)layout.strtab_section_index, isLe);
            write_u32(ref writer, (uint)(1 + layout.local_symbol_count), isLe);
            write_u64(ref writer, 8, isLe);
            write_u64(ref writer, (ulong)(is64 ? ElfConstants.symbol_entry_size64 : ElfConstants.symbol_entry_size32),
                isLe);
        }
        else
        {
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, (uint)layout.symbol_table_offset, isLe);
            write_u32(ref writer, (uint)layout.symbol_table_size, isLe);
            write_u32(ref writer, (uint)layout.strtab_section_index, isLe);
            write_u32(ref writer, (uint)(1 + layout.local_symbol_count), isLe);
            write_u32(ref writer, 4, isLe);
            write_u32(ref writer, (uint)(is64 ? ElfConstants.symbol_entry_size64 : ElfConstants.symbol_entry_size32),
                isLe);
        }
    }

    private static void write_strtab_section_header(ref ByteBufferWriter writer, bool is64, bool isLe, ElfLayout layout)
    {
        write_u32(ref writer, (uint)layout.strtab_name_offset, isLe);
        write_u32(ref writer, 3, isLe);

        if (is64)
        {
            write_u64(ref writer, 0, isLe);
            write_u64(ref writer, 0, isLe);
            write_u64(ref writer, (ulong)layout.symbol_string_table_offset, isLe);
            write_u64(ref writer, (ulong)layout.symbol_string_table_size, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 0, isLe);
            write_u64(ref writer, 1, isLe);
            write_u64(ref writer, 0, isLe);
        }
        else
        {
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, (uint)layout.symbol_string_table_offset, isLe);
            write_u32(ref writer, (uint)layout.symbol_string_table_size, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 1, isLe);
            write_u32(ref writer, 0, isLe);
        }
    }

    #endregion

    #region 符号的

    /// <summary>
    ///     写入符号表内容（.symtab）的
    /// </summary>
    private static void write_symbol_table(ref ByteBufferWriter writer, IReadOnlyList<ElfSymbolData> symbols, bool is64,
        bool isLe)
    {
        var nameOffsets = build_symbol_name_offsets(symbols);

        foreach (var sym in symbols)
        {
            var nameIdx = sym.name_index;
            if (nameIdx == 0 && !string.IsNullOrEmpty(sym.name) && nameOffsets.TryGetValue(sym.name, out var offset))
                nameIdx = offset;

            write_u32(ref writer, nameIdx, isLe);

            if (is64)
            {
                writer.write_u8(sym.info);
                writer.write_u8(sym.other);
                write_u16(ref writer, sym.section_index, isLe);
                write_u64(ref writer, sym.value, isLe);
                write_u64(ref writer, sym.size, isLe);
            }
            else
            {
                write_u32(ref writer, (uint)sym.value, isLe);
                write_u32(ref writer, (uint)sym.size, isLe);
                writer.write_u8(sym.info);
                writer.write_u8(sym.other);
                write_u16(ref writer, sym.section_index, isLe);
            }
        }
    }

    /// <summary>
    ///     写入符号字符串表内容的strtab）的
    /// </summary>
    private static void write_symbol_string_table(ref ByteBufferWriter writer, IReadOnlyList<ElfSymbolData> symbols)
    {
        writer.write_u8(0);

        foreach (var sym in symbols)
            if (!string.IsNullOrEmpty(sym.name))
                writer.write_null_terminated_string(sym.name);
    }

    /// <summary>
    ///     构建符号名称到字符串表偏移的映射的
    /// </summary>
    private static Dictionary<string, uint> build_symbol_name_offsets(IReadOnlyList<ElfSymbolData> symbols)
    {
        var result = new Dictionary<string, uint>();
        uint currentOffset = 1;

        foreach (var sym in symbols)
            if (!string.IsNullOrEmpty(sym.name) && result.TryAdd(sym.name, currentOffset))
                currentOffset += (uint)Encoding.UTF8.GetByteCount(sym.name) + 1;

        return result;
    }

    #endregion

    #region 辅助方法

    private static void write_padding(ref ByteBufferWriter writer, int count)
    {
        for (var i = 0; i < count; i++) writer.write_u8(0);
    }

    /// <summary>
    ///     将写入位置推进到指定偏移，不足部分用零填充的
    /// </summary>
    private static void write_to_offset(ref ByteBufferWriter writer, int targetOffset)
    {
        var currentPos = writer.position;
        if (currentPos < targetOffset) write_padding(ref writer, targetOffset - currentPos);
    }

    private static int align_up(int offset, int alignment)
    {
        return (offset + alignment - 1) & ~(alignment - 1);
    }

    private static void write_u16(ref ByteBufferWriter writer, ushort value, bool isLe)
    {
        if (isLe)
            writer.write_u16_le(value);
        else
            writer.write_u16_be(value);
    }

    private static void write_u32(ref ByteBufferWriter writer, uint value, bool isLe)
    {
        if (isLe)
            writer.write_u32_le(value);
        else
            writer.write_u32_be(value);
    }

    private static void write_u64(ref ByteBufferWriter writer, ulong value, bool isLe)
    {
        if (isLe)
            writer.write_u64_le(value);
        else
            writer.write_u64_be(value);
    }

    #endregion
}