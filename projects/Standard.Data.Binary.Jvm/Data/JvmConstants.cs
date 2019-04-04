using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Jvm.Data;

/// <summary>
///     JVM ClassFile 格式常量的
/// </summary>
public static class JvmConstants
{
    /// <summary>
    ///     ClassFile 魔数的xCAFEBABE）的
    /// </summary>
    public const uint magic = 0xCAFEBABE;

    /// <summary>
    ///     ClassFile 魔数的大端字节序表示，用的<see cref="SpanScanner.match_magic" />的
    /// </summary>
    public static readonly byte[] magic_big_endian = [0xCA, 0xFE, 0xBA, 0xBE];
}