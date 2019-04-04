using Std.Data.Binary.Dxil.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dxil.Decode;

/// <summary>
///     DXContainer 解码器，解析 DirectX DXContainer 二进制格式的
/// </summary>
/// <remarks>
///     DXContainer 的DirectX 着色器的容器格式，的"DXBC" 魔数开头，
///     包含多个 Part（DXIL 着色器程序、特征标志、哈希、管线状态验证等）的
/// </remarks>
public sealed class DxContainerDecoder
{
    /// <summary>
    ///     从二进制数据解码 DXContainer的
    /// </summary>
    /// <param name="data">
    ///     DXContainer 二进制数据的/param>
    ///     <returns>
    ///         解码后的容器数据的/returns>
    ///         <exception cref="InvalidDataException">数据不是有效的DXContainer 格式的/exception>
    public DxContainerData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        return decode_container(ref buffer);
    }

    private DxContainerData decode_container(ref ByteBuffer buffer)
    {
        var header = read_header(ref buffer);
        var partOffsets = read_part_offsets(ref buffer, header.part_count);
        var parts = read_parts(ref buffer, partOffsets);

        return new DxContainerData
        {
            header = header,
            parts = parts
        };
    }

    private DxContainerHeader read_header(ref ByteBuffer buffer)
    {
        var magic = buffer.read_u32_le();

        if (magic != DxilConstants.container_magic_number)
            throw new InvalidDataException(
                $"不是有效的 DXContainer 文件（魔数不匹配：期望 0x{DxilConstants.container_magic_number:X8}，实际为 0x{magic:X8}）。");

        var versionMajor = buffer.read_u16_le();
        var versionMinor = buffer.read_u16_le();
        var fileSize = buffer.read_u32_le();
        var partCount = buffer.read_u32_le();

        var header = new DxContainerHeader
        {
            magic_number = magic,
            version_major = versionMajor,
            version_minor = versionMinor,
            file_size = fileSize,
            part_count = partCount
        };
        return header;
    }

    private static List<uint> read_part_offsets(ref ByteBuffer buffer, uint partCount)
    {
        var offsets = new List<uint>((int)partCount);

        for (var i = 0; i < partCount; i++) offsets.Add(buffer.read_u32_le());

        return offsets;
    }

    private static List<DxContainerPart> read_parts(ref ByteBuffer buffer, List<uint> partOffsets)
    {
        var parts = new List<DxContainerPart>(partOffsets.Count);

        foreach (var offset in partOffsets)
        {
            buffer.position = (int)offset;
            var part = read_part(ref buffer);
            parts.Add(part);
        }

        return parts;
    }

    private static DxContainerPart read_part(ref ByteBuffer buffer)
    {
        var fourCc = buffer.read_u32_le();
        var size = buffer.read_u32_le();

        var header = new DxContainerPartHeader
        {
            four_cc = fourCc,
            size = size
        };

        var data = buffer.read_bytes((int)size).ToArray();

        return new DxContainerPart
        {
            header = header,
            data = data
        };
    }
}