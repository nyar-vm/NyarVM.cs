namespace Std.Data.Binary.Tga.Data;

/// <summary>
///     TGA 图像格式常量的
/// </summary>
public static class TgaConstants
{
    /// <summary>
    ///     TGA 文件尾签名字符串的8 字节，含空终止符）的
    /// </summary>
    public const string footer_signature = "TRUEVISION-XFILE.";

    /// <summary>
    ///     TGA 文件尾签名长度的
    /// </summary>
    public const int footer_signature_length = 18;

    /// <summary>
    ///     TGA 文件头大小（固定 18 字节）的
    /// </summary>
    public const int header_size = 18;
}

/// <summary>
///     TGA 图像类型的
/// </summary>
public enum TgaImageType : byte
{
    /// <summary>
    ///     无图像数据的
    /// </summary>
    no_data = 0,

    /// <summary>
    ///     未压缩的调色板图像的
    /// </summary>
    uncompressed_color_map = 1,

    /// <summary>
    ///     未压缩的真彩色图像的
    /// </summary>
    uncompressed_truecolor = 2,

    /// <summary>
    ///     未压缩的灰度图像的
    /// </summary>
    uncompressed_grayscale = 3,

    /// <summary>
    ///     RLE 压缩的调色板图像的
    /// </summary>
    rle_color_map = 9,

    /// <summary>
    ///     RLE 压缩的真彩色图像的
    /// </summary>
    rle_truecolor = 10,

    /// <summary>
    ///     RLE 压缩的灰度图像的
    /// </summary>
    rle_grayscale = 11
}

/// <summary>
///     TGA 像素位深度的
/// </summary>
public enum TgaPixelDepth : byte
{
    /// <summary>
    ///     8 位像素深度的
    /// </summary>
    bit8 = 8,

    /// <summary>
    ///     16 位像素深度的
    /// </summary>
    bit16 = 16,

    /// <summary>
    ///     24 位像素深度的
    /// </summary>
    bit24 = 24,

    /// <summary>
    ///     32 位像素深度的
    /// </summary>
    bit32 = 32
}