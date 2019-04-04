namespace Std.Data.Binary.Dds.Data;

/// <summary>
///     DDS 纹理文件数据的
/// </summary>
public sealed class DdsTextureData
{
    /// <summary>
    ///     纹理高度（像素）的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     纹理宽度（像素）的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     纹理深度（体积纹理使用，默认的0）的
    /// </summary>
    public int depth { get; init; }

    /// <summary>
    ///     Mipmap 级别数量的
    /// </summary>
    public int mip_map_count { get; init; }

    /// <summary>
    ///     像素格式的
    /// </summary>
    public DdsPixelFormatData pixel_format { get; init; } = new();

    /// <summary>
    ///     资源维度的
    /// </summary>
    public DdsResourceDimension dimension { get; init; }

    /// <summary>
    ///     是否为立方体贴图的
    /// </summary>
    public bool is_cube_map { get; init; }

    /// <summary>
    ///     立方体贴图面数（6 表示完整立方体贴图）的
    /// </summary>
    public int cube_map_face_count { get; init; }

    /// <summary>
    ///     纹理数据（按表面排列）的
    /// </summary>
    public IReadOnlyList<DdsSurfaceData> surfaces { get; init; } = [];
}

/// <summary>
///     DDS 像素格式数据的
/// </summary>
public sealed class DdsPixelFormatData
{
    /// <summary>
    ///     像素格式标志位的
    /// </summary>
    public DdsPixelFormatFlags flags { get; init; }

    /// <summary>
    ///     FourCC 压缩格式代码的
    /// </summary>
    public uint four_cc { get; init; }

    /// <summary>
    ///     每像的RGB 位数的
    /// </summary>
    public uint rgb_bit_count { get; init; }

    /// <summary>
    ///     红色通道位掩码的
    /// </summary>
    public uint r_bit_mask { get; init; }

    /// <summary>
    ///     绿色通道位掩码的
    /// </summary>
    public uint g_bit_mask { get; init; }

    /// <summary>
    ///     蓝色通道位掩码的
    /// </summary>
    public uint b_bit_mask { get; init; }

    /// <summary>
    ///     Alpha 通道位掩码的
    /// </summary>
    public uint a_bit_mask { get; init; }

    /// <summary>
    ///     FourCC 格式名称的
    /// </summary>
    public string four_cc_name => four_cc switch
    {
        DdsFourCc.dxt1 => "DXT1",
        DdsFourCc.dxt3 => "DXT3",
        DdsFourCc.dxt5 => "DXT5",
        DdsFourCc.ati1 => "ATI1",
        DdsFourCc.ati2 => "ATI2",
        DdsFourCc.bc6_h => "BC6H",
        DdsFourCc.bc7 => "BC7",
        0 => "无",
        _ => $"0x{four_cc:X8}"
    };
}

/// <summary>
///     DDS 表面数据（包的Mipmap 链）的
/// </summary>
public sealed class DdsSurfaceData
{
    /// <summary>
    ///     Mipmap 级别数据的
    /// </summary>
    public IReadOnlyList<DdsMipLevelData> mip_levels { get; init; } = [];
}

/// <summary>
///     DDS Mipmap 级别数据的
/// </summary>
public sealed class DdsMipLevelData
{
    /// <summary>
    ///     级别宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     级别高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     像素数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}