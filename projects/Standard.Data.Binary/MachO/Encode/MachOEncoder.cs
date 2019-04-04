using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.MachO.Data;

namespace Std.Data.Binary.MachO.Encode;

/// <summary>
///     Mach-O 文件编码器，的MachOFileData 编码的Mach-O 二进制格式的
///     支持 32 的64 位双模式和小的大端双字节序的
///     支持节区内容编码的LC_SEGMENT/LC_SEGMENT_64 结构化编码的
/// </summary>
public sealed class MachOEncoder
{
    /// <summary>
    ///     编码 Mach-O 文件数据为字节数组的
    /// </summary>
    public byte[] encode(MachOFileData data)
    {
        var header = data.header;
        var is64 = header.is64_bit;
        var isLe = header.is_little_endian;

        var size = estimate_size(data);
        var writer = new ByteBufferWriter(size);

        write_mach_o_header(ref writer, header, is64, isLe);
        write_load_commands(ref writer, data, is64, isLe);
        write_section_contents(ref writer, data, isLe);

        return writer.to_array();
    }

    #region Mach-O 的

    private static void write_mach_o_header(ref ByteBufferWriter writer, MachOHeaderData header, bool is64, bool isLe)
    {
        write_u32(ref writer, header.magic, isLe);
        write_i32(ref writer, header.cpu_type, isLe);
        write_i32(ref writer, header.cpu_subtype, isLe);
        write_u32(ref writer, header.file_type, isLe);
        write_u32(ref writer, header.number_of_load_commands, isLe);
        write_u32(ref writer, header.size_of_load_commands, isLe);
        write_u32(ref writer, header.flags, isLe);

        if (is64) write_u32(ref writer, header.reserved, isLe);
    }

    #endregion

    #region 节区内容

    private static void write_section_contents(ref ByteBufferWriter writer, MachOFileData data, bool isLe)
    {
        foreach (var section in data.sections)
        {
            if (section.content.Length == 0) continue;

            var targetOffset = (int)section.offset;
            var currentPos = writer.position;

            if (currentPos < targetOffset) write_padding(ref writer, targetOffset - currentPos);

            writer.write(section.content);

            var alignment = 1 << (int)section.alignment;
            if (alignment > 1)
            {
                var aligned = (writer.position + alignment - 1) & ~(alignment - 1);
                if (aligned > writer.position) write_padding(ref writer, aligned - writer.position);
            }
        }
    }

    #endregion

    #region 大小预估

    private static int estimate_size(MachOFileData data)
    {
        var headerSize = data.header.is64_bit ? 32 : 28;

        var loadCommandsSize = 0;
        foreach (var cmd in data.load_commands) loadCommandsSize += (int)cmd.size;

        var sectionContentSize = 0;
        foreach (var section in data.sections)
        {
            sectionContentSize += section.content.Length;
            var alignment = 1 << (int)section.alignment;
            if (alignment > 1) sectionContentSize = (sectionContentSize + alignment - 1) & ~(alignment - 1);
        }

        return headerSize + loadCommandsSize + sectionContentSize + 4096;
    }

    #endregion

    #region 加载命令

    private static void write_load_commands(ref ByteBufferWriter writer, MachOFileData data, bool is64, bool isLe)
    {
        foreach (var cmd in data.load_commands)
        {
            write_u32(ref writer, cmd.command, isLe);
            write_u32(ref writer, cmd.size, isLe);

            if (cmd.command == MachOConstants.lc_segment64 && is64)
                write_segment64_command(ref writer, cmd, data.sections, isLe);
            else if (cmd.command == MachOConstants.lc_segment && !is64)
                write_segment_command(ref writer, cmd, data.sections, isLe);
            else
                writer.write(cmd.data);
        }
    }

    private static void write_segment64_command(ref ByteBufferWriter writer, MachOLoadCommandData cmd,
        IReadOnlyList<MachOSectionData> sections, bool isLe)
    {
        var segmentName = read_string(cmd.data, 0, 16);
        write_padded_name(ref writer, segmentName, 16);

        if (cmd.data.Length >= 16 + 48)
        {
            write_u64(ref writer, read_u64_be(cmd.data, 16), isLe);
            write_u64(ref writer, read_u64_be(cmd.data, 24), isLe);
            write_u64(ref writer, read_u64_be(cmd.data, 32), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 40), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 44), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 48), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 52), isLe);

            write_u32(ref writer, (uint)sections.Count, isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 60), isLe);

            for (var i = 0; i < sections.Count; i++) write_section64(ref writer, sections, i, isLe);
        }
        else
        {
            writer.write(cmd.data.AsSpan(16));
        }
    }

    private static void write_segment_command(ref ByteBufferWriter writer, MachOLoadCommandData cmd,
        IReadOnlyList<MachOSectionData> sections, bool isLe)
    {
        var segmentName = read_string(cmd.data, 0, 16);
        write_padded_name(ref writer, segmentName, 16);

        if (cmd.data.Length >= 16 + 32)
        {
            write_u32(ref writer, read_u32_be(cmd.data, 16), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 20), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 24), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 28), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 32), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 36), isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 40), isLe);

            write_u32(ref writer, (uint)sections.Count, isLe);
            write_u32(ref writer, read_u32_be(cmd.data, 48), isLe);

            for (var i = 0; i < sections.Count; i++) write_section32(ref writer, sections, i, isLe);
        }
        else
        {
            writer.write(cmd.data.AsSpan(16));
        }
    }

    private static void write_section64(ref ByteBufferWriter writer, IReadOnlyList<MachOSectionData> sections,
        int index,
        bool isLe)
    {
        if (index < sections.Count)
        {
            var section = sections[index];
            write_padded_name(ref writer, section.section_name, 16);
            write_padded_name(ref writer, section.segment_name, 16);
            write_u64(ref writer, section.address, isLe);
            write_u64(ref writer, section.size, isLe);
            write_u32(ref writer, section.offset, isLe);
            write_u32(ref writer, section.alignment, isLe);
            write_u32(ref writer, section.relocations_offset, isLe);
            write_u32(ref writer, section.number_of_relocations, isLe);
            write_u32(ref writer, section.flags, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 0, isLe);
        }
        else
        {
            write_padding(ref writer, 80);
        }
    }

    private static void write_section32(ref ByteBufferWriter writer, IReadOnlyList<MachOSectionData> sections,
        int index,
        bool isLe)
    {
        if (index < sections.Count)
        {
            var section = sections[index];
            write_padded_name(ref writer, section.section_name, 16);
            write_padded_name(ref writer, section.segment_name, 16);
            write_u32(ref writer, (uint)section.address, isLe);
            write_u32(ref writer, (uint)section.size, isLe);
            write_u32(ref writer, section.offset, isLe);
            write_u32(ref writer, section.alignment, isLe);
            write_u32(ref writer, section.relocations_offset, isLe);
            write_u32(ref writer, section.number_of_relocations, isLe);
            write_u32(ref writer, section.flags, isLe);
            write_u32(ref writer, 0, isLe);
            write_u32(ref writer, 0, isLe);
        }
        else
        {
            write_padding(ref writer, 68);
        }
    }

    #endregion

    #region 辅助方法

    private static void write_padded_name(ref ByteBufferWriter writer, string name, int totalLength)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var writeLength = System.Math.Min(nameBytes.Length, totalLength);
        writer.write(nameBytes.AsSpan(0, writeLength));
        for (var i = writeLength; i < totalLength; i++) writer.write_u8(0);
    }

    private static void write_padding(ref ByteBufferWriter writer, int count)
    {
        for (var i = 0; i < count; i++) writer.write_u8(0);
    }

    private static string read_string(byte[] data, int offset, int length)
    {
        var end = System.Math.Min(offset + length, data.Length);
        var span = data.AsSpan(offset, end - offset);
        var nullIndex = span.IndexOf((byte)0);
        if (nullIndex >= 0) span = span[..nullIndex];

        return Encoding.UTF8.GetString(span);
    }

    private static uint read_u32_be(byte[] data, int offset)
    {
        if (offset + 4 > data.Length) return 0;

        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    private static ulong read_u64_be(byte[] data, int offset)
    {
        if (offset + 8 > data.Length) return 0;

        return ((ulong)read_u32_be(data, offset) << 32) | read_u32_be(data, offset + 4);
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

    private static void write_i32(ref ByteBufferWriter writer, int value, bool isLe)
    {
        if (isLe)
            writer.write_i32_le(value);
        else
            writer.write_i32_be(value);
    }

    #endregion
}