namespace Std.Data.Binary.Bmp.Data;

/// <summary>
///     BMP 图像格式常量的
/// </summary>
public static class BmpConstants
{
    /// <summary>
    ///     BMP 文件头大小（14 字节）的
    /// </summary>
    public const int file_header_size = 14;

    /// <summary>
    ///     BITMAPINFOHEADER 大小的0 字节）的
    /// </summary>
    public const int info_header_size = 40;

    /// <summary>
    ///     BMP 魔数字符串的
    /// </summary>
    public const string magic_tag = "BM";

    /// <summary>
    ///     BMP 文件魔数的BM"）的
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => "BM"u8;
}

/// <summary>
///     BMP 压缩方式的
/// </summary>
public enum BmpCompression : uint
{
    /// <summary>
    ///     无压缩的
    /// </summary>
    none = 0,

    /// <summary>
    ///     RLE 8 位压缩的
    /// </summary>
    rle8 = 1,

    /// <summary>
    ///     RLE 4 位压缩的
    /// </summary>
    rle4 = 2,

    /// <summary>
    ///     位域掩码的
    /// </summary>
    bit_fields = 3,

    /// <summary>
    ///     JPEG 压缩的
    /// </summary>
    jpeg = 4,

    /// <summary>
    ///     PNG 压缩的
    /// </summary>
    png = 5
}