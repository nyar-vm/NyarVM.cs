using Core.Text;
using Std.Text.Ascii;
using Std.Text.Utf8;

namespace Std.Text.Utf16;

public class Utf16Text : IText
{
    private readonly ushort[] _chars;

    private Utf16Text(ushort[] chars)
    {
        _chars = chars;
    }

    /// <summary>
    ///     获取空文本实例
    /// </summary>
    public static Utf16Text empty => new([]);

    /// <summary>
    ///     获取字节长度
    /// </summary>
    public int byte_length => _chars.Length * 2;

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
    TextEncoding IText.encoding => TextEncoding.Utf16;

    public Utf16Builder to_builder()
    {
        var newChars = new ushort[_chars.Length];
        Array.Copy(_chars, newChars, _chars.Length);
        return new Utf16Builder(newChars);
    }

    public static Utf16Text from_chars(ushort[] chars)
    {
        validate_utf16(chars);
        return new Utf16Text(chars);
    }

    public static Utf16Text from_chars_unchecked(ushort[] chars)
    {
        return new Utf16Text(chars);
    }

    public static Utf16Text from_string(string str)
    {
        var chars = new ushort[str.Length];
        for (var i = 0; i < str.Length; i++)
            chars[i] = str[i];
        return new Utf16Text(chars);
    }

    public static Utf16Text from_utf8(Utf8Text utf8)
    {
        var str = utf8.ToString();
        return from_string(str);
    }

    public static Utf16Text from_ascii(AsciiText ascii)
    {
        var chars = new ushort[ascii.char_count];
        var span = ascii.as_span();
        for (var i = 0; i < span.Length; i++)
            chars[i] = span[i];
        return new Utf16Text(chars);
    }

    public Utf16View view(TextSpan span)
    {
        validate_span(span);
        return new Utf16View(this, span);
    }

    public Utf16Text slice(TextSpan span)
    {
        validate_span(span);
        var newChars = new ushort[span.length];
        Array.Copy(_chars, span.offset, newChars, 0, span.length);
        return new Utf16Text(newChars);
    }

    public ReadOnlySpan<ushort> as_span()
    {
        return _chars.AsSpan();
    }

    public ReadOnlySpan<ushort> as_span(TextSpan span)
    {
        validate_span(span);
        return _chars.AsSpan(span.offset, span.length);
    }

    public string to_system_string()
    {
        var chars = new char[_chars.Length];
        for (var i = 0; i < _chars.Length; i++)
            chars[i] = (char)_chars[i];
        return new string(chars);
    }

    public Utf8Text to_utf8()
    {
        return Utf8Text.from_string(to_system_string());
    }

    public override string ToString()
    {
        return to_system_string();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Utf16Text other) return false;

