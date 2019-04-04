using System.Text;

namespace Std.Data.Text.Syntax;

/// <summary>
///     文本读取器，提供通用的基于位置的文本扫描功能
/// </summary>
public sealed class TextReader
{
    public TextReader(string source)
    {
        this.source = source;
        position = 0;
        line = 1;
        column = 1;
    }

    /// <summary>
    ///     源代码内容
    /// </summary>
    public string source { get; }

    /// <summary>
    ///     当前位置
    /// </summary>
    public int position { get; private set; }

    /// <summary>
    ///     当前行号（从 1 开始）
    /// </summary>
    public int line { get; private set; }

    /// <summary>
    ///     当前列号（从 1 开始）
    /// </summary>
    public int column { get; private set; }

    /// <summary>
    ///     是否已到达末尾
    /// </summary>
    public bool is_at_end => position >= source.Length;

    /// <summary>
    ///     剩余字符数
    /// </summary>
    public int remaining => source.Length - position;

    /// <summary>
    ///     查看当前字符
    /// </summary>
    public char peek()
    {
        return is_at_end ? '\0' : source[position];
    }

    /// <summary>
    ///     查看指定偏移处的字符
    /// </summary>
    public char peek(int offset)
    {
        var index = position + offset;
        return index >= source.Length ? '\0' : source[index];
    }

    /// <summary>
    ///     前进一个字符并返回
    /// </summary>
    public char advance()
    {
        if (is_at_end) return '\0';

        var c = source[position];
        position++;

        if (c == '\n')
        {
            line++;
            column = 1;
        }
        else
        {
            column++;
        }

        return c;
    }

    /// <summary>
    ///     尝试匹配指定字符
    /// </summary>
    public bool match(char expected)
    {
        if (peek() != expected) return false;

        advance();
        return true;
    }

    /// <summary>
    ///     尝试匹配指定字符串
    /// </summary>
    public bool match(string expected)
    {
        if (position + expected.Length > source.Length) return false;

        if (source.Substring(position, expected.Length) != expected) return false;

        for (var i = 0; i < expected.Length; i++) advance();

        return true;
    }

    /// <summary>
    ///     跳过空白字符
    /// </summary>
    public void skip_whitespace()
    {
        while (!is_at_end && char.IsWhiteSpace(peek())) advance();
    }

    /// <summary>
    ///     跳过空白字符和注释（行注释以 # 开头，块注释为 &lt;# #&gt;）
    /// </summary>
    public void skip_whitespace_and_comments()
    {
        while (!is_at_end)
            if (char.IsWhiteSpace(peek()))
                skip_whitespace();
            else if (peek() == '#' && peek(1) == '?')
                break;
            else if (peek() == '#')
                skip_line_comment();
            else if (peek() == '<' && peek(1) == '#')
                skip_block_comment();
            else
                break;
    }

    /// <summary>
    ///     跳过行注释
    /// </summary>
    public void skip_line_comment()
    {
        while (!is_at_end && peek() != '\n') advance();
    }

    /// <summary>
    ///     跳过块注释
    /// </summary>
    public void skip_block_comment()
    {
        advance();
        advance();

        var depth = 1;

        while (!is_at_end && depth > 0)
            if (peek() == '<' && peek(1) == '#')
            {
                advance();
                advance();
                depth++;
            }
            else if (peek() == '#' && peek(1) == '>')
            {
                advance();
                advance();
                depth--;
            }
            else
            {
                advance();
            }
    }

    /// <summary>
    ///     读取标识符
    /// </summary>
    public string read_identifier()
    {
        var start = position;

        while (!is_at_end && is_identifier_part(peek())) advance();

        return source[start..position];
    }

    /// <summary>
    ///     读取字符串直到指定字符
    /// </summary>
    public string read_until(char terminator)
    {
        var start = position;

        while (!is_at_end && peek() != terminator) advance();

        return source[start..position];
    }

    /// <summary>
    ///     读取字符串直到指定条件成立
    /// </summary>
    public string read_while(Func<char, bool> predicate)
    {
        var start = position;

        while (!is_at_end && predicate(peek())) advance();

        return source[start..position];
    }

    /// <summary>
    ///     读取带转义的字符串
    /// </summary>
    public string read_escaped_string(char quote)
    {
        var sb = new StringBuilder();

        while (!is_at_end && peek() != quote)
            if (peek() == '\\')
            {
                advance();
                if (is_at_end) break;

                var escaped = advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end && peek() == quote) advance();

        return sb.ToString();
    }

    /// <summary>
    ///     保存当前位置
    /// </summary>
    public TextPosition save_position()
    {
        return new TextPosition(position, line, column);
    }

    /// <summary>
    ///     恢复到指定位置
    /// </summary>
    public void restore_position(TextPosition position)
    {
        this.position = position.position;
        line = position.line;
        column = position.column;
    }

    /// <summary>
    ///     提取指定范围的子字符串
    /// </summary>
    public string slice(int start, int length)
    {
        return source.Substring(start, System.Math.Min(length, source.Length - start));
    }

    private static bool is_identifier_part(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}