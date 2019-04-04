using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin 词法分析器
///     将 Jasmin 源码文本分解为 Token 流
/// </summary>
public sealed class JmLexer
{
    private static readonly HashSet<string> _directives =
    [
        ".class", ".super", ".implements", ".interface", ".field",
        ".method", ".end", ".limit", ".line", ".var", ".throws",
        ".catch", ".source", ".version", ".attribute", ".debug"
    ];

    private static readonly HashSet<string> _access_modifiers =
    [
        "public", "private", "protected", "static", "final",
        "synchronized", "volatile", "transient", "native",
        "abstract", "strictfp", "enum", "annotation", "interface"
    ];

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = "";


    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">Jasmin 源码文本。</param>
    /// <param name="diagnostics">诊断接收器。</param>
    /// <returns>Token 列表。</returns>
    public IReadOnlyList<JmToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<JmToken>();

        while (!is_at_end())
        {
            skip_whitespace();
            if (is_at_end()) break;

            var token = scan_token();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new JmToken(JmTokenType.eof, "", _line, _column));
        return tokens;
    }


    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private JmToken? scan_token()
    {
        var startLine = _line;
        var startColumn = _column;

        if (peek() == ';') return scan_comment(startLine, startColumn);

        if (peek() == '"') return scan_string(startLine, startColumn);

        if (peek() == ':')
        {
            advance();
            return new JmToken(JmTokenType.colon, ":", startLine, startColumn);
        }

        if (peek() == '=')
        {
            advance();
            return new JmToken(JmTokenType.equals, "=", startLine, startColumn);
        }

        if (peek() == '.') return scan_directive(startLine, startColumn);

        if (char.IsDigit(peek()) || (peek() == '-' && peek(1) is not '\0' && char.IsDigit(peek(1))))
            return scan_number(startLine, startColumn);

        if (is_identifier_start(peek())) return scan_identifier_or_opcode(startLine, startColumn);

        _diagnostics?.report_warning("", default, 1001, $"未识别的字符 '{peek()}'");
        advance();
        return null;
    }


    /// <summary>
    ///     扫描注释
    /// </summary>
    private JmToken scan_comment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && peek() != '\n') sb.Append(advance());

        return new JmToken(JmTokenType.comment, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描字符串字面量
    /// </summary>
    private JmToken scan_string(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        while (!is_at_end() && peek() != '"')
            if (peek() == '\\')
            {
                sb.Append(advance());
                if (!is_at_end()) sb.Append(advance());
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end()) sb.Append(advance());

        return new JmToken(JmTokenType.string_literal, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描指令关键字
    /// </summary>
    private JmToken scan_directive(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_')) sb.Append(advance());

        return new JmToken(JmTokenType.directive, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描数字
    /// </summary>
    private JmToken scan_number(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (peek() == '-') sb.Append(advance());

        while (!is_at_end() && (char.IsDigit(peek()) || peek() == 'x' || is_hex_digit(peek()))) sb.Append(advance());

        return new JmToken(JmTokenType.number, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描标识符或操作码
    /// </summary>
    private JmToken scan_identifier_or_opcode(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && is_identifier_part(peek())) sb.Append(advance());

        var value = sb.ToString();

        if (value.EndsWith(':')) return new JmToken(JmTokenType.label, value[..^1], startLine, startColumn);

        if (_access_modifiers.Contains(value))
            return new JmToken(JmTokenType.access_modifier, value, startLine, startColumn);

        if (is_opcode(value)) return new JmToken(JmTokenType.opcode, value, startLine, startColumn);

        if (is_descriptor(value)) return new JmToken(JmTokenType.descriptor, value, startLine, startColumn);

        return new JmToken(JmTokenType.identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool is_opcode(string value)
    {
        return value.Length >= 2 && !value.StartsWith('.') && !char.IsDigit(value[0]) &&
               !_access_modifiers.Contains(value);
    }

    private static bool is_descriptor(string value)
    {
        return value is "V" or "Z" or "B" or "S" or "I" or "J" or "F" or "D" or "C"
               || (value.StartsWith("L") && value.EndsWith(";"))
               || value.StartsWith("[");
    }

    private static bool is_identifier_start(char c)
    {
        return char.IsLetter(c) || c is '_' or '/' or '$' or '<' or '>';
    }

    private static bool is_identifier_part(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '/' or '$' or '<' or '>' or '-' or ':';
    }

    private static bool is_hex_digit(char c)
    {
        return c is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    private void skip_whitespace()
    {
        while (!is_at_end() && char.IsWhiteSpace(peek()))
        {
            if (peek() == '\n')
            {
                _line++;
                _column = 0;
            }

            advance();
        }
    }

    private bool is_at_end()
    {
        return _position >= _source.Length;
    }

    private char peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _source.Length ? _source[index] : '\0';
    }

    private char advance()
    {
        var c = _source[_position++];
        _column++;
        return c;
    }

    #endregion
}
