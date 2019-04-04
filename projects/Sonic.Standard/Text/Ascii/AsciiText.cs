using Core.Text;
using Std.Text.Utf16;
using Std.Text.Utf32;
using Std.Text.Utf8;

namespace Std.Text.Ascii;

public class AsciiText : IText
{
    private readonly byte[] _bytes;

    private AsciiText(byte[] bytes)
    {
        _bytes = bytes;
    }

    /// <summary>
    ///     获取空文本实例
    /// </summary>
    public static AsciiText empty => new([]);

    /// <summary>
    ///     获取字节长度
    /// </summary>
    public int byte_length => _bytes.Length;

    /// <summary>
    ///     获取字符数量
    /// </summary>
    public int char_count => _bytes.Length;

    public bool is_empty => _bytes.Length == 0;

    public TextSpan span => new(0, _bytes.Length);

    /// <summary>
    ///     获取文本长度
    /// </summary>
    int IText.length => byte_length;

    /// <summary>
    ///     获取文本编码
    /// </summary>
    TextEncoding IText.encoding => TextEncoding.Ascii;

    public AsciiBuilder to_builder()
    {
        var newBytes = new byte[_bytes.Length];
        Array.Copy(_bytes, newBytes, _bytes.Length);
        return new AsciiBuilder(newBytes);
    }

    public static AsciiText from_bytes(byte[] bytes)
    {
        validate_ascii(bytes);
        return new AsciiText(bytes);
    }

    public static AsciiText from_bytes_unchecked(byte[] bytes)
    {
        return new AsciiText(bytes);
    }

