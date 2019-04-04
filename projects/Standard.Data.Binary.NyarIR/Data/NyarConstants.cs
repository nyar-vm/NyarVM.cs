namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 字节码模块格式常量
/// </summary>
public static class NyarConstants
{
    /// <summary>
    ///     魔数值
    /// </summary>
    public const uint magic_value = 0x4E594152;

    /// <summary>
    ///     当前版本号
    /// </summary>
    public const uint current_version = 1;

    /// <summary>
    ///     头部大小（16 字节）
    /// </summary>
    public const int header_size = 16;

    /// <summary>
    ///     段头大小（9 字节：Kind 1 + Offset 4 + Size 4）
    /// </summary>
    public const int section_header_size = 9;

    /// <summary>
    ///     .nyar 文件魔数（"NYAR" 大端序）
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => "NYAR"u8;
}