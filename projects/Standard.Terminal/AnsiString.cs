namespace Std.Terminal;

/// <summary>
///     ANSI 安全字符串，同时维护纯文本和包含 ANSI 转义序列的完整表示�?///
/// </summary>
public readonly struct AnsiString : IEquatable<AnsiString>
{
    /// <summary>
    ///     获取纯文本内容（不含 ANSI 转义序列）�?    ///
    /// </summary>
    public string plain_text { get; }

    /// <summary>
    ///     获取包含 ANSI 转义序列的完整字符串�?    ///
    /// </summary>
    public string ansi_sequence { get; }

    /// <summary>
    ///     获取空的 <see cref="AnsiString" /> 实例�?    ///
    /// </summary>
    public static AnsiString empty { get; } = new(string.Empty, string.Empty);

    /// <summary>
    ///     初始�?<see cref="AnsiString" /> 的新实例�?    ///
    /// </summary>
    /// <param name="plainText">
    ///     纯文本内容�?/param>
    ///     <param name="ansiSequence">包含 ANSI 转义序列的字符串�?/param>
    public AnsiString(string plainText, string ansiSequence)
    {
        plain_text = plainText ?? string.Empty;
        ansi_sequence = ansiSequence ?? string.Empty;
    }

    /// <summary>
    ///     从纯文本创建 <see cref="AnsiString" />，ANSI 序列与纯文本相同�?    ///
    /// </summary>
    /// <param name="text">纯文本内容�?/param>
    public static AnsiString plain(string text)
    {
        return new AnsiString(text, text);
    }

    /// <summary>
    ///     拼接两个 <see cref="AnsiString" /> 实例�?    ///
    /// </summary>
    /// <param name="left">
    ///     左侧字符串�?/param>
    ///     <param name="right">
    ///         右侧字符串�?/param>
    ///         <returns>拼接后的新实例�?/returns>
    public static AnsiString operator +(AnsiString left, AnsiString right)
    {
        return new AnsiString(left.plain_text + right.plain_text, left.ansi_sequence + right.ansi_sequence);
    }

    /// <summary>
    ///     从字符串隐式转换�?<see cref="AnsiString" />�?    ///
    /// </summary>
    /// <param name="text">源字符串�?/param>
    public static implicit operator AnsiString(string text)
    {
        return plain(text);
    }

    /// <summary>
    ///     指示当前实例是否等于另一�?<see cref="AnsiString" />�?    ///
    /// </summary>
    public bool Equals(AnsiString other)
    {
        return plain_text == other.plain_text && ansi_sequence == other.ansi_sequence;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is AnsiString other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(plain_text, ansi_sequence);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ansi_sequence;
    }
}