        return _chars.AsSpan().SequenceEqual(other._chars);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var c in _chars) hash.Add(c);

        return hash.ToHashCode();
    }

    public static bool operator ==(Utf16Text left, Utf16Text right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Utf16Text left, Utf16Text right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Utf16Text(string str)
    {
        return from_string(str);
    }

    private static int count_occurrences(ReadOnlySpan<ushort> haystack, ReadOnlySpan<ushort> needle)
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

    private Utf16Text trim_whitespace(bool trimStart, bool trimEnd)
    {
        var span = as_span();
        var start = 0;
        var end = span.Length;

        if (trimStart)
            while (start < end && is_whitespace_char(span[start]))
                start++;

        if (trimEnd)
            while (end > start && is_whitespace_char(span[end - 1]))
                end--;

        return slice(new TextSpan(start, end - start));
    }

    private static bool is_whitespace_char(ushort c)
    {
        return c is ' ' or '\t' or '\n' or '\r';
    }

    private static void validate_utf16(ushort[] chars)
    {
        foreach (var c in chars)
            if (c is >= 0xD800 and <= 0xDFFF)
                throw new ArgumentException("Invalid UTF-16 surrogate pair");
    }

    private void validate_span(TextSpan span)
    {
        if (span.offset < 0) throw new ArgumentOutOfRangeException(nameof(span));

        if (span.offset + span.length > _chars.Length) throw new ArgumentOutOfRangeException(nameof(span));
    }

    #region 字符串查询操�?

    /// <summary>
    ///     检查是否包含指定的字符序列
    /// </summary>
    /// <param name="needle">要搜索的字符序列</param>
    public bool contains(ReadOnlySpan<ushort> needle)
    {
        return as_span().IndexOf(needle) >= 0;
    }

    /// <summary>
    ///     检查是否包含指定的 Utf16Text
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    public bool contains(Utf16Text needle)
    {
        return as_span().IndexOf(needle.as_span()) >= 0;
    }

    /// <summary>
    ///     查找指定字符序列的首次出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的字符序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(ReadOnlySpan<ushort> needle)
    {
        return as_span().IndexOf(needle);
    }

    /// <summary>
    ///     查找指定 Utf16Text 的首次出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(Utf16Text needle)
    {
        return as_span().IndexOf(needle.as_span());
    }

    /// <summary>
    ///     查找指定字符序列的最后出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的字符序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(ReadOnlySpan<ushort> needle)
    {
        return as_span().LastIndexOf(needle);
    }

    /// <summary>
    ///     查找指定 Utf16Text 的最后出现位�?    ///
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(Utf16Text needle)
    {
        return as_span().LastIndexOf(needle.as_span());
    }

    /// <summary>
    ///     检查是否以指定的字符序列开�?    ///
    /// </summary>
    /// <param name="prefix">前缀字符序列</param>
    public bool starts_with(ReadOnlySpan<ushort> prefix)
    {
        return as_span().StartsWith(prefix);
    }

    /// <summary>
    ///     检查是否以指定�?Utf16Text 开�?    ///
    /// </summary>
    /// <param name="prefix">前缀文本</param>
    public bool starts_with(Utf16Text prefix)
    {
        return as_span().StartsWith(prefix.as_span());
    }

    /// <summary>
    ///     检查是否以指定的字符序列结�?    ///
    /// </summary>
    /// <param name="suffix">后缀字符序列</param>
    public bool ends_with(ReadOnlySpan<ushort> suffix)
    {
        return as_span().EndsWith(suffix);
    }

    /// <summary>
    ///     检查是否以指定�?Utf16Text 结尾
    /// </summary>
    /// <param name="suffix">后缀文本</param>
    public bool ends_with(Utf16Text suffix)
    {
        return as_span().EndsWith(suffix.as_span());
    }

    #endregion

    #region 字符串修剪和替换操作

    /// <summary>
    ///     移除首尾空白字符
    /// </summary>
    public Utf16Text trim()
    {
        return trim_whitespace(true, true);
    }

    /// <summary>
    ///     移除首部空白字符
    /// </summary>
    public Utf16Text trim_start()
    {
        return trim_whitespace(true, false);
    }

    /// <summary>
    ///     移除尾部空白字符
    /// </summary>
    public Utf16Text trim_end()
    {
        return trim_whitespace(false, true);
    }

    /// <summary>
    ///     替换所有出现的目标字符序列
    /// </summary>
    /// <param name="oldValue">要替换的字符序列</param>
    /// <param name="newValue">替换为的字符序列</param>
    public Utf16Text replace(ReadOnlySpan<ushort> oldValue, ReadOnlySpan<ushort> newValue)
    {
        if (oldValue.IsEmpty) throw new ArgumentException("oldValue 不能为空", nameof(oldValue));

        var span = as_span();
        var count = count_occurrences(span, oldValue);

        if (count == 0) return this;

        var newSize = span.Length - count * oldValue.Length + count * newValue.Length;
        var result = new ushort[newSize];
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

        return new Utf16Text(result);
    }

    #endregion
}