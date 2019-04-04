namespace Std.Data.Binary.Dds.Data;

/// <summary>
///     DirectDraw Surface (DDS) 二进制格式常量的
/// </summary>
public static class DdsConstants
{
    /// <summary>
    ///     DDS 文件头大小（124 字节）的
    /// </summary>
    public const int header_size = 124;

    /// <summary>
    ///     DDS 头部结构大小（含魔数 4 字节 + 头部 124 字节）的
    /// </summary>
    public const int full_header_size = 128;

    /// <summary>
    ///     像素格式结构大小的2 字节）的
    /// </summary>
    public const int pixel_format_size = 32;

    /// <summary>
    ///     DDS 文件魔数的DDS "）的
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => "DDS "u8;
}

/// <summary>
///     DDS 表面标志位的
/// </summary>
[Flags]
public enum DdsFlags : uint
{
    /// <summary>
    ///     包含高度信息的
    /// </summary>
    height = 0x00000002,

    /// <summary>
    ///     包含宽度信息的
    /// </summary>
    width = 0x00000004,

    /// <summary>
    ///     包含像素格式的
    /// </summary>
    pixel_format = 0x00001000,

    /// <summary>
    ///     包含间距（压缩纹理）的
    /// </summary>
    pitch = 0x00000008,

    /// <summary>
    ///     包含行间距（未压缩纹理）的
    /// </summary>
    linear_size = 0x00080000,

    /// <summary>
    ///     包含 Mipmap 数量的
    /// </summary>
    mip_map_count = 0x00020000,

    /// <summary>
    ///     包含深度（体积纹理）的
    /// </summary>
    depth = 0x00800000
}

/// <summary>
///     DDS 像素格式标志位的
/// </summary>
[Flags]
public enum DdsPixelFormatFlags : uint
{
    /// <summary>
    ///     包含 Alpha 数据的
    /// </summary>
    alpha_pixels = 0x00000001,

    /// <summary>
    ///     Alpha 预乘的
    /// </summary>
    alpha = 0x00000002,

    /// <summary>
    ///     四字符代码（FourCC）压缩格式的
    /// </summary>
    four_cc = 0x00000004,

    /// <summary>
    ///     RGB 格式的
    /// </summary>
    rgb = 0x00000040,

    /// <summary>
    ///     YUV 格式的
    /// </summary>
    yuv = 0x00000200,

    /// <summary>
    ///     Luminance 格式的
    /// </summary>
    luminance = 0x00020000
}

/// <summary>
///     DDS 资源维度的
/// </summary>
public enum DdsResourceDimension : uint
{
    /// <summary>
    ///     未知维度的
    /// </summary>
    unknown = 0,

    /// <summary>
    ///     纹理 1D的
    /// </summary>
    texture1_d = 2,

    /// <summary>
    ///     纹理 2D的
    /// </summary>
    texture2_d = 3,

    /// <summary>
    ///     纹理 3D的
    /// </summary>
    texture3_d = 4
}

/// <summary>
///     DDS 常用 FourCC 压缩格式的
/// </summary>
public static class DdsFourCc
{
    /// <summary>
    ///     DXT1 压缩（BC1）的
    /// </summary>
    public const uint dxt1 = 0x31545844;

    /// <summary>
    ///     DXT3 压缩（BC2）的
    /// </summary>
    public const uint dxt3 = 0x33545844;

    /// <summary>
    ///     DXT5 压缩（BC3）的
    /// </summary>
    public const uint dxt5 = 0x35545844;

    /// <summary>
    ///     ATI1 压缩（BC4）的
    /// </summary>
    public const uint ati1 = 0x31495441;

    /// <summary>
    ///     ATI2 压缩（BC5）的
    /// </summary>
    public const uint ati2 = 0x32495441;

    /// <summary>
    ///     BC6H 压缩（HDR）的
    /// </summary>
    public const uint bc6_h = 0x48364342;

    /// <summary>
    ///     BC7 压缩（高质量）的
    /// </summary>
    public const uint bc7 = 0x37434220;
}