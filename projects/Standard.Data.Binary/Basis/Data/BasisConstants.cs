namespace Std.Data.Binary.Basis.Data;

/// <summary>
///     Basis Universal / KTX2 格式常量的
/// </summary>
public static class BasisConstants
{
    /// <summary>
    ///     KTX2 头部大小的
    /// </summary>
    public const int ktx2_header_size = 68;

    /// <summary>
    ///     Basis 头部大小的
    /// </summary>
    public const int basis_header_size = 78;

    /// <summary>
    ///     KTX2 文件魔数的
    /// </summary>
    public static ReadOnlySpan<byte> ktx2_magic =>
    [
        0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
    ];

    /// <summary>
    ///     Basis 文件魔数的sB"）的
    /// </summary>
    public static ReadOnlySpan<byte> basis_magic => "sB"u8.ToArray();
}

/// <summary>
///     Basis 纹理格式的
/// </summary>
public enum BasisTextureFormat : uint
{
    /// <summary>
    ///     ETC1S 格式的
    /// </summary>
    etc1_s = 0,

    /// <summary>
    ///     UASTC 格式的
    /// </summary>
    uastc = 1
}