    public static AsciiText from_string(string str)
    {
        var bytes = new byte[str.Length];
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] > 127) throw new ArgumentException("String contains non-ASCII characters");

            bytes[i] = (byte)str[i];
        }

        return new AsciiText(bytes);
    }

    public static AsciiText from_utf8(Utf8Text utf8)
    {
        var span = utf8.as_span();
        var bytes = new byte[span.Length];
        span.CopyTo(bytes);
        validate_ascii(bytes);
        return new AsciiText(bytes);
    }

    public AsciiView view(TextSpan span)
    {
        validate_span(span);
        return new AsciiView(this, span);
    }

    public AsciiText slice(TextSpan span)
    {
        validate_span(span);
        var newBytes = new byte[span.length];
        Array.Copy(_bytes, span.offset, newBytes, 0, span.length);
        return new AsciiText(newBytes);
    }

    public ReadOnlySpan<byte> as_span()
    {
        return _bytes.AsSpan();
    }

    public ReadOnlySpan<byte> as_span(TextSpan span)
    {
        validate_span(span);
        return _bytes.AsSpan(span.offset, span.length);
    }

    public string to_system_string()
    {
        var chars = new char[_bytes.Length];
        for (var i = 0; i < _bytes.Length; i++)
            chars[i] = (char)_bytes[i];
        return new string(chars);
    }

    public Utf8Text to_utf8()
    {
        return Utf8Text.from_bytes(_bytes);
    }

    public Utf16Text to_utf16()
    {
        var chars = new ushort[_bytes.Length];
        for (var i = 0; i < _bytes.Length; i++)
            chars[i] = _bytes[i];
        return Utf16Text.from_chars_unchecked(chars);
    }

    public Utf32Text to_utf32()
    {
        var chars = new uint[_bytes.Length];
        for (var i = 0; i < _bytes.Length; i++)
            chars[i] = _bytes[i];
        return Utf32Text.from_chars_unchecked(chars);
    }

    public CText to_c_text()
    {
        var bytes = new byte[_bytes.Length + 1];
        Array.Copy(_bytes, bytes, _bytes.Length);
        bytes[_bytes.Length] = 0;
        return CText.from_bytes_unchecked(bytes);
    }

    public override string ToString()
    {
        return to_system_string();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AsciiText other) return false;

        return _bytes.AsSpan().SequenceEqual(other._bytes);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in _bytes) hash.Add(b);

        return hash.ToHashCode();
    }

    public static bool operator ==(AsciiText left, AsciiText right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AsciiText left, AsciiText right)
    {
        return !left.Equals(right);
    }

    public static implicit operator AsciiText(string str)
    {
        return from_string(str);
    }

    private static int count_occurrences(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        var count = 0;
        var pos = 0;

        while (pos < haystack.Length)
        {
            var found = haystack[pos..].IndexOf(needle);

            if (found < 0) break;

            count++;
            pos += found + needle.Length;
        }

        return count;
    }

    private AsciiText trim_whitespace(bool trimStart, bool trimEnd)
    {
        var span = as_span();
        var start = 0;
        var end = span.Length;

        if (trimStart)
            while (start < end && is_whitespace_byte(span[start]))
                start++;

        if (trimEnd)
            while (end > start && is_whitespace_byte(span[end - 1]))
                end--;

        return slice(new TextSpan(start, end - start));
    }

    private static bool is_whitespace_byte(byte b)
    {
        return b is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
    }

    private static void validate_ascii(byte[] bytes)
    {
        foreach (var b in bytes)
            if (b > 127)
                throw new ArgumentException("Byte array contains non-ASCII value");
    }

    private void validate_span(TextSpan span)
    {
        if (span.offset < 0) throw new ArgumentOutOfRangeException(nameof(span));

        if (span.offset + span.length > _bytes.Length) throw new ArgumentOutOfRangeException(nameof(span));
    }

    #region 字符串查询操�?

    /// <summary>
    ///     检查是否包含指定的字节序列
    /// </summary>
    /// <param name="needle">要搜索的字节序列</param>
    public bool contains(ReadOnlySpan<byte> needle)
    {
        return as_span().IndexOf(needle) >= 0;
    }

    /// <summary>
    ///     检查是否包含指定的 AsciiText
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    public bool contains(AsciiText needle)
    {
        return as_span().IndexOf(needle.as_span()) >= 0;
    }

    /// <summary>
    ///     查找指定字节序列的首次出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的字节序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(ReadOnlySpan<byte> needle)
    {
        return as_span().IndexOf(needle);
    }

    /// <summary>
    ///     查找指定 AsciiText 的首次出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(AsciiText needle)
    {
        return as_span().IndexOf(needle.as_span());
    }

    /// <summary>
    ///     查找指定字节序列的最后出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的字节序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(ReadOnlySpan<byte> needle)
    {
        return as_span().LastIndexOf(needle);
    }

    /// <summary>
    ///     查找指定 AsciiText 的最后出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(AsciiText needle)
    {
        return as_span().LastIndexOf(needle.as_span());
    }

    /// <summary>
    ///     检查是否以指定的字节序列开�?    ///
    /// </summary>
    /// <param name="prefix">前缀字节序列</param>
    public bool starts_with(ReadOnlySpan<byte> prefix)
    {
        return as_span().StartsWith(prefix);
    }

    /// <summary>
    ///     检查是否以指定�?AsciiText 开�?    ///
    /// </summary>
    /// <param name="prefix">前缀文本</param>
    public bool starts_with(AsciiText prefix)
    {
        return as_span().StartsWith(prefix.as_span());
    }

    /// <summary>
    ///     检查是否以指定的字节序列结�?    ///
    /// </summary>
    /// <param name="suffix">后缀字节序列</param>
    public bool ends_with(ReadOnlySpan<byte> suffix)
    {
        return as_span().EndsWith(suffix);
    }

    /// <summary>
    ///     检查是否以指定�?AsciiText 结尾
    /// </summary>
    /// <param name="suffix">后缀文本</param>
    public bool ends_with(AsciiText suffix)
    {
        return as_span().EndsWith(suffix.as_span());
    }

    #endregion

    #region 字符串修剪和替换操作

    /// <summary>
    ///     移除首尾空白字符
    /// </summary>
    public AsciiText trim()
    {
        return trim_whitespace(true, true);
    }

    /// <summary>
    ///     移除首部空白字符
    /// </summary>
    public AsciiText trim_start()
    {
        return trim_whitespace(true, false);
    }

    /// <summary>
    ///     移除尾部空白字符
    /// </summary>
    public AsciiText trim_end()
    {
        return trim_whitespace(false, true);
    }

    /// <summary>
    ///     替换所有出现的目标字节序列
    /// </summary>
    /// <param name="oldValue">要替换的字节序列</param>
    /// <param name="newValue">替换为的字节序列</param>
    public AsciiText replace(ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        if (oldValue.IsEmpty) throw new ArgumentException("oldValue 不能为空", nameof(oldValue));

        var span = as_span();
        var count = count_occurrences(span, oldValue);

        if (count == 0) return this;

        var newSize = span.Length - count * oldValue.Length + count * newValue.Length;
        var result = new byte[newSize];
        var srcPos = 0;
        var dstPos = 0;

        while (srcPos < span.Length)
        {
            var remaining = span[srcPos..];
            var found = remaining.IndexOf(oldValue);

            if (found < 0)
            {
                remaining.CopyTo(result.AsSpan(dstPos));
                break;
            }

            span.Slice(srcPos, found).CopyTo(result.AsSpan(dstPos));
            dstPos += found;
            srcPos += found;
            newValue.CopyTo(result.AsSpan(dstPos));
            dstPos += newValue.Length;
            srcPos += oldValue.Length;
        }

        return new AsciiText(result);
    }

    #endregion
}
