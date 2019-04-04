namespace Std.Codec;

/// <summary>
///     ZigZag 编码工具类，用于将有符号整数映射为无符号整数，以便配合变长编码（如 LEB128）高效压缩负数。
/// </summary>
/// <remarks>
///     ZigZag 编码原理：将符号位移动到最低位，使得绝对值较小的负数也能被高效编码。
///     例如：0 -> 0, -1 -> 1, 1 -> 2, -2 -> 3, 2 -> 4 ...
/// </remarks>
public static class ZigZag
{
    /// <summary>
    ///     将 32 位有符号整数编码为 ZigZag 格式。
    /// </summary>
    /// <param name="value">要编码的有符号整数。</param>
    /// <returns>ZigZag 编码后的无符号整数。</returns>
    public static uint encode(int value)
    {
        return (uint)((value << 1) ^ (value >> 31));
    }

    /// <summary>
    ///     将 64 位有符号整数编码为 ZigZag 格式。
    /// </summary>
    /// <param name="value">要编码的有符号整数。</param>
    /// <returns>ZigZag 编码后的无符号整数。</returns>
    public static ulong encode(long value)
    {
        return (ulong)((value << 1) ^ (value >> 63));
    }

    /// <summary>
    ///     将 16 位有符号整数编码为 ZigZag 格式。
    /// </summary>
    /// <param name="value">要编码的有符号整数。</param>
    /// <returns>ZigZag 编码后的无符号整数。</returns>
    public static ushort encode(short value)
    {
        return (ushort)((value << 1) ^ (value >> 15));
    }

    /// <summary>
    ///     将 ZigZag 编码的 32 位无符号整数解码为原始有符号整数。
    /// </summary>
    /// <param name="value">ZigZag 编码后的无符号整数。</param>
    /// <returns>解码后的有符号整数。</returns>
    public static int decode(uint value)
    {
        return (int)((value >> 1) ^ -(value & 1));
    }

    /// <summary>
    ///     将 ZigZag 编码的 64 位无符号整数解码为原始有符号整数。
    /// </summary>
    /// <param name="value">ZigZag 编码后的无符号整数。</param>
    /// <returns>解码后的有符号整数。</returns>
    public static long decode(ulong value)
    {
        return (long)((value >> 1) ^ (0UL - (value & 1)));
    }

    /// <summary>
    ///     将 ZigZag 编码的 16 位无符号整数解码为原始有符号整数。
    /// </summary>
    /// <param name="value">ZigZag 编码后的无符号整数。</param>
    /// <returns>解码后的有符号整数。</returns>
    public static short decode(ushort value)
    {
        return (short)((value >> 1) ^ -(value & 1));
    }
}