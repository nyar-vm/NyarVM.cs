namespace Std.Data.Binary.Psd.Data;

/// <summary>
///     Adobe Photoshop PSD 二进制格式常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 Adobe PSD 文件格式规范，Acorn 独占二进制编解码职责的
/// </remarks>
public static class PsdConstants
{
    /// <summary>
    ///     PSD 文件版本号的
    /// </summary>
    public const ushort version = 1;

    /// <summary>
    ///     PSD 扩展长度标记的GB 以上）的
    /// </summary>
    public const uint extended_length_marker = 0xFFFFFFFF;

    /// <summary>
    ///     PSD 文件头大小（26 字节的 签名 + 2 版本 + 6 保留 + 2 通道 + 4 高度 + 4 宽度 + 2 深度 + 2 颜色模式）的
    /// </summary>
    public const int header_size = 26;

    /// <summary>
    ///     PSD 文件魔数的8BPS"）的
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => "8BPS"u8;

    /// <summary>
    ///     PSD 图层混合模式签名的8BPS"）的
    /// </summary>
    public static ReadOnlySpan<byte> blend_mode_signature => "8BPS"u8;
}

/// <summary>
///     PSD 颜色模式枚举的
/// </summary>
public enum PsdColorMode : ushort
{
    /// <summary>
    ///     位图的
    /// </summary>
    bitmap = 0,

    /// <summary>
    ///     灰度的
    /// </summary>
    grayscale = 1,

    /// <summary>
    ///     索引色的
    /// </summary>
    indexed = 2,

    /// <summary>
    ///     RGB的
    /// </summary>
    rgb = 3,

    /// <summary>
    ///     CMYK的
    /// </summary>
    cmyk = 4,

    /// <summary>
    ///     多通道的
    /// </summary>
    multichannel = 7,

    /// <summary>
    ///     双色调的
    /// </summary>
    duotone = 8,

    /// <summary>
    ///     Lab的
    /// </summary>
    lab = 9
}

/// <summary>
///     PSD 压缩方式枚举的
/// </summary>
public enum PsdCompression : short
{
    /// <summary>
    ///     无压缩的
    /// </summary>
    raw = 0,

    /// <summary>
    ///     RLE 压缩的
    /// </summary>
    rle = 1
}

/// <summary>
///     PSD 图层混合模式枚举的
/// </summary>
/// <remarks>
///     对应 PSD 规范中图层记录的混合模式 4 字节 ASCII 标识的
/// </remarks>
public enum PsdBlendMode
{
    /// <summary>
    ///     未知混合模式的
    /// </summary>
    unknown = 0,

    /// <summary>
    ///     正常（norm）的
    /// </summary>
    normal = 1,

    /// <summary>
    ///     溶解（diss）的
    /// </summary>
    dissolve = 2,

    /// <summary>
    ///     正片叠底（mul）的
    /// </summary>
    multiply = 3,

    /// <summary>
    ///     滤色（scrn）的
    /// </summary>
    screen = 4,

    /// <summary>
    ///     叠加（over）的
    /// </summary>
    overlay = 5,

    /// <summary>
    ///     柔光（sLit）的
    /// </summary>
    soft_light = 6,

    /// <summary>
    ///     强光（hLit）的
    /// </summary>
    hard_light = 7,

    /// <summary>
    ///     颜色减淡（hLit）的
    /// </summary>
    color_dodge = 8,

    /// <summary>
    ///     颜色加深（cBurn）的
    /// </summary>
    color_burn = 9,

    /// <summary>
    ///     深色（dkCl）的
    /// </summary>
    darken = 10,

    /// <summary>
    ///     浅色（lgCl）的
    /// </summary>
    lighten = 11,

    /// <summary>
    ///     差值（diff）的
    /// </summary>
    difference = 12,

    /// <summary>
    ///     排除（smud）的
    /// </summary>
    exclusion = 13,

    /// <summary>
    ///     色相（hue）的
    /// </summary>
    hue = 14,

    /// <summary>
    ///     饱和度（sat）的
    /// </summary>
    saturation = 15,

    /// <summary>
    ///     颜色（colr）的
    /// </summary>
    color = 16,

    /// <summary>
    ///     明度（lum）的
    /// </summary>
    luminosity = 17,

    /// <summary>
    ///     穿透（pass）的
    /// </summary>
    pass_through = 18
}