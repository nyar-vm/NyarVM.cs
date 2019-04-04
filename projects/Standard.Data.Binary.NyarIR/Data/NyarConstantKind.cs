namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     常量池条目类型
/// </summary>
public enum NyarConstantKind : byte
{
    /// <summary>
    ///     空值
    /// </summary>
    @null = 0x00,

    /// <summary>
    ///     布尔值
    /// </summary>
    boolean = 0x01,

    /// <summary>
    ///     32 位整数
    /// </summary>
    integer32 = 0x11,

    /// <summary>
    ///     大整数
    /// </summary>
    big_int = 0x06,

    /// <summary>
    ///     字符串
    /// </summary>
    @string = 0x05,

    /// <summary>
    ///     64 位浮点数
    /// </summary>
    float64 = 0x22
}