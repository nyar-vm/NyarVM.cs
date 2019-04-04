namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 文件中可选头的格式标识的
/// </summary>
public enum OptionalHeaderMagic : ushort
{
    /// <summary>
    ///     PE32 格式的2 位可执行文件），可选头使用 32 位地址的
    /// </summary>
    pe32 = 0x10b,

    /// <summary>
    ///     PE32+ 格式的4 位可执行文件），可选头使用 64 位地址的
    /// </summary>
    pe32_plus = 0x20b
}