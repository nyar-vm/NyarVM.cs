namespace Std.Data.Binary.Exr.Data;

/// <summary>
///     OpenEXR 格式常量的
/// </summary>
public static class ExrConstants
{
    /// <summary>
    ///     EXR 头部大小（魔的+ 版本）的
    /// </summary>
    public const int header_size = 8;

    /// <summary>
    ///     EXR 魔数的x762f3101）的
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => [0x76, 0x2F, 0x31, 0x01];
}

/// <summary>
///     EXR 压缩类型的
/// </summary>
public enum ExrCompression : byte
{
    /// <summary>
    ///     无压缩的
    /// </summary>
    none = 0,

    /// <summary>
    ///     RLE 压缩的
    /// </summary>
    rle = 1,

    /// <summary>
    ///     ZIPS 压缩的
    /// </summary>
    zips = 2,

    /// <summary>
    ///     ZIP 压缩的
    /// </summary>
    zip = 3,

    /// <summary>
    ///     PIZ 压缩的
    /// </summary>
    piz = 4,

    /// <summary>
    ///     PXR24 压缩的
    /// </summary>
    pxr24 = 5,

    /// <summary>
    ///     B44 压缩的
    /// </summary>
    b44 = 6,

    /// <summary>
    ///     B44A 压缩的
    /// </summary>
    b44_a = 7,

    /// <summary>
    ///     DWAA 压缩的
    /// </summary>
    dwaa = 8,

    /// <summary>
    ///     DWAB 压缩的
    /// </summary>
    dwab = 9
}

/// <summary>
///     EXR 像素类型的
/// </summary>
public enum ExrPixelType
{
    /// <summary>
    ///     32 位无符号整数的
    /// </summary>
    @uint = 0,

    /// <summary>
    ///     16 位半精度浮点的
    /// </summary>
    half = 1,

    /// <summary>
    ///     32 位单精度浮点的
    /// </summary>
    @float = 2
}