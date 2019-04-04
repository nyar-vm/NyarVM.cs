namespace Std.Data.Binary.BmFont.Data;

/// <summary>
///     BMFont 二进制格式常量的
/// </summary>
public static class BmFontConstants
{
    /// <summary>
    ///     BMFont 二进制版本号的）的
    /// </summary>
    public const byte binary_version = 3;

    /// <summary>
    ///     信息的ID的
    /// </summary>
    public const byte block_info = 1;

    /// <summary>
    ///     通用的ID的
    /// </summary>
    public const byte block_common = 2;

    /// <summary>
    ///     页面的ID的
    /// </summary>
    public const byte block_pages = 3;

    /// <summary>
    ///     字符的ID的
    /// </summary>
    public const byte block_chars = 4;

    /// <summary>
    ///     字距的ID的
    /// </summary>
    public const byte block_kerning_pairs = 5;

    /// <summary>
    ///     BMFont 二进制格式魔数（"BMF"）的
    /// </summary>
    public static byte[] binary_magic => "BMF"u8.ToArray();
}

/// <summary>
///     BMFont 通道类型的
/// </summary>
public enum BmFontChannel : byte
{
    /// <summary>
    ///     通道值等于字形属性的
    /// </summary>
    glyph = 0,

    /// <summary>
    ///     轮廓通道的
    /// </summary>
    outline = 1,

    /// <summary>
    ///     字形 + 轮廓通道的
    /// </summary>
    glyph_and_outline = 2,

    /// <summary>
    ///     零通道（alpha = 0）的
    /// </summary>
    zero = 3
}