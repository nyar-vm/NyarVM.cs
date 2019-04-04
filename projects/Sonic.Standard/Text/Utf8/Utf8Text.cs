using Core.Text;

namespace Std.Text.Utf8;

public class Utf8Text : IText
{
    private readonly byte[] _bytes;

    private Utf8Text(byte[] bytes)
    {
        _bytes = bytes;
    }

    /// <summary>
    ///     获取空文本实例
    /// </summary>
    public static Utf8Text empty => new([]);

    /// <summary>
    ///     获取字节长度
    /// </summary>
    public int byte_length => _bytes.Length;

    public int char_count
    {
        get
        {
            var count = 0;
            var i = 0;

            while (i < _bytes.Length)
            {
                var b = _bytes[i];
                if (b < 0x80)
                {
                    i++;
                }
                else if (b < 0xE0)
                {
                    i += 2;
                }
                else if (b < 0xF0)
                {
                    i += 3;
                }
                else
                {
                    i += 4;
                }

                count++;
            }

            return count;
        }
    }

    public bool is_empty => _bytes.Length == 0;

    public TextSpan span => new(0, _bytes.Length);

    /// <summary>
    ///     获取文本长度
    /// </summary>
    int IText.length => byte_length;

    /// <summary>
    ///     获取文本编码
    /// </summary>
    TextEncoding IText.encoding => TextEncoding.Utf8;

    public Utf8Builder to_builder()
    {
        var newBytes = new byte[_bytes.Length];
        Array.Copy(_bytes, newBytes, _bytes.Length);
        return new Utf8Builder(newBytes);
    }

    public static Utf8Text from_bytes(byte[] bytes)
    {
        validate_utf8(bytes);
        return new Utf8Text(bytes);
    }

    public static Utf8Text from_bytes_unchecked(byte[] bytes)
    {
        return new Utf8Text(bytes);
    }

    public static Utf8Text from_string(string str)
    {
        var byteCount = SonicEncoding.utf8_byte_count(str);
        var bytes = new byte[byteCount];
        SonicEncoding.encode_utf8(str, bytes);
        return new Utf8Text(bytes);
    }

    public Utf8View view(TextSpan span)
    {
        validate_span(span);
        return new Utf8View(this, span);
    }

    public Utf8Text slice(TextSpan span)
    {
        validate_span(span);
        var newBytes = new byte[span.length];
        Array.Copy(_bytes, span.offset, newBytes, 0, span.length);
        return new Utf8Text(newBytes);
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

    public override string ToString()
    {
        return SonicEncoding.decode_utf8(_bytes);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Utf8Text other)
        {
            return false;
        }

        return _bytes.AsSpan().SequenceEqual(other._bytes);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var b in _bytes)
        {
            hash.Add(b);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(Utf8Text left, Utf8Text right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Utf8Text left, Utf8Text right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Utf8Text(string str)
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

            if (found < 0)
            {
                break;
            }

            count++;
            pos += found + needle.Length;
        }

        return count;
    }

    private Utf8Text trim_whitespace(bool trimStart, bool trimEnd)
    {
        var span = as_span();
        var start = 0;
        var end = span.Length;

        if (trimStart)
        {
            while (start < end && is_whitespace_byte(span[start]))
            {
                start++;
            }
        }

        if (trimEnd)
        {
            while (end > start && is_whitespace_byte(span[end - 1]))
            {
                end--;
            }
        }

        return slice(new TextSpan(start, end - start));
    }

    private static bool is_whitespace_byte(byte b)
    {
        return b is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
    }

