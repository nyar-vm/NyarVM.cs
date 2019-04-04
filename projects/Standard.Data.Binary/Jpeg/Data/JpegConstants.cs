namespace Std.Data.Binary.Jpeg.Data;

/// <summary>
///     JPEG 图像格式常量的
/// </summary>
public static class JpegConstants
{
    /// <summary>
    ///     SOI（Start of Image）标记的
    /// </summary>
    public const byte soi_marker = 0xD8;

    /// <summary>
    ///     EOI（End of Image）标记的
    /// </summary>
    public const byte eoi_marker = 0xD9;

    /// <summary>
    ///     SOF0（Baseline DCT）标记的
    /// </summary>
    public const byte sof0_marker = 0xC0;

    /// <summary>
    ///     SOF2（Progressive DCT）标记的
    /// </summary>
    public const byte sof2_marker = 0xC2;

    /// <summary>
    ///     DHT（Define Huffman Table）标记的
    /// </summary>
    public const byte dht_marker = 0xC4;

    /// <summary>
    ///     DQT（Define Quantization Table）标记的
    /// </summary>
    public const byte dqt_marker = 0xDB;

    /// <summary>
    ///     SOS（Start of Scan）标记的
    /// </summary>
    public const byte sos_marker = 0xDA;

    /// <summary>
    ///     RST0（Restart 0）标记的
    /// </summary>
    public const byte rst0_marker = 0xD0;

    /// <summary>
    ///     RST1（Restart 1）标记的
    /// </summary>
    public const byte rst1_marker = 0xD1;

    /// <summary>
    ///     RST2（Restart 2）标记的
    /// </summary>
    public const byte rst2_marker = 0xD2;

    /// <summary>
    ///     RST3（Restart 3）标记的
    /// </summary>
    public const byte rst3_marker = 0xD3;

    /// <summary>
    ///     RST4（Restart 4）标记的
    /// </summary>
    public const byte rst4_marker = 0xD4;

    /// <summary>
    ///     RST5（Restart 5）标记的
    /// </summary>
    public const byte rst5_marker = 0xD5;

    /// <summary>
    ///     RST6（Restart 6）标记的
    /// </summary>
    public const byte rst6_marker = 0xD6;

    /// <summary>
    ///     RST7（Restart 7）标记的
    /// </summary>
    public const byte rst7_marker = 0xD7;

    /// <summary>
    ///     APP0（JFIF 应用标记）的
    /// </summary>
    public const byte app0_marker = 0xE0;

    /// <summary>
    ///     APP1（EXIF 应用标记）的
    /// </summary>
    public const byte app1_marker = 0xE1;

    /// <summary>
    ///     JPEG 文件签名的 字节的xFF 0xD8 0xFF）的
    /// </summary>
    public static ReadOnlySpan<byte> signature => [0xFF, 0xD8, 0xFF];
}

/// <summary>
///     JPEG 色彩空间的
/// </summary>
public enum JpegColorSpace
{
    /// <summary>
    ///     灰度的
    /// </summary>
    grayscale = 0,

    /// <summary>
    ///     YCbCr 色彩空间的
    /// </summary>
    y_cb_cr = 1,

    /// <summary>
    ///     YCCK 色彩空间的
    /// </summary>
    ycck = 2,

    /// <summary>
    ///     CMYK 色彩空间的
    /// </summary>
    cmyk = 3,

    /// <summary>
    ///     RGB 色彩空间的
    /// </summary>
    rgb = 4
}