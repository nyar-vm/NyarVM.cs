namespace Std.Data.Binary.Usd.Data;

/// <summary>
///     USD 格式常量的
/// </summary>
public static class UsdConstants
{
    /// <summary>
    ///     USDC 魔数长度的
    /// </summary>
    public const int magic_length = 8;

    /// <summary>
    ///     USDC 版本偏移的
    /// </summary>
    public const int version_offset = 8;

    /// <summary>
    ///     USDC 文件头大小的
    /// </summary>
    public const int header_size = 48;

    /// <summary>
    ///     USDA 文本格式标识的
    /// </summary>
    public const string usda_identifier = "#usda";

    /// <summary>
    ///     USDC 二进制格式魔数（"PXR-USDC"）的
    /// </summary>
    public static ReadOnlySpan<byte> usdc_magic => "PXR-USDC"u8.ToArray();
}

/// <summary>
///     USD 文件类型的
/// </summary>
public enum UsdFileType
{
    /// <summary>
    ///     二进的crate 格式的usdc）的
    /// </summary>
    crate,

    /// <summary>
    ///     文本格式的usda）的
    /// </summary>
    ascii,

    /// <summary>
    ///     打包格式的usdz）的
    /// </summary>
    package,

    /// <summary>
    ///     未知格式的
    /// </summary>
    unknown
}