namespace Std.Terminal;

/// <summary>
///     边框字符集
/// </summary>
internal sealed class BorderChars
{
    public char top_left { get; init; }
    public char top_right { get; init; }
    public char bottom_left { get; init; }
    public char bottom_right { get; init; }
    public char horizontal { get; init; }
    public char vertical { get; init; }

    internal static BorderChars get(BorderStyle style)
    {
        return style switch
        {
            BorderStyle.single => new BorderChars
            {
                top_left = '┌', top_right = '┐', bottom_left = '└', bottom_right = '┘', horizontal = '─', vertical = '│'
            },
            BorderStyle.@double => new BorderChars
            {
                top_left = '╔', top_right = '╗', bottom_left = '╚', bottom_right = '╝', horizontal = '═', vertical = '║'
            },
            BorderStyle.rounded => new BorderChars
            {
                top_left = '╭', top_right = '╮', bottom_left = '╰', bottom_right = '╯', horizontal = '─', vertical = '│'
            },
            BorderStyle.heavy => new BorderChars
            {
                top_left = '┏', top_right = '┓', bottom_left = '┗', bottom_right = '┛', horizontal = '━', vertical = '┃'
            },
            BorderStyle.dashed => new BorderChars
            {
                top_left = '┌', top_right = '┐', bottom_left = '└', bottom_right = '┘', horizontal = '╌', vertical = '╎'
            },
            _ => new BorderChars
            {
                top_left = '┌', top_right = '┐', bottom_left = '└', bottom_right = '┘', horizontal = '─', vertical = '│'
            }
        };
    }
}