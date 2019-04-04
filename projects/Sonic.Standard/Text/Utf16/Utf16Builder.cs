using Core.Text;
using Std.Math;
using Std.Text.Utf8;

namespace Std.Text.Utf16;

public struct Utf16Builder : ITextBuilder
{
    private ushort[] _chars;

    public Utf16Builder(int capacity)
    {
        _chars = new ushort[SonicMath.max(capacity, 16)];
        length = 0;
    }

    internal Utf16Builder(ushort[] chars)
    {
        _chars = chars;
        length = chars.Length;
    }

    /// <summary>
    ///     获取当前长度
    /// </summary>
    public int length { get; private set; }

    /// <summary>
    ///     获取容量
    /// </summary>
    public int capacity => _chars.Length;

    /// <summary>
    ///     获取是否为空
    /// </summary>
    public bool is_empty => length == 0;

    public ReadOnlySpan<ushort> as_span()
    {
        return _chars.AsSpan(0, length);
    }

    public void append(ushort c)
    {
        ensure_capacity(length + 1);
        _chars[length++] = c;
    }

    public void append(char c)
    {
        append((ushort)c);
    }

    public void append(string str)
    {
        ensure_capacity(length + str.Length);
        for (var i = 0; i < str.Length; i++)
            _chars[length++] = str[i];
    }

    public void append(ReadOnlySpan<ushort> chars)
    {
        ensure_capacity(length + chars.Length);
        chars.CopyTo(_chars.AsSpan(length));
        length += chars.Length;
    }

    public void append(Utf16Text text)
    {
        var span = text.as_span();
        append(span);
    }

    public void append(Utf16View view)
    {
        var span = view.as_span();
        append(span);
    }

    public void append(Utf8Text text)
    {
        var utf16 = Utf16Text.from_utf8(text);
        append(utf16);
    }

    public void concat(Utf16Builder other)
    {
        ensure_capacity(length + other.length);
        other._chars.AsSpan(0, other.length).CopyTo(_chars.AsSpan(length));
        length += other.length;
    }

    public void clear()
    {
        length = 0;
    }

    public Utf16Text build()
    {
        if (length == _chars.Length) return Utf16Text.from_chars_unchecked(_chars);

        var result = new ushort[length];
        Array.Copy(_chars, result, length);
        return Utf16Text.from_chars_unchecked(result);
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
        if (_chars.Length >= minCapacity) return;

        var newCapacity = SonicMath.max(_chars.Length * 2, minCapacity);
        var newChars = new ushort[newCapacity];
        Array.Copy(_chars, newChars, length);
        _chars = newChars;
    }
}