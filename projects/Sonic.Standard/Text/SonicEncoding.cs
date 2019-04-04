namespace Std.Text;

/// <summary>
///     文本编码工具，封装字符串与字节序列之间的转换�?/// 内部委托�?<c>System.Text.Encoding</c>，将依赖收敛到单一文件�?///
/// </summary>
public static class SonicEncoding
{
    /// <summary>
    ///     将字符串编码�?UTF-8 字节数组�?    ///
    /// </summary>
    /// <param name="text">
    ///     要编码的字符串�?/param>
    ///     <returns>UTF-8 编码的字节数组�?/returns>
    public static byte[] encode_utf8(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    ///     将字符串编码�?UTF-8 字节并写入目标跨度�?    ///
    /// </summary>
    /// <param name="text">
    ///     要编码的字符串�?/param>
    ///     <param name="destination">
    ///         目标字节跨度�?/param>
    ///         <returns>写入的字节数�?/returns>
    public static int encode_utf8(string text, Span<byte> destination)
    {
        return Encoding.UTF8.GetBytes(text, destination);
    }

    /// <summary>
    ///     计算字符串的 UTF-8 编码字节长度�?    ///
    /// </summary>
    /// <param name="text">
    ///     要计算的字符串�?/param>
    ///     <returns>UTF-8 编码所需的字节数�?/returns>
    public static int utf8_byte_count(string text)
    {
        return Encoding.UTF8.GetByteCount(text);
    }

    /// <summary>
    ///     �?UTF-8 字节序列解码为字符串�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     UTF-8 编码的字节序列�?/param>
    ///     <returns>解码后的字符串�?/returns>
    public static string decode_utf8(ReadOnlySpan<byte> bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    ///     �?UTF-8 字节数组解码为字符串�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     UTF-8 编码的字节数组�?/param>
    ///     <returns>解码后的字符串�?/returns>
    public static string decode_utf8(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    ///     将字符串编码�?ASCII 字节数组�?    ///
    /// </summary>
    /// <param name="text">
    ///     要编码的字符串�?/param>
    ///     <returns>ASCII 编码的字节数组�?/returns>
    public static byte[] encode_ascii(string text)
    {
        return Encoding.ASCII.GetBytes(text);
    }

    /// <summary>
    ///     �?ASCII 字节序列解码为字符串�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     ASCII 编码的字节序列�?/param>
    ///     <returns>解码后的字符串�?/returns>
    public static string decode_ascii(ReadOnlySpan<byte> bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    ///     将字节数组编码为 Base64 字符串�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     要编码的字节数组�?/param>
    ///     <returns>Base64 编码的字符串�?/returns>
    public static string to_base64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    ///     将字节跨度编码为 Base64 字符串�?    ///
    /// </summary>
    /// <param name="bytes">
    ///     要编码的字节跨度�?/param>
    ///     <returns>Base64 编码的字符串�?/returns>
    public static string to_base64(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes);
    }
}