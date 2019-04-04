namespace Std.Data.Text.Syntax;

/// <summary>
///     文本位置
/// </summary>
public readonly record struct TextPosition
{
    public TextPosition(int position, int line, int column)
    {
        this.position = position;
        this.line = line;
        this.column = column;
    }

    /// <summary>
    ///     字符位置
    /// </summary>
    public int position { get; init; }

    /// <summary>
    ///     行号
    /// </summary>
    public int line { get; init; }

    /// <summary>
    ///     列号
    /// </summary>
    public int column { get; init; }
}