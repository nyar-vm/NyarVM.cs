using Std.Data.Binary.Dds.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dds.Decode;

/// <summary>
///     DDS 文件解码器，的DirectDraw Surface 纹理格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     DDS 的Microsoft DirectDraw 的纹理容器格式，广泛用于 PC 游戏和图形应用的
///     支持 BC1-BC7 块压缩、立方体贴图、体积纹理和 Mipmap 链的
/// </remarks>
public ref struct DdsDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="DdsDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">DDS 二进制数据的/param>
    public DdsDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 DDS 文件的
    /// </summary>
    /// <returns>DDS 纹理数据的/returns>
    public DdsTextureData decode()
    {
        var magic = _buffer.read_string(4);

        if (magic != "DDS ") throw new InvalidDataException($"DDS 文件签名无效，期的\"DDS \"，实的\"{magic}\"");

        var header = read_header();
        var dx10Header = try_read_dx10_header(header);
        var surfaces = read_surfaces(header, dx10Header);

        return new DdsTextureData
        {
            height = header.height,
            width = header.width,
            depth = header.depth,
            mip_map_count = header.mip_map_count,
            pixel_format = header.pixel_format,
            dimension = dx10Header?.dimension ?? infer_dimension(header),
            is_cube_map = (header.caps2 & 0x200) != 0,
            cube_map_face_count = count_cube_map_faces(header),
            surfaces = surfaces
        };
    }

    /// <summary>
    ///     仅解的DDS 文件头信息的
    /// </summary>
    public DdsTextureData decode_header()
    {
        var magic = _buffer.read_string(4);

        if (magic != "DDS ") throw new InvalidDataException($"DDS 文件签名无效，期的\"DDS \"，实的\"{magic}\"");

        var header = read_header();

        return new DdsTextureData
        {
            height = header.height,
            width = header.width,
            depth = header.depth,
            mip_map_count = header.mip_map_count,
            pixel_format = header.pixel_format
        };
    }

    #region 私有解析方法

    private DdsHeaderInfo read_header()
    {
        var size = _buffer.read_u32_le();

        if (size != DdsConstants.header_size)
            throw new InvalidDataException($"DDS 头部大小无效，期的{DdsConstants.header_size}，实的{size}");

        var flags = (DdsFlags)_buffer.read_u32_le();
        var height = (int)_buffer.read_u32_le();
        var width = (int)_buffer.read_u32_le();
        var pitchOrLinearSize = _buffer.read_u32_le();
        var depth = (int)_buffer.read_u32_le();
        var mipMapCount = (int)_buffer.read_u32_le();

        _buffer.advance(44);

        var pixelFormat = read_pixel_format();

        var caps1 = _buffer.read_u32_le();
        var caps2 = _buffer.read_u32_le();
        var caps3 = _buffer.read_u32_le();
        var caps4 = _buffer.read_u32_le();

        _buffer.advance(4);

        return new DdsHeaderInfo
        {
            flags = flags,
            height = height,
            width = width,
            pitch_or_linear_size = (int)pitchOrLinearSize,
            depth = depth,
            mip_map_count = mipMapCount,
            pixel_format = pixelFormat,
            caps1 = caps1,
            caps2 = caps2
        };
    }

    private DdsPixelFormatData read_pixel_format()
    {
        var size = _buffer.read_u32_le();

        if (size != DdsConstants.pixel_format_size)
            throw new InvalidDataException($"DDS 像素格式大小无效，期的{DdsConstants.pixel_format_size}，实的{size}");

        var flags = (DdsPixelFormatFlags)_buffer.read_u32_le();
        var fourCc = _buffer.read_u32_le();
        var rgbBitCount = _buffer.read_u32_le();
        var rBitMask = _buffer.read_u32_le();
        var gBitMask = _buffer.read_u32_le();
        var bBitMask = _buffer.read_u32_le();
        var aBitMask = _buffer.read_u32_le();

        return new DdsPixelFormatData
        {
            flags = flags,
            four_cc = fourCc,
            rgb_bit_count = rgbBitCount,
            r_bit_mask = rBitMask,
            g_bit_mask = gBitMask,
            b_bit_mask = bBitMask,
            a_bit_mask = aBitMask
        };
    }

    private DdsDx10Header? try_read_dx10_header(DdsHeaderInfo header)
    {
        if (header.pixel_format.four_cc != 0x30315844) return null;

        var dx10 = new DdsDx10Header
        {
            format = _buffer.read_u32_le(),
            dimension = (DdsResourceDimension)_buffer.read_u32_le(),
            misc_flag = _buffer.read_u32_le(),
            array_size = _buffer.read_u32_le(),
            misc_flags2 = _buffer.read_u32_le()
        };

        return dx10;
    }

    private List<DdsSurfaceData> read_surfaces(DdsHeaderInfo header, DdsDx10Header? dx10)
    {
        var surfaces = new List<DdsSurfaceData>();
        var surfaceCount = get_surface_count(header, dx10);

        for (var s = 0; s < surfaceCount; s++)
        {
            var mipLevels = new List<DdsMipLevelData>();
            var mipCount = System.Math.Max(1, header.mip_map_count);

            for (var m = 0; m < mipCount; m++)
            {
                var mipWidth = System.Math.Max(1, header.width >> m);
                var mipHeight = System.Math.Max(1, header.height >> m);
                var dataSize = compute_mip_data_size(header.pixel_format, mipWidth, mipHeight);

                if (_buffer.remaining < dataSize) break;

                var data = _buffer.read_bytes(dataSize).ToArray();
                mipLevels.Add(new DdsMipLevelData
                {
                    width = mipWidth,
                    height = mipHeight,
                    data = data
                });
            }

            surfaces.Add(new DdsSurfaceData { mip_levels = mipLevels });
        }

        return surfaces;
    }

    private static int get_surface_count(DdsHeaderInfo header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            var count = (int)dx10.array_size;

            if (dx10.dimension == DdsResourceDimension.texture3_d) count = 1;

            return count;
        }

        if ((header.caps2 & 0x200) != 0) return count_cube_map_faces(header);

        return 1;
    }

    private static int count_cube_map_faces(DdsHeaderInfo header)
    {
        if ((header.caps2 & 0x200) == 0) return 1;

        var faces = 0;

        if ((header.caps2 & 0x400) != 0) faces++;

        if ((header.caps2 & 0x800) != 0) faces++;

        if ((header.caps2 & 0x1000) != 0) faces++;

        if ((header.caps2 & 0x2000) != 0) faces++;

        if ((header.caps2 & 0x4000) != 0) faces++;

        if ((header.caps2 & 0x8000) != 0) faces++;

        return System.Math.Max(faces, 1);
    }

    private static DdsResourceDimension infer_dimension(DdsHeaderInfo header)
    {
        if (header.depth > 0) return DdsResourceDimension.texture3_d;

        return DdsResourceDimension.texture2_d;
    }

    private static int compute_mip_data_size(DdsPixelFormatData format, int width, int height)
    {
        var blockSize = get_block_size(format);

        if (blockSize > 0)
        {
            var blocksX = System.Math.Max(1, (width + 3) / 4);
            var blocksY = System.Math.Max(1, (height + 3) / 4);
            return blocksX * blocksY * blockSize;
        }

        var bpp = (int)format.rgb_bit_count;

        if (bpp == 0) bpp = 32;

        return width * height * (bpp / 8);
    }

    private static int get_block_size(DdsPixelFormatData format)
    {
        return format.four_cc switch
        {
            DdsFourCc.dxt1 => 8,
            DdsFourCc.dxt3 => 16,
            DdsFourCc.dxt5 => 16,
            DdsFourCc.ati1 => 8,
            DdsFourCc.ati2 => 16,
            DdsFourCc.bc6_h => 16,
            DdsFourCc.bc7 => 16,
            _ => 0
        };
    }

    #endregion
}

/// <summary>
///     DDS 头部信息（内部使用）的
/// </summary>
internal sealed class DdsHeaderInfo
{
    public DdsFlags flags { get; init; }
    public int height { get; init; }
    public int width { get; init; }
    public int pitch_or_linear_size { get; init; }
    public int depth { get; init; }
    public int mip_map_count { get; init; }
    public DdsPixelFormatData pixel_format { get; init; } = new();
    public uint caps1 { get; init; }
    public uint caps2 { get; init; }
}

/// <summary>
///     DDS DX10 扩展头部的
/// </summary>
internal sealed class DdsDx10Header
{
    public uint format { get; init; }
    public DdsResourceDimension dimension { get; init; }
    public uint misc_flag { get; init; }
    public uint array_size { get; init; }
    public uint misc_flags2 { get; init; }
}