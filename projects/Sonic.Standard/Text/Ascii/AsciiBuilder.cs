using Core.Text;
using Std.Math;
using Std.Text.Utf8;

namespace Std.Text.Ascii;

public struct AsciiBuilder : ITextBuilder
{
    private byte[] _bytes;

    public AsciiBuilder(int capacity)
    {
        _bytes = new byte[SonicMath.max(capacity, 16)];
        length = 0;
    }

    internal AsciiBuilder(byte[] bytes)
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

    public void append(byte b)
    {
        if (b > 127) throw new ArgumentException("Non-ASCII byte value");

        ensure_capacity(length + 1);
        _bytes[length++] = b;
    }

    public void append(char c)
    {
        if (c > 127) throw new ArgumentException("Non-ASCII character");

        ensure_capacity(length + 1);
        _bytes[length++] = (byte)c;
    }

    public void append(string str)
    {
        ensure_capacity(length + str.Length);
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] > 127) throw new ArgumentException("String contains non-ASCII characters");

            _bytes[length++] = (byte)str[i];
        }
    }

    public void append(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
            if (b > 127)
                throw new ArgumentException("Non-ASCII byte value");

        ensure_capacity(length + bytes.Length);
        bytes.CopyTo(_bytes.AsSpan(length));
        length += bytes.Length;
    }

    public void append(AsciiText text)
    {
        append(text.as_span());
    }

    public void append(AsciiView view)
    {
        append(view.as_span());
    }

    public void append(Utf8Text text)
    {
        var ascii = AsciiText.from_utf8(text);
        append(ascii);
    }

    public void concat(AsciiBuilder other)
    {
        foreach (var b in other._bytes.AsSpan(0, other.length))
            if (b > 127)
                throw new ArgumentException("Non-ASCII byte value");

        ensure_capacity(length + other.length);
        other._bytes.AsSpan(0, other.length).CopyTo(_bytes.AsSpan(length));
        length += other.length;
    }

    public void clear()
    {
        length = 0;
    }

    public AsciiText build()
    {
        if (length == _bytes.Length) return AsciiText.from_bytes_unchecked(_bytes);

        var result = new byte[length];
        Array.Copy(_bytes, result, length);
        return AsciiText.from_bytes_unchecked(result);
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

    public CText build_c_text()
    {
        ensure_capacity(length + 1);
        _bytes[length++] = 0;
        return CText.from_bytes_unchecked(_bytes);
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