    private static void validate_utf8(byte[] bytes)
    {
        var i = 0;

        while (i < bytes.Length)
        {
            var b = bytes[i];
            if (b < 0x80)
            {
                i++;
            }
            else if (b < 0xE0)
            {
                if (i + 1 >= bytes.Length || (bytes[i + 1] & 0xC0) != 0x80)
                {
                    throw new ArgumentException("Invalid UTF-8 sequence");
                }

                i += 2;
            }
            else if (b < 0xF0)
            {
                if (i + 2 >= bytes.Length || (bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80)
                {
                    throw new ArgumentException("Invalid UTF-8 sequence");
                }

                i += 3;
            }
            else
            {
                if (i + 3 >= bytes.Length || (bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80 ||
                    (bytes[i + 3] & 0xC0) != 0x80)
                {
                    throw new ArgumentException("Invalid UTF-8 sequence");
                }

                i += 4;
            }
        }
    }

    private void validate_span(TextSpan span)
    {
        if (span.offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(span));
        }

        if (span.offset + span.length > _bytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(span));
        }
    }

    #region 字符串查询操作

    /// <summary>
    ///     检查是否包含指定的字节序列
    /// </summary>
    /// <param name="needle">要搜索的字节序列</param>
    public bool contains(ReadOnlySpan<byte> needle)
    {
        return as_span().IndexOf(needle) >= 0;
    }

    /// <summary>
    ///     检查是否包含指定的 Utf8Text
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    public bool contains(Utf8Text needle)
    {
        return as_span().IndexOf(needle.as_span()) >= 0;
    }

    /// <summary>
    ///     查找指定字节序列的首次出现位置
    /// </summary>
    /// <param name="needle">要搜索的字节序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(ReadOnlySpan<byte> needle)
    {
        return as_span().IndexOf(needle);
    }

    /// <summary>
    ///     查找指定 Utf8Text 的首次出现位置
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int index_of(Utf8Text needle)
    {
        return as_span().IndexOf(needle.as_span());
    }

    /// <summary>
    ///     查找指定字节序列的最后出现位置
    /// </summary>
    /// <param name="needle">要搜索的字节序列</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(ReadOnlySpan<byte> needle)
    {
        return as_span().LastIndexOf(needle);
    }

    /// <summary>
    ///     查找指定 Utf8Text 的最后出现位置
    /// </summary>
    /// <param name="needle">要搜索的文本</param>
    /// <returns>找到则返回起始索引，否则返回 -1</returns>
    public int last_index_of(Utf8Text needle)
    {
        return as_span().LastIndexOf(needle.as_span());
    }

    /// <summary>
    ///     检查是否以指定的字节序列开头
    /// </summary>
    /// <param name="prefix">前缀字节序列</param>
    public bool starts_with(ReadOnlySpan<byte> prefix)
    {
        return as_span().StartsWith(prefix);
    }

    /// <summary>
    ///     检查是否以指定的 Utf8Text 开头
    /// </summary>
    /// <param name="prefix">前缀文本</param>
    public bool starts_with(Utf8Text prefix)
    {
        return as_span().StartsWith(prefix.as_span());
    }

    /// <summary>
    ///     检查是否以指定的字节序列结尾
    /// </summary>
    /// <param name="suffix">后缀字节序列</param>
    public bool ends_with(ReadOnlySpan<byte> suffix)
    {
        return as_span().EndsWith(suffix);
    }

    /// <summary>
    ///     检查是否以指定的 Utf8Text 结尾
    /// </summary>
    /// <param name="suffix">后缀文本</param>
    public bool ends_with(Utf8Text suffix)
    {
        return as_span().EndsWith(suffix.as_span());
    }

    #endregion

    #region 字符串修剪与替换

    /// <summary>
    ///     移除首尾空白字符
    /// </summary>
    public Utf8Text trim()
    {
        return trim_whitespace(true, true);
    }

    /// <summary>
    ///     移除首部空白字符
    /// </summary>
    public Utf8Text trim_start()
    {
        return trim_whitespace(true, false);
    }

    /// <summary>
    ///     移除尾部空白字符
    /// </summary>
    public Utf8Text trim_end()
    {
        return trim_whitespace(false, true);
    }

