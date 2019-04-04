using Core.Text;
using Std.Text.Ascii;
using Std.Text.Utf16;
using Std.Text.Utf8;

namespace Std.Text.Utf32;

public class Utf32Text : IText
{
    private readonly uint[] _chars;

    private Utf32Text(uint[] chars)
    {
        _chars = chars;
    }

    /// <summary>
    ///     获取空文本实例
    /// </summary>
    public static Utf32Text empty => new([]);

    /// <summary>
    ///     获取字节长度
    /// </summary>
    public int byte_length => _chars.Length * 4;

    /// <summary>
    ///     获取字符数量
    /// </summary>
    public int char_count => _chars.Length;

    public bool is_empty => _chars.Length == 0;

    public TextSpan span => new(0, _chars.Length);

    /// <summary>
    ///     获取文本长度
    /// </summary>
    int IText.length => char_count;

    /// <summary>
    ///     获取文本编码
    /// </summary>
    TextEncoding IText.encoding => TextEncoding.Utf32;

    public Utf32Builder to_builder()
    {
        var newChars = new uint[_chars.Length];
        Array.Copy(_chars, newChars, _chars.Length);
        return new Utf32Builder(newChars);
    }

    public static Utf32Text from_chars(uint[] chars)
    {
        validate_utf32(chars);
        return new Utf32Text(chars);
    }

    public static Utf32Text from_chars_unchecked(uint[] chars)
    {
        return new Utf32Text(chars);
    }

    public static Utf32Text from_string(string str)
    {
        var chars = new uint[str.Length];
        for (var i = 0; i < str.Length; i++)
            chars[i] = str[i];
        return new Utf32Text(chars);
    }

    public static Utf32Text from_utf8(Utf8Text utf8)
    {
        var str = utf8.ToString();
        return from_string(str);
    }

    public static Utf32Text from_utf16(Utf16Text utf16)
    {
        var chars = new uint[utf16.char_count];
        var span = utf16.as_span();
        for (var i = 0; i < span.Length; i++)
            chars[i] = span[i];
        return new Utf32Text(chars);
    }

    public static Utf32Text from_ascii(AsciiText ascii)
    {
        var chars = new uint[ascii.char_count];
        var span = ascii.as_span();
        for (var i = 0; i < span.Length; i++)
            chars[i] = span[i];
        return new Utf32Text(chars);
    }

    public Utf32View view(TextSpan span)
    {
        validate_span(span);
        return new Utf32View(this, span);
    }

    public Utf32Text slice(TextSpan span)
    {
        validate_span(span);
        var newChars = new uint[span.length];
        Array.Copy(_chars, span.offset, newChars, 0, span.length);
        return new Utf32Text(newChars);
    }

    public ReadOnlySpan<uint> as_span()
    {
        return _chars.AsSpan();
    }

    public ReadOnlySpan<uint> as_span(TextSpan span)
    {
        validate_span(span);
        return _chars.AsSpan(span.offset, span.length);
    }

    public string to_system_string()
    {
        var chars = new char[_chars.Length];
        for (var i = 0; i < _chars.Length; i++)
            if (_chars[i] > 0xFFFF)
                chars[i] = '\uFFFD';
            else
                chars[i] = (char)_chars[i];

        return new string(chars);
    }

    public Utf8Text to_utf8()
    {
        return Utf8Text.from_string(to_system_string());
    }

    public Utf16Text to_utf16()
    {
        return Utf16Text.from_string(to_system_string());
    }

    public override string ToString()
    {
        return to_system_string();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Utf32Text other) return false;

