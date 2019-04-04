using Core.Text;
using Std.Math;

namespace Std.Text.Utf8;

public struct Utf8Builder : ITextBuilder
{
    private byte[] _bytes;

    public Utf8Builder(int capacity)
    {
        _bytes = new byte[SonicMath.max(capacity, 16)];
        length = 0;
    }

    internal Utf8Builder(byte[] bytes)
    {
        _bytes = bytes;
        length = bytes.Length;
    }

    /// <summary>
    ///     获取当前长度
    /// </summary>
    public int length { get; private set; }

    /// <summary>
    ///     获取容量
    /// </summary>
    public int capacity => _bytes.Length;

    /// <summary>
    ///     获取是否为空
    /// </summary>
    public bool is_empty => length == 0;

    public ReadOnlySpan<byte> as_span()
    {
        return _bytes.AsSpan(0, length);
    }

    public void append(char c)
    {
        if (c < 0x80)
        {
            ensure_capacity(length + 1);
            _bytes[length++] = (byte)c;
        }
        else if (c < 0x800)
        {
            ensure_capacity(length + 2);
            _bytes[length++] = (byte)(0xC0 | (c >> 6));
            _bytes[length++] = (byte)(0x80 | (c & 0x3F));
        }
        else if (c < 0x10000)
        {
            ensure_capacity(length + 3);
            _bytes[length++] = (byte)(0xE0 | (c >> 12));
            _bytes[length++] = (byte)(0x80 | ((c >> 6) & 0x3F));
            _bytes[length++] = (byte)(0x80 | (c & 0x3F));
        }
        else
        {
            ensure_capacity(length + 4);
            _bytes[length++] = (byte)(0xF0 | (c >> 18));
            _bytes[length++] = (byte)(0x80 | ((c >> 12) & 0x3F));
            _bytes[length++] = (byte)(0x80 | ((c >> 6) & 0x3F));
            _bytes[length++] = (byte)(0x80 | (c & 0x3F));
        }
    }

    public void append(string str)
    {
        var byteCount = SonicEncoding.utf8_byte_count(str);
        ensure_capacity(length + byteCount);
        SonicEncoding.encode_utf8(str, _bytes.AsSpan(length));
        length += byteCount;
    }

    public void append(ReadOnlySpan<byte> bytes)
    {
        ensure_capacity(length + bytes.Length);
        bytes.CopyTo(_bytes.AsSpan(length));
        length += bytes.Length;
    }

    public void append(Utf8Text text)
    {
        var span = text.as_span();
        ensure_capacity(length + span.Length);
        span.CopyTo(_bytes.AsSpan(length));
        length += span.Length;
    }

    public void append(Utf8View view)
    {
        var span = view.as_span();
        ensure_capacity(length + span.Length);
        span.CopyTo(_bytes.AsSpan(length));
        length += span.Length;
    }

    public void concat(Utf8Builder other)
    {
        ensure_capacity(length + other.length);
        other._bytes.AsSpan(0, other.length).CopyTo(_bytes.AsSpan(length));
        length += other.length;
    }

    public void clear()
    {
        length = 0;
    }

    public Utf8Text build()
    {
        if (length == _bytes.Length) return Utf8Text.from_bytes_unchecked(_bytes);

        var result = new byte[length];
        Array.Copy(_bytes, result, length);
        return Utf8Text.from_bytes_unchecked(result);
    }

    /// <summary>
    ///     追加文本
    /// </summary>
    /// <param name="text">要追加的文本</param>
    /// <returns>当前构建器实例</returns>
    ITextBuilder ITextBuilder.append(string text)
    {
        append(text);
        return this;
    }

    /// <summary>
    ///     构建最终的不变文本
    /// </summary>
    /// <returns>构建完成的文本</returns>
    IText ITextBuilder.build()
    {
        return build();
    }

    private void ensure_capacity(int minCapacity)
    {
        if (_bytes.Length >= minCapacity) return;

        var newCapacity = SonicMath.max(_bytes.Length * 2, minCapacity);
        var newBytes = new byte[newCapacity];
        Array.Copy(_bytes, newBytes, length);
        _bytes = newBytes;
    }
}