    /// <summary>
    ///     替换所有出现的目标字节序列
    /// </summary>
    /// <param name="oldValue">要替换的字节序列</param>
    /// <param name="newValue">替换为的字节序列</param>
    public Utf8Text replace(ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        if (oldValue.IsEmpty)
        {
            throw new ArgumentException("oldValue 不能为空", nameof(oldValue));
        }

        var span = as_span();
        var count = count_occurrences(span, oldValue);

        if (count == 0)
        {
            return this;
        }

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

        return new Utf8Text(result);
    }

    #endregion

    #region 字符串拆分与大小写转换

    /// <summary>
    ///     按指定的字节分隔符拆分字符串
    /// </summary>
    /// <param name="separator">
    ///     分隔符字节序列
    /// </param>
    /// <returns>拆分后的文本片段列表</returns>
    public List<Utf8Text> split(ReadOnlySpan<byte> separator)
    {
        var result = new List<Utf8Text>();

        if (separator.IsEmpty)
        {
            result.Add(this);
            return result;
        }

        var span = as_span();
        var start = 0;

        while (start < span.Length)
        {
            var remaining = span[start..];
            var found = remaining.IndexOf(separator);

            if (found < 0)
            {
                result.Add(slice(new TextSpan(start, span.Length - start)));
                break;
            }

            result.Add(slice(new TextSpan(start, found)));
            start += found + separator.Length;
        }

        return result;
    }

    /// <summary>
    ///     按指定的 Utf8Text 分隔符拆分字符串
    /// </summary>
    /// <param name="separator">
    ///     分隔符文本
    /// </param>
    /// <returns>拆分后的文本片段列表</returns>
    public List<Utf8Text> split(Utf8Text separator)
    {
        return split(separator.as_span());
    }

    /// <summary>
    ///     将字符串中的 ASCII 大写字母转换为小写
    /// </summary>
    public Utf8Text to_lower()
    {
        var span = as_span();
        var changed = false;
        var result = new byte[span.Length];
        span.CopyTo(result);

        for (var i = 0; i < result.Length; i++)
        {
            if (result[i] >= (byte)'A' && result[i] <= (byte)'Z')
            {
                result[i] = (byte)(result[i] + 32);
                changed = true;
            }
        }

        if (!changed)
        {
            return this;
        }

        return new Utf8Text(result);
    }

    /// <summary>
    ///     将字符串中的 ASCII 小写字母转换为大写
    /// </summary>
    public Utf8Text to_upper()
    {
        var span = as_span();
        var changed = false;
        var result = new byte[span.Length];
        span.CopyTo(result);

        for (var i = 0; i < result.Length; i++)
        {
            if (result[i] >= (byte)'a' && result[i] <= (byte)'z')
            {
                result[i] = (byte)(result[i] - 32);
                changed = true;
            }
        }

        if (!changed)
        {
            return this;
        }

        return new Utf8Text(result);
    }

    #endregion

    #region 静态拼接方法

    /// <summary>
    ///     拼接两个 Utf8Text
    /// </summary>
    public static Utf8Text concat(Utf8Text a, Utf8Text b)
    {
        var result = new byte[a.byte_length + b.byte_length];
        a.as_span().CopyTo(result);
        b.as_span().CopyTo(result.AsSpan(a.byte_length));
        return new Utf8Text(result);
    }

    /// <summary>
    ///     使用分隔符拼接多个 Utf8Text
    /// </summary>
    /// <param name="separator">
    ///     分隔符
    /// </param>
    /// <param name="parts">要拼接的文本片段列表</param>
    public static Utf8Text join(Utf8Text separator, List<Utf8Text> parts)
    {
        if (parts.Count == 0)
        {
            return empty;
        }

        var totalLength = 0;

        for (var i = 0; i < parts.Count; i++)
        {
            totalLength += parts[i].byte_length;
        }

        totalLength += separator.byte_length * (parts.Count - 1);

        var result = new byte[totalLength];
        var pos = 0;

        for (var i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                separator.as_span().CopyTo(result.AsSpan(pos));
                pos += separator.byte_length;
            }

            parts[i].as_span().CopyTo(result.AsSpan(pos));
            pos += parts[i].byte_length;
        }

        return new Utf8Text(result);
    }

    #endregion
}
