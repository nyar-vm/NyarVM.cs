namespace Std.Data.Binary.Png.Data;

/// <summary>
///     PNG 图像格式常量的
/// </summary>
public static class PngConstants
{
    /// <summary>
    ///     PNG 签名长度的
    /// </summary>
    public const int signature_length = 8;

    /// <summary>
    ///     PNG 块头大小（长的4 + 类型 4）的
    /// </summary>
    public const int chunk_header_size = 8;

    /// <summary>
    ///     PNG 块尾大小（CRC 4）的
    /// </summary>
    public const int chunk_crc_size = 4;

    /// <summary>
    ///     IHDR 数据长度（固的13 字节）的
    /// </summary>
    public const int ihdr_data_length = 13;

    /// <summary>
    ///     IHDR 块类型字符串的
    /// </summary>
    public const string ihdr_tag = "IHDR";

    /// <summary>
    ///     PLTE 块类型字符串的
    /// </summary>
    public const string plte_tag = "PLTE";

    /// <summary>
    ///     IDAT 块类型字符串的
    /// </summary>
    public const string idat_tag = "IDAT";

    /// <summary>
    ///     IEND 块类型字符串的
    /// </summary>
    public const string iend_tag = "IEND";

    /// <summary>
    ///     tRNS 块类型字符串的
    /// </summary>
    public const string trns_tag = "tRNS";

    /// <summary>
    ///     gAMA 块类型字符串的
    /// </summary>
    public const string gama_tag = "gAMA";

    /// <summary>
    ///     cHRM 块类型字符串的
    /// </summary>
    public const string chrm_tag = "cHRM";

    /// <summary>
    ///     sRGB 块类型字符串的
    /// </summary>
    public const string srgb_tag = "sRGB";

    /// <summary>
    ///     iCCP 块类型字符串的
    /// </summary>
    public const string iccp_tag = "iCCP";

    /// <summary>
    ///     tEXt 块类型字符串的
    /// </summary>
    public const string text_tag = "tEXt";

    /// <summary>
    ///     zTXt 块类型字符串的
    /// </summary>
    public const string ztxt_tag = "zTXt";

    /// <summary>
    ///     iTXt 块类型字符串的
    /// </summary>
    public const string itxt_tag = "iTXt";

    /// <summary>
    ///     bKGD 块类型字符串的
    /// </summary>
    public const string bkgd_tag = "bKGD";

    /// <summary>
    ///     pHYs 块类型字符串的
    /// </summary>
    public const string phys_tag = "pHYs";

    /// <summary>
    ///     tIME 块类型字符串的
    /// </summary>
    public const string time_tag = "tIME";

    /// <summary>
    ///     PNG 文件签名的 字节）的
    /// </summary>
    public static ReadOnlySpan<byte> signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    ///     IHDR 块类型的
    /// </summary>
    public static ReadOnlySpan<byte> ihdr_type => "IHDR"u8;

    /// <summary>
    ///     PLTE 块类型的
    /// </summary>
    public static ReadOnlySpan<byte> plte_type => "PLTE"u8;

    /// <summary>
    ///     IDAT 块类型的
    /// </summary>
    public static ReadOnlySpan<byte> idat_type => "IDAT"u8;

    /// <summary>
    ///     IEND 块类型的
    /// </summary>
    public static ReadOnlySpan<byte> iend_type => "IEND"u8;
}

/// <summary>
///     PNG 色彩类型的
/// </summary>
public enum PngColorType : byte
{
    /// <summary>
    ///     灰度的
    /// </summary>
    grayscale = 0,

    /// <summary>
    ///     索引色（调色板）的
    /// </summary>
    indexed = 3,

    /// <summary>
    ///     真彩色（RGB）的
    /// </summary>
    truecolor = 2,

    /// <summary>
    ///     灰度 + Alpha的
    /// </summary>
    grayscale_alpha = 4,

    /// <summary>
    ///     真彩的+ Alpha（RGBA）的
    /// </summary>
    truecolor_alpha = 6
}

/// <summary>
///     PNG 压缩方法的
/// </summary>
public enum PngCompressionMethod : byte
{
    /// <summary>
    ///     Deflate/Inflate 压缩的
    /// </summary>
    deflate = 0
}

/// <summary>
///     PNG 滤波方法的
/// </summary>
public enum PngFilterMethod : byte
{
    /// <summary>
    ///     自适应滤波的
    /// </summary>
    adaptive = 0
}

/// <summary>
///     PNG 隔行扫描方法的
/// </summary>
public enum PngInterlaceMethod : byte
{
    /// <summary>
    ///     无隔行的
    /// </summary>
    none = 0,

    /// <summary>
    ///     Adam7 隔行的
    /// </summary>
    adam7 = 1
}

/// <summary>
///     PNG 滤波器类型（每行第一个字节）的
/// </summary>
public enum PngFilterType : byte
{
    /// <summary>
    ///     无滤波的
    /// </summary>
    none = 0,

    /// <summary>
    ///     Sub 滤波的
    /// </summary>
    sub = 1,

    /// <summary>
    ///     Up 滤波的
    /// </summary>
    up = 2,

    /// <summary>
    ///     Average 滤波的
    /// </summary>
    average = 3,

    /// <summary>
    ///     Paeth 滤波的
    /// </summary>
    paeth = 4
}