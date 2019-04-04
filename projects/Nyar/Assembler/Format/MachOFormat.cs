using System.Buffers.Binary;
using System.Text;
using Nyar.Types.Targets;
using Std.Data.Binary.MachO.Data;
using Std.Data.Binary.MachO.Encode;

namespace Nyar.Assembler.Format;

/// <summary>
///     Mach-O 可执行格式构建器，使用 Nyar.Binary.MachO 数据模型和编码器。
///     支持 macOS 平台的 Mach-O 可执行文件输出。
/// </summary>
public sealed class MachOFormat : IExecutableFormat
{
    /// <inheritdoc />
    public string name => "Mach-O";

    /// <inheritdoc />
    public byte[] build_and_encode(NativeBuildContext context)
    {
        var is64 = context.target.is64_bit;
        var cpuType = map_mach_o_cpu_type(context.arch);

        var headerSize = is64 ? 32 : 28;
        var segmentCommandSize = is64 ? 72 + 80 * 2 : 56 + 68 * 2;
        var loadCommandSize = segmentCommandSize;

        var textOffset = headerSize + loadCommandSize;
        var dataOffset = textOffset + context.text_bytes.Length;
        dataOffset = (dataOffset + 15) & ~15;

        var segmentData = new byte[segmentCommandSize - 8];
        var segNameBytes = Encoding.UTF8.GetBytes("__TEXT\0\0\0\0\0\0\0\0\0\0");
        segNameBytes.CopyTo(segmentData, 0);

        if (is64)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(segmentData.AsSpan(16), 0);
            BinaryPrimitives.WriteUInt64LittleEndian(segmentData.AsSpan(24), (ulong)textOffset);
            BinaryPrimitives.WriteUInt64LittleEndian(segmentData.AsSpan(32),
                (ulong)(context.text_bytes.Length + context.data_bytes.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(40), 7);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(44), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(48), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(52), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(56), 2);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(60), 0);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(16), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(20), (uint)textOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(24),
                (uint)(context.text_bytes.Length + context.data_bytes.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(28), 7);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(32), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(36), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(40), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(44), 2);
            BinaryPrimitives.WriteUInt32LittleEndian(segmentData.AsSpan(48), 0);
        }

        var machOData = new MachOFileData
        {
            header = new MachOHeaderData
            {
                magic = is64 ? 0xFEEDFACF : 0xFEEDFACE,
                cpu_type = cpuType,
                cpu_subtype = 3,
                file_type = 2,
                number_of_load_commands = 1,
                size_of_load_commands = (uint)loadCommandSize,
                flags = 0,
                reserved = 0,
                is_little_endian = true
            },
            load_commands =
            [
                new MachOLoadCommandData
                {
                    command = is64 ? 0x19u : 0x1u,
                    size = (uint)segmentCommandSize,
                    data = segmentData
                }
            ],
            sections =
            [
                new MachOSectionData
                {
                    section_name = "__text",
                    segment_name = "__TEXT",
                    address = 0,
                    size = (ulong)context.text_bytes.Length,
                    offset = (uint)textOffset,
                    alignment = 4,
                    relocations_offset = 0,
                    number_of_relocations = 0,
                    flags = 0x80000400,
                    content = context.text_bytes
                },
                new MachOSectionData
                {
                    section_name = "__data",
                    segment_name = "__DATA",
                    address = (ulong)context.text_bytes.Length,
                    size = (ulong)context.data_bytes.Length,
                    offset = (uint)dataOffset,
                    alignment = 3,
                    relocations_offset = 0,
                    number_of_relocations = 0,
                    flags = 0,
                    content = context.data_bytes
                }
            ]
        };

        var encoder = new MachOEncoder();
        return encoder.encode(machOData);
    }

    /// <summary>
    ///     将架构映射为 Mach-O CPU 类型
    /// </summary>
    private static int map_mach_o_cpu_type(TargetArch arch)
    {
        return arch switch
        {
            TargetArch.x86 => 7,
            TargetArch.x86_64 => 0x01000007,
            TargetArch.arm => 12,
            TargetArch.a_arch64 => 0x0100000C,
            _ => 0x01000007
        };
    }
}