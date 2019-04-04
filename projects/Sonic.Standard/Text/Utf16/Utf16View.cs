using Core.Text;

namespace Std.Text.Utf16;

public readonly struct Utf16View : ITextView
{
    public readonly Utf16Text text;
    public readonly TextSpan span;

    public Utf16View(Utf16Text text, TextSpan span)
    {
        this.text = text;
        this.span = span;
    }

    /// <summary>
    ///     获取视图的文本范围
    /// </summary>
    public TextSpan span_value => span;

    /// <summary>
    ///     获取字节长度
    /// </summary>
    public int byte_length => span.length * 2;

    /// <summary>
    ///     获取视图长度
    /// </summary>
    int ITextView.length => span.length;

    /// <summary>
    ///     获取是否为空
    /// </summary>
    public bool is_empty => span.is_empty;

    public Utf16Text as_text()
    {
        return text.slice(span);
    }

    /// <summary>
    ///     从指定位置切取指定长度的文本视图
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="length">切片长度</param>
    /// <returns>新的文本视图对应的文本</returns>
    IText ITextView.slice(int start, int length)
    {
        return slice(new TextSpan(start, length));
    }

    public Utf16View view(TextSpan span)
    {
        var absStart = this.span.offset + span.offset;
        var absEnd = absStart + span.length;
        return new Utf16View(text, new TextSpan(absStart, absEnd - absStart));
    }

    public Utf16Text slice(TextSpan span)
    {
        return view(span).as_text();
    }

    public ReadOnlySpan<ushort> as_span()
    {
        return text.as_span(span);
    }

    public string to_system_string()
    {
        var chars = new char[span.length];
        var textSpan = text.as_span(span);
        for (var i = 0; i < textSpan.Length; i++)
            chars[i] = (char)textSpan[i];
        return new string(chars);
    }

    public override string ToString()
    {
        return to_system_string();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Utf16View other) return false;

        return text == other.text && span == other.span;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(text, span);
    }

    public static bool operator ==(Utf16View left, Utf16View right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Utf16View left, Utf16View right)
    {
        return !left.Equals(right);
    }
}