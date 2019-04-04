using Std.Data.Binary.Dxil.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dxil.Encode;

/// <summary>
///     DXContainer 编码器，的C# 数据结构编码的DirectX DXContainer 二进制格式的
/// </summary>
/// <remarks>
///     DXContainer 的DirectX 着色器的容器格式，的"DXBC" 魔数开头，
///     包含多个 Part。编码器生成符合 Microsoft DXContainer 规范的二进制数据的
/// </remarks>
public sealed class DxContainerEncoder
{
    /// <summary>
    ///     将容器数据编码为 DXContainer 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     容器数据的/param>
    ///     <returns>DXContainer 二进制数据的/returns>
    public byte[] encode(DxContainerData data)
    {
        var partDataList = new List<byte[]>();

        foreach (var part in data.parts) partDataList.Add(encode_part(part));

        var headerSize = 20;
        var offsetTableSize = 4 * data.parts.Count;
        var partOffsets = new uint[data.parts.Count];

        var currentOffset = (uint)(headerSize + offsetTableSize);

        for (var i = 0; i < data.parts.Count; i++)
        {
            partOffsets[i] = currentOffset;
            currentOffset += (uint)partDataList[i].Length;
        }

        var totalSize = (int)currentOffset;
        var buffer = new byte[totalSize];
        var writer = new ByteBufferWriter(buffer);

        write_header(ref writer, data.header, (uint)totalSize, (uint)data.parts.Count);
        write_part_offsets(ref writer, partOffsets);

        for (var i = 0; i < partDataList.Count; i++) writer.write(partDataList[i]);

        return buffer[..writer.position];
    }

    /// <summary>
    ///     的Part 列表编码的DXContainer 二进制格式的
    /// </summary>
    /// <param name="parts">
    ///     Part 列表的/param>
    ///     <returns>DXContainer 二进制数据的/returns>
    public byte[] encode_parts(IReadOnlyList<DxContainerPart> parts)
    {
        var header = new DxContainerHeader
        {
            magic_number = DxilConstants.container_magic_number,
            version_major = DxilConstants.container_version_major,
            version_minor = DxilConstants.container_version_minor
        };
        var data = new DxContainerData
        {
            header = header,
            parts = parts
        };

        return encode(data);
    }

    /// <summary>
    ///     编码单个 Part 为二进制数据（Part 的+ Part 数据）的
    /// </summary>
    /// <param name="part">
    ///     Part 数据的/param>
    ///     <returns>编码后的 Part 二进制数据的/returns>
    public byte[] encode_part(DxContainerPart part)
    {
        var size = 8 + part.data.Length;
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.write_u32_le(part.header.four_cc);
        writer.write_u32_le(part.header.size);
        writer.write(part.data);

        return buffer[..writer.position];
    }

    private static void write_header(ref ByteBufferWriter writer, DxContainerHeader header,
        uint fileSize, uint partCount)
    {
        writer.write_u32_le(DxilConstants.container_magic_number);
        writer.write_u16_le(header.version_major);
        writer.write_u16_le(header.version_minor);
        writer.write_u32_le(fileSize);
        writer.write_u32_le(partCount);
    }

    private static void write_part_offsets(ref ByteBufferWriter writer, uint[] partOffsets)
    {
        foreach (var offset in partOffsets) writer.write_u32_le(offset);
    }
}