        return _chars.AsSpan().SequenceEqual(other._chars);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var c in _chars) hash.Add((int)c);

        return hash.ToHashCode();
    }

    public static bool operator ==(Utf32Text left, Utf32Text right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Utf32Text left, Utf32Text right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Utf32Text(string str)
    {
        return from_string(str);
    }

    private static int count_occurrences(ReadOnlySpan<uint> haystack, ReadOnlySpan<uint> needle)
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

    private Utf32Text trim_whitespace(bool trimStart, bool trimEnd)
    {
        var span = as_span();
        var start = 0;
        var end = span.Length;

        if (trimStart)
            while (start < end && is_whitespace_code_point(span[start]))
                start++;

        if (trimEnd)
            while (end > start && is_whitespace_code_point(span[end - 1]))
                end--;

        return slice(new TextSpan(start, end - start));
    }

    private static bool is_whitespace_code_point(uint c)
    {
        return c is ' ' or '\t' or '\n' or '\r';
    }

    private static void validate_utf32(uint[] chars)
    {
        foreach (var c in chars)
            if (c > 0x10FFFF)
                throw new ArgumentException("Invalid UTF-32 codepoint");
    }

    private void validate_span(TextSpan span)
    {
        if (span.offset < 0) throw new ArgumentOutOfRangeException(nameof(span));

        if (span.offset + span.length > _chars.Length) throw new ArgumentOutOfRangeException(nameof(span));
    }

    #region 字符串查询操�?

    /// <summary>
    ///     检查是否包含指定的码位序列
    /// </summary>
    /// <param name="needle">要搜索的码位序列</param>
    public bool contains(ReadOnlySpan<uint> needle)
    {
        return as_span().IndexOf(needle) >= 0;
    }

    /// <summary>
    ///     检查是否包含指定的 Utf32Text
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    public bool contains(Utf32Text needle)
    {
        return as_span().IndexOf(needle.as_span()) >= 0;
    }

    /// <summary>
    ///     查找指定码位序列的首次出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的码位序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(ReadOnlySpan<uint> needle)
    {
        return as_span().IndexOf(needle);
    }

    /// <summary>
    ///     查找指定 Utf32Text 的首次出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(Utf32Text needle)
    {
        return as_span().IndexOf(needle.as_span());
    }

    /// <summary>
    ///     查找指定码位序列的最后出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的码位序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(ReadOnlySpan<uint> needle)
    {
        return as_span().LastIndexOf(needle);
    }

    /// <summary>
    ///     查找指定 Utf32Text 的最后出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(Utf32Text needle)
    {
        return as_span().LastIndexOf(needle.as_span());
    }

    /// <summary>
    ///     检查是否以指定的码位序列开�?    ///
    /// </summary>
    /// <param name="prefix">前缀码位序列</param>
    public bool starts_with(ReadOnlySpan<uint> prefix)
    {
        return as_span().StartsWith(prefix);
    }

    /// <summary>
    ///     检查是否以指定�?Utf32Text 开�?    ///
    /// </summary>
    /// <param name="prefix">前缀文本</param>
    public bool starts_with(Utf32Text prefix)
    {
        return as_span().StartsWith(prefix.as_span());
    }

    /// <summary>
    ///     检查是否以指定的码位序列结�?    ///
    /// </summary>
    /// <param name="suffix">后缀码位序列</param>
    public bool ends_with(ReadOnlySpan<uint> suffix)
    {
        return as_span().EndsWith(suffix);
    }

    /// <summary>
    ///     检查是否以指定�?Utf32Text 结尾
    /// </summary>
    /// <param name="suffix">后缀文本</param>
    public bool ends_with(Utf32Text suffix)
    {
        return as_span().EndsWith(suffix.as_span());
    }

    #endregion

    #region 字符串修剪和替换操作

    /// <summary>
    ///     移除首尾空白字符
    /// </summary>
    public Utf32Text trim()
    {
        return trim_whitespace(true, true);
    }

    /// <summary>
    ///     移除首部空白字符
    /// </summary>
    public Utf32Text trim_start()
    {
        return trim_whitespace(true, false);
    }

    /// <summary>
    ///     移除尾部空白字符
    /// </summary>
    public Utf32Text trim_end()
    {
        return trim_whitespace(false, true);
    }

    /// <summary>
    ///     替换所有出现的目标码位序列
    /// </summary>
    /// <param name="oldValue">要替换的码位序列</param>
    /// <param name="newValue">替换为的码位序列</param>
    public Utf32Text replace(ReadOnlySpan<uint> oldValue, ReadOnlySpan<uint> newValue)
    {
        if (oldValue.IsEmpty) throw new ArgumentException("oldValue 不能为空", nameof(oldValue));

        var span = as_span();
        var count = count_occurrences(span, oldValue);

        if (count == 0) return this;

        var newSize = span.Length - count * oldValue.Length + count * newValue.Length;
        var result = new uint[newSize];
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

        return new Utf32Text(result);
    }

    #endregion
}