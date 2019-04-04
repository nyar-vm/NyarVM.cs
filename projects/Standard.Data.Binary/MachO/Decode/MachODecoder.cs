using System.Buffers.Binary;
using Std.Data.Binary.Frame;
using Std.Data.Binary.MachO.Data;

namespace Std.Data.Binary.MachO.Decode;

/// <summary>
///     Mach-O 文件解码器，解析 macOS/iOS 可执行文件（.macho, .dylib）格式的
/// </summary>
public sealed class MachODecoder
{
    /// <summary>
    ///     的Mach-O 二进制数据解码文件的
    /// </summary>
    /// <param name="data">
    ///     Mach-O 二进制数据的/param>
    ///     <returns>解码后的 Mach-O 文件数据的/returns>
    public MachOFileData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return decode_file(ref buffer);
    }

    private MachOFileData decode_file(ref ByteBuffer buffer)
    {
        var header = read_mach_o_header(ref buffer, out var isLittleEndian);
        var loadCommands = read_load_commands(ref buffer, header, isLittleEndian);
        var sections = read_sections(ref buffer, header, loadCommands, isLittleEndian);

        read_section_contents(ref buffer, sections);

        return new MachOFileData
        {
            header = header,
            load_commands = loadCommands,
            sections = sections
        };
    }

    /// <summary>
    ///     读取 Mach-O 头的
    /// </summary>
    private MachOHeaderData read_mach_o_header(ref ByteBuffer buffer, out bool isLittleEndian)
    {
        var rawMagic = buffer.read_u32_le();

        if (rawMagic is 0xFEEDFACE or 0xFEEDFACF)
            isLittleEndian = true;
        else if (rawMagic is 0xCEFAEDFE or 0xCFFAEDFE)
            isLittleEndian = false;
        else
            throw new InvalidDataException("Invalid Mach-O file: magic mismatch.");

        var magic = isLittleEndian ? rawMagic : BinaryPrimitives.ReverseEndianness(rawMagic);

        var cpuType = read_int32(ref buffer, isLittleEndian);
        var cpuSubtype = read_int32(ref buffer, isLittleEndian);
        var fileType = read_u_int32(ref buffer, isLittleEndian);
        var numberOfLoadCommands = read_u_int32(ref buffer, isLittleEndian);
        var sizeOfLoadCommands = read_u_int32(ref buffer, isLittleEndian);
        var flags = read_u_int32(ref buffer, isLittleEndian);

        uint reserved = 0;
        if (magic == 0xFEEDFACF) reserved = read_u_int32(ref buffer, isLittleEndian);

        return new MachOHeaderData
        {
            magic = magic,
            cpu_type = cpuType,
            cpu_subtype = cpuSubtype,
            file_type = fileType,
            number_of_load_commands = numberOfLoadCommands,
            size_of_load_commands = sizeOfLoadCommands,
            flags = flags,
            reserved = reserved,
            is_little_endian = isLittleEndian
        };
    }

    /// <summary>
    ///     读取加载命令的
    /// </summary>
    private List<MachOLoadCommandData> read_load_commands(ref ByteBuffer buffer, MachOHeaderData header,
        bool isLittleEndian)
    {
        var commands = new List<MachOLoadCommandData>();

        for (var i = 0; i < header.number_of_load_commands; i++)
        {
            var cmd = read_u_int32(ref buffer, isLittleEndian);
            var cmdSize = read_u_int32(ref buffer, isLittleEndian);

            var dataSize = (int)cmdSize - 8;
            var data = dataSize > 0 ? buffer.read_bytes(dataSize).ToArray() : [];

            commands.Add(new MachOLoadCommandData
            {
                command = cmd,
                size = cmdSize,
                data = data
            });
        }

        return commands;
    }

    /// <summary>
    ///     从二进制缓冲区读取节区定义的
    /// </summary>
    private static List<MachOSectionData> read_sections(ref ByteBuffer buffer, MachOHeaderData header,
        List<MachOLoadCommandData> loadCommands, bool isLittleEndian)
    {
        var sections = new List<MachOSectionData>();
        var headerSize = header.is64_bit ? 32 : 28;
        var cmdOffset = headerSize;

        foreach (var cmd in loadCommands)
        {
            if (cmd.command is MachOConstants.lc_segment or MachOConstants.lc_segment64)
            {
                var nsectsOffset = header.is64_bit
                    ? cmdOffset + 8 + 56
                    : cmdOffset + 8 + 40;

                buffer.position = nsectsOffset;
                var nsects = read_u_int32(ref buffer, isLittleEndian);

                var sectionsOffset = header.is64_bit
                    ? cmdOffset + 8 + 64
                    : cmdOffset + 8 + 48;

                buffer.position = sectionsOffset;

                for (var i = 0; i < nsects; i++)
                {
                    var sectionName = read_string(ref buffer, 16);
                    var segmentName = read_string(ref buffer, 16);

                    ulong address;
                    ulong size;
                    uint offset;
                    uint alignment;
                    uint relocationsOffset;
                    uint numberOfRelocations;
                    uint flags;

                    if (header.is64_bit)
                    {
                        address = read_u_int64(ref buffer, isLittleEndian);
                        size = read_u_int64(ref buffer, isLittleEndian);
                        offset = read_u_int32(ref buffer, isLittleEndian);
                        alignment = read_u_int32(ref buffer, isLittleEndian);
                        relocationsOffset = read_u_int32(ref buffer, isLittleEndian);
                        numberOfRelocations = read_u_int32(ref buffer, isLittleEndian);
                        flags = read_u_int32(ref buffer, isLittleEndian);
                        buffer.advance(4);
                        buffer.advance(4);
                    }
                    else
                    {
                        address = read_u_int32(ref buffer, isLittleEndian);
                        size = read_u_int32(ref buffer, isLittleEndian);
                        offset = read_u_int32(ref buffer, isLittleEndian);
                        alignment = read_u_int32(ref buffer, isLittleEndian);
                        relocationsOffset = read_u_int32(ref buffer, isLittleEndian);
                        numberOfRelocations = read_u_int32(ref buffer, isLittleEndian);
                        flags = read_u_int32(ref buffer, isLittleEndian);
                        buffer.advance(4);
                        buffer.advance(4);
                    }

                    sections.Add(new MachOSectionData
                    {
                        section_name = sectionName,
                        segment_name = segmentName,
                        address = address,
                        size = size,
                        offset = offset,
                        alignment = alignment,
                        relocations_offset = relocationsOffset,
                        number_of_relocations = numberOfRelocations,
                        flags = flags
                    });
                }
            }

            cmdOffset += (int)cmd.size;
        }

        return sections;
    }

    /// <summary>
    ///     读取节区原始内容数据的
    /// </summary>
    private static void read_section_contents(ref ByteBuffer buffer, List<MachOSectionData> sections)
    {
        foreach (var section in sections)
        {
            if (section.offset == 0 || section.size == 0) continue;

            var offset = (int)section.offset;
            var size = (int)section.size;

            if (offset + size > buffer.length) continue;

            buffer.position = offset;
            section.content = [.. buffer.read_bytes(size)];
        }
    }

    /// <summary>
    ///     读取固定长度字符串的
    /// </summary>
    private static string read_string(ref ByteBuffer buffer, int length)
    {
        return buffer.read_string(length).TrimEnd('\0');
    }

    /// <summary>
    ///     读取 Int32，根据字节序转换的
    /// </summary>
    private static int read_int32(ref ByteBuffer buffer, bool isLittleEndian)
    {
        return isLittleEndian ? buffer.read_i32_le() : buffer.read_i32_be();
    }

    /// <summary>
    ///     读取 UInt32，根据字节序转换的
    /// </summary>
    private static uint read_u_int32(ref ByteBuffer buffer, bool isLittleEndian)
    {
        return isLittleEndian ? buffer.read_u32_le() : buffer.read_u32_be();
    }

    /// <summary>
    ///     读取 UInt64，根据字节序转换的
    /// </summary>
    private static ulong read_u_int64(ref ByteBuffer buffer, bool isLittleEndian)
    {
        return isLittleEndian ? buffer.read_u64_le() : buffer.read_u64_be();
    }
}