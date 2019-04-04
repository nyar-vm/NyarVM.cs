namespace Std.Codec;

/// <summary>
///     字节序枚举，定义二进制数据在内存中的存储顺序。
/// </summary>
public enum Endianness
{
    /// <summary>
    ///     小端序（Little Endian），低位字节存储在低地址。
    /// </summary>
    little_endian,

    /// <summary>
    ///     大端序（Big Endian），高位字节存储在低地址。
    /// </summary>
    big_endian
}