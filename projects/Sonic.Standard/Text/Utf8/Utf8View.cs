using Core.Text;

namespace Std.Text.Utf8;

public readonly struct Utf8View : ITextView
{
    public readonly Utf8Text text;
    public readonly TextSpan span;

    public Utf8View(Utf8Text text, TextSpan span)
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
    public int byte_length => span.length;

    /// <summary>
    ///     获取视图长度
    /// </summary>
    int ITextView.length => byte_length;

    /// <summary>
    ///     获取是否为空
    /// </summary>
    public bool is_empty => span.is_empty;

    public Utf8Text as_text()
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

    public Utf8View view(TextSpan span)
    {
        var absStart = this.span.offset + span.offset;
        var absEnd = absStart + span.length;
        return new Utf8View(text, new TextSpan(absStart, absEnd - absStart));
    }

    public Utf8Text slice(TextSpan span)
    {
        return view(span).as_text();
    }

    public ReadOnlySpan<byte> as_span()
    {
        return text.as_span(span);
    }

    public override string ToString()
    {
        return SonicEncoding.decode_utf8(as_span());
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Utf8View other) return false;

        return text == other.text && span == other.span;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(text, span);
    }

    public static bool operator ==(Utf8View left, Utf8View right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Utf8View left, Utf8View right)
    {
        return !left.Equals(right);
    }
}