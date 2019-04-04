using System.Buffers.Binary;
using Std.Data.Binary.Basis.Data;

namespace Std.Data.Binary.Basis.Encode;

/// <summary>
///     Basis/KTX2 编码器，的<see cref="BasisFileData" /> 编码的KTX2 二进制格式的
/// </summary>
/// <remarks>
///     KTX2 的Khronos 纹理容器格式，Basis Universal 超级压缩使用此容器的
///     编码器生的KTX2 格式（魔的«KTX 20»），不含实际压缩数据的
///     Basis 原生格式（BSS1）暂不支持编码的
/// </remarks>
public sealed class BasisEncoder
{
    /// <summary>
    ///     KTX2 头大小（含魔数）的
    /// </summary>
    private const int _ktx2_header_size = 80;

    /// <summary>
    ///     的Basis 文件数据编码的KTX2 二进制的
    /// </summary>
    /// <param name="data">
    ///     Basis 文件数据的/param>
    ///     <param name="format">
    ///         Vulkan 格式（默的VK_FORMAT_UNDEFINED）的/param>
    ///         <returns>KTX2 二进制数据的/returns>
    public byte[] encode(BasisFileData data, uint format = 0)
    {
        var buffer = new byte[_ktx2_header_size];
        var pos = 0;

        // 魔数
        BasisConstants.ktx2_magic.CopyTo(buffer.AsSpan(pos));
        pos += 12;

        // KTX2 头部字段（共 17 个字的= 68 字节的        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), format); // vkFormat
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // typeSize
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.width); // pixelWidth
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.height); // pixelHeight
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // pixelDepth
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos),
            (uint)System.Math.Max(1, data.image_count)); // layerCount
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // faceCount
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos),
            (uint)System.Math.Max(1, data.mip_levels)); // levelCount
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // supercompressionScheme (1 = BasisLZ)
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), _ktx2_header_size); // dfdByteOffset
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 0); // dfdByteLength
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), _ktx2_header_size); // kvDataByteOffset
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 0); // kvDataByteLength
        pos += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), _ktx2_header_size); // sgdByteOffset
        pos += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0); // sgdByteLength

        return buffer;
    }

    /// <summary>
    ///     编码包含实际 DFD 数据的完的KTX2的
    /// </summary>
    /// <param name="data">
    ///     Basis 文件数据的/param>
    ///     <param name="dfdData">
    ///         DFD 数据块的/param>
    ///         <param name="levelData">
    ///             的mipmap 级别的数据的/param>
    ///             <param name="format">
    ///                 Vulkan 格式的/param>
    ///                 <returns>KTX2 二进制数据的/returns>
    public byte[] encode_with_data(BasisFileData data, byte[]? dfdData, byte[]?[]? levelData, uint format = 0)
    {
        dfdData ??= [];
        levelData ??= [];

        var dfdOffset = 80L;
        var kvOffset = dfdOffset + dfdData.Length;
        var levelCount = System.Math.Max(1, data.mip_levels);

        // 计算层级索引区域大小
        var levelsOffset = kvOffset;
        var levelIndexSize = levelCount * 24; // 每级 24 字节（byteOffset:8 + byteLength:8 + uncompressedByteLength:8的
        // 写入头部
        var totalSize = levelsOffset + levelIndexSize;

        foreach (var lvl in levelData)
            if (lvl != null)
                totalSize += lvl.Length;

        var buffer = new byte[totalSize];
        var pos = 0;

        // 魔数
        BasisConstants.ktx2_magic.CopyTo(buffer.AsSpan(pos));
        pos += 12;

        // 头部
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), format);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.width);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.height);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)System.Math.Max(1, data.image_count));
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)levelCount);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)dfdOffset);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)dfdData.Length);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)kvOffset);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 0);
        pos += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)kvOffset);
        pos += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0);
        pos += 8;

        // DFD 数据
        if (dfdData.Length > 0)
        {
            dfdData.CopyTo(buffer.AsSpan(pos));
            pos += dfdData.Length;
        }

        // Level 索引
        var dataCursor = pos + levelIndexSize;

        for (var i = 0; i < levelCount; i++)
        {
            var lvlBytes = i < levelData.Length ? levelData[i] : null;

            if (lvlBytes is { Length: > 0 })
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)dataCursor);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)lvlBytes.Length);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)lvlBytes.Length);
                pos += 8;
                lvlBytes.CopyTo(buffer.AsSpan(dataCursor));
                dataCursor += lvlBytes.Length;
            }
            else
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)dataCursor);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0);
                pos += 8;
            }
        }

        return buffer;
    }
}