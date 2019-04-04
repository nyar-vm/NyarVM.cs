namespace Std.Data.Text.Ktx;

/// <summary>
///     KTX/KTX2 纹理格式解析器
/// </summary>
public sealed class KtxParser
{
    private static readonly byte[] _ktx1_magic =
        [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] _ktx2_magic =
        [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];


    /// <summary>
    ///     解析 KTX 数据
    /// </summary>
    public KtxParseResult parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12) throw new InvalidDataException("KTX 文件过小");

        var isKtx1 = data[..12].SequenceEqual(_ktx1_magic);
        var isKtx2 = data[..12].SequenceEqual(_ktx2_magic);

        if (!isKtx1 && !isKtx2) throw new InvalidDataException("KTX 魔数无效");

        return isKtx1 ? parse_ktx1(data) : parse_ktx2(data);
    }


    /// <summary>
    ///     判断是否为 KTX 文件
    /// </summary>
    public static bool is_ktx_file(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12) return false;

        return header[..12].SequenceEqual(_ktx1_magic) || header[..12].SequenceEqual(_ktx2_magic);
    }

    private static KtxParseResult parse_ktx1(ReadOnlySpan<byte> data)
    {
        if (data.Length < 64) throw new InvalidDataException("KTX1 文件头不完整");

        var endianness = BitConverter.ToUInt32(data[12..16]);
        if (endianness != 0x04030201) throw new InvalidDataException($"KTX1 字节序不支持：0x{endianness:X8}");

        var glType = BitConverter.ToUInt32(data[16..20]);
        var glTypeSize = BitConverter.ToUInt32(data[20..24]);
        var glFormat = BitConverter.ToUInt32(data[24..28]);
        var glInternalFormat = BitConverter.ToUInt32(data[28..32]);
        var glBaseInternalFormat = BitConverter.ToUInt32(data[32..36]);
        var pixelWidth = BitConverter.ToUInt32(data[36..40]);
        var pixelHeight = BitConverter.ToUInt32(data[40..44]);
        var pixelDepth = BitConverter.ToUInt32(data[44..48]);
        var numberOfArrayElements = BitConverter.ToUInt32(data[48..52]);
        var numberOfFaces = BitConverter.ToUInt32(data[52..56]);
        var numberOfMipmapLevels = BitConverter.ToUInt32(data[56..60]);
        var bytesOfKeyValueData = BitConverter.ToUInt32(data[60..64]);

        var width = (int)pixelWidth;
        var height = (int)pixelHeight;
        var depth = System.Math.Max(1, (int)pixelDepth);
        var arrayLayers = System.Math.Max(1, (int)numberOfArrayElements);
        var faces = System.Math.Max(1, (int)numberOfFaces);
        var mipLevels = System.Math.Max(1, (int)numberOfMipmapLevels);

        var textureFormat = map_gl_internal_format(glInternalFormat);
        var dimension = determine_dimension(pixelDepth, numberOfArrayElements, numberOfFaces);

        var offset = 64 + (int)bytesOfKeyValueData;
        var mipDataList = new List<byte[]>();

        for (var mip = 0; mip < mipLevels && offset < data.Length; mip++)
        {
            if (offset + 4 > data.Length) break;

            var imageSize = (int)BitConverter.ToUInt32(data[offset..(offset + 4)]);
            offset += 4;

            var mipWidth = System.Math.Max(1, width >> mip);
            var mipHeight = System.Math.Max(1, height >> mip);

            var totalFaceSize = 0;

            for (var face = 0; face < faces; face++)
            {
                if (offset + imageSize > data.Length) break;

                if (mip == 0 && face == 0)
                {
                    var faceData = data[offset..(offset + imageSize)].ToArray();
                    mipDataList.Add(faceData);
                }

                totalFaceSize += imageSize;
                offset += imageSize;

                var cubePadding = (3 - (imageSize + 3) % 4) % 4;
                offset += cubePadding;
            }

            var mipPadding = (3 - (totalFaceSize + 3) % 4) % 4;
            offset += mipPadding;
        }

        var rawData = mipDataList.Count > 0 ? mipDataList[0] : [];

        return new KtxParseResult
        {
            width = width,
            height = height,
            depth = depth,
            mip_levels = mipLevels,
            array_layers = arrayLayers,
            format = textureFormat,
            dimension = dimension,
            raw_data = rawData,
            mip_data = mipDataList,
            gl_internal_format = glInternalFormat
        };
    }

    private static KtxParseResult parse_ktx2(ReadOnlySpan<byte> data)
    {
        if (data.Length < 80) throw new InvalidDataException("KTX2 文件头不完整");

        var vkFormat = BitConverter.ToUInt32(data[12..16]);
        var typeSize = BitConverter.ToUInt32(data[16..20]);
        var pixelWidth = BitConverter.ToUInt32(data[20..24]);
        var pixelHeight = BitConverter.ToUInt32(data[24..28]);
        var pixelDepth = BitConverter.ToUInt32(data[28..32]);
        var layerCount = BitConverter.ToUInt32(data[32..36]);
        var faceCount = BitConverter.ToUInt32(data[36..40]);
        var levelCount = BitConverter.ToUInt32(data[40..44]);
        var supercompressionScheme = BitConverter.ToUInt32(data[44..48]);

        var dataFormatDescriptorOffset = BitConverter.ToUInt64(data[48..56]);
        var dataFormatDescriptorLength = BitConverter.ToUInt64(data[56..64]);
        var keyValueDataOffset = BitConverter.ToUInt64(data[64..72]);
        var keyValueDataLength = BitConverter.ToUInt64(data[72..80]);

        var width = (int)pixelWidth;
        var height = (int)pixelHeight;
        var depth = System.Math.Max(1, (int)pixelDepth);
        var arrayLayers = System.Math.Max(1, (int)layerCount);
        var faces = System.Math.Max(1, (int)faceCount);
        var mipLevels = System.Math.Max(1, (int)levelCount);

        var textureFormat = map_vk_format(vkFormat);
        var dimension = determine_dimension(pixelDepth, layerCount, faceCount);

        var levelOffset = 80;
        var mipDataList = new List<byte[]>();
        var rawData = Array.Empty<byte>();

        for (var level = 0; level < mipLevels && levelOffset + 24 <= data.Length; level++)
        {
            var byteOffset = BitConverter.ToUInt64(data[levelOffset..(levelOffset + 8)]);
            var byteLength = BitConverter.ToUInt64(data[(levelOffset + 8)..(levelOffset + 16)]);
            var uncompressedByteLength = BitConverter.ToUInt64(data[(levelOffset + 16)..(levelOffset + 24)]);

            if (byteOffset + byteLength <= (ulong)data.Length)
            {
                var levelData = data[(int)byteOffset..(int)(byteOffset + byteLength)].ToArray();

                if (level == 0) rawData = levelData;

                mipDataList.Add(levelData);
            }

            levelOffset += 24;
        }

        return new KtxParseResult
        {
            width = width,
            height = height,
            depth = depth,
            mip_levels = mipLevels,
            array_layers = arrayLayers,
            format = textureFormat,
            dimension = dimension,
            raw_data = rawData,
            mip_data = mipDataList,
            vk_format = vkFormat
        };
    }

    private static TextureFormat map_gl_internal_format(uint glFormat)
    {
        return glFormat switch
        {
            0x8D94 => TextureFormat.etc2_rgb,
            0x9278 => TextureFormat.etc2_rgba,
            0x93B0 => TextureFormat.astc4_x4,
            0x93B2 => TextureFormat.astc6_x6,
            0x93B4 => TextureFormat.astc8_x8,
            0x83F1 => TextureFormat.bc1_rgb_u_norm,
            0x83F2 => TextureFormat.bc1_rgba_u_norm,
            0x83F3 => TextureFormat.bc2_u_norm,
            0x83F4 => TextureFormat.bc3_u_norm,
            0x8DBE => TextureFormat.bc4_u_norm,
            0x8DBF => TextureFormat.bc5_u_norm,
            0x8E8F => TextureFormat.bc6_hu_float,
            0x8E8D => TextureFormat.bc7_u_norm,
            0x8058 => TextureFormat.r8_g8_b8_a8_u_norm,
            0x8D62 => TextureFormat.r8_g8_b8_a8_srgb,
            0x8C3A => TextureFormat.r16_g16_b16_a16_float,
            0x8814 => TextureFormat.r32_g32_b32_a32_float,
            0x8229 => TextureFormat.r8_u_norm,
            0x822D => TextureFormat.r16_float,
            0x822E => TextureFormat.r32_float,
            0x8C00 => TextureFormat.pvrtc_rgb4_bpp,
            0x8C01 => TextureFormat.pvrtc_rgba4_bpp,
            _ => TextureFormat.unknown
        };
    }

    private static TextureFormat map_vk_format(uint vkFormat)
    {
        return vkFormat switch
        {
            37 => TextureFormat.r8_g8_b8_a8_u_norm,
            38 => TextureFormat.r8_g8_b8_a8_srgb,
            43 => TextureFormat.b8_g8_r8_a8_u_norm,
            44 => TextureFormat.b8_g8_r8_a8_srgb,
            55 => TextureFormat.r16_g16_b16_a16_float,
            109 => TextureFormat.r32_g32_b32_a32_float,
            9 => TextureFormat.r8_u_norm,
            76 => TextureFormat.r16_float,
            98 => TextureFormat.r32_float,
            131 => TextureFormat.bc1_rgb_u_norm,
            132 => TextureFormat.bc1_rgba_u_norm,
            134 => TextureFormat.bc2_u_norm,
            136 => TextureFormat.bc3_u_norm,
            139 => TextureFormat.bc4_u_norm,
            141 => TextureFormat.bc5_u_norm,
            145 => TextureFormat.bc6_hu_float,
            147 => TextureFormat.bc7_u_norm,
            158 => TextureFormat.etc2_rgb,
            159 => TextureFormat.etc2_rgba,
            164 => TextureFormat.astc4_x4,
            166 => TextureFormat.astc6_x6,
            168 => TextureFormat.astc8_x8,
            180 => TextureFormat.pvrtc_rgb4_bpp,
            181 => TextureFormat.pvrtc_rgba4_bpp,
            _ => TextureFormat.unknown
        };
    }

    private static TextureDimension determine_dimension(uint depth, uint arrayLayers, uint faces)
    {
        if (faces == 6) return arrayLayers > 1 ? TextureDimension.texture_cube_array : TextureDimension.texture_cube;

        if (depth > 0) return TextureDimension.texture3_d;

        if (arrayLayers > 1) return TextureDimension.texture2_d_array;

        return TextureDimension.texture2_d;
    }
}