using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Javap;

/// <summary>
///     Javap 词法分析器
///     将 javap -c 输出文本分解为 Token 流
/// </summary>
public sealed class JvpLexer
{
    private static readonly HashSet<string> _access_modifiers =
    [
        "public", "private", "protected", "static", "final",
        "synchronized", "volatile", "transient", "native",
        "abstract", "strictfp", "enum", "interface"
    ];

    private static readonly HashSet<string> _type_keywords = ["class", "interface", "enum", "record", "module"];
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = "";


    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">javap -c 输出文本。</param>
    /// <param name="diagnostics">诊断接收器。</param>
    /// <returns>Token 列表。</returns>
    public IReadOnlyList<JvpToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<JvpToken>();

        while (!is_at_end())
        {
            skip_whitespace();
            if (is_at_end()) break;

            var token = scan_token();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new JvpToken(JvpTokenType.eof, "", _line, _column));
        return tokens;
    }


    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private JvpToken? scan_token()
    {
        var startLine = _line;
        var startColumn = _column;

        if (peek() == '/' && peek(1) == '/') return scan_comment(startLine, startColumn);

        if (peek() == '#') return scan_constant_pool_ref(startLine, startColumn);

        if (peek() is '{' or '}' or '(' or ')' or ';' or ':' or ',' or '.' or '[' or ']')
        {
            var c = advance();
            return new JvpToken(JvpTokenType.punctuation, c.ToString(), startLine, startColumn);
        }

        if (char.IsDigit(peek()) || (peek() == '-' && peek(1) is not '\0' && char.IsDigit(peek(1))))
            return scan_number(startLine, startColumn);

        if (is_identifier_start(peek())) return scan_word(startLine, startColumn);

        _diagnostics?.report_warning("", default, 1001, $"未识别的字符 '{peek()}'");
        advance();
        return null;
    }


    /// <summary>
    ///     扫描注释
    /// </summary>
    private JvpToken scan_comment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && peek() != '\n') sb.Append(advance());

        return new JvpToken(JvpTokenType.comment, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描常量池引用
    /// </summary>
    private JvpToken scan_constant_pool_ref(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());

        return new JvpToken(JvpTokenType.constant_pool_ref, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描数字
    /// </summary>
    private JvpToken scan_number(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (peek() == '-') sb.Append(advance());

        while (!is_at_end() && (char.IsDigit(peek()) || peek() == 'x' || is_hex_digit(peek()))) sb.Append(advance());

        return new JvpToken(JvpTokenType.number, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描单词（标识符、操作码、关键字等）
    /// </summary>
    private JvpToken scan_word(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && is_identifier_part(peek())) sb.Append(advance());

        var value = sb.ToString();

        if (_access_modifiers.Contains(value))
            return new JvpToken(JvpTokenType.access_modifier, value, startLine, startColumn);

        if (_type_keywords.Contains(value))
            return new JvpToken(JvpTokenType.type_keyword, value, startLine, startColumn);

        if (value == "Compiled") return new JvpToken(JvpTokenType.header_keyword, value, startLine, startColumn);

        if (value == "Code") return new JvpToken(JvpTokenType.section_marker, value, startLine, startColumn);

        if (is_jvm_opcode(value)) return new JvpToken(JvpTokenType.opcode, value, startLine, startColumn);

        return new JvpToken(JvpTokenType.identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool is_jvm_opcode(string value)
    {
        return (value.Length >= 2 && value.Contains('_')) || value is
            "nop" or "aconst_null" or "iconst_m1" or "return" or "areturn"
            or "ireturn" or "lreturn" or "freturn" or "dreturn" or "dup" or "pop"
            or "swap" or "iadd" or "isub" or "imul" or "idiv" or "ineg" or "iand"
            or "ior" or "ixor" or "ishl" or "ishr" or "iushr" or "ladd" or "lsub"
            or "lmul" or "ldiv" or "i2l" or "i2f" or "i2d" or "l2i" or "l2f" or "l2d"
            or "f2i" or "f2l" or "f2d" or "d2i" or "d2l" or "d2f" or "lcmp" or "new"
            or "athrow" or "monitorenter" or "monitorexit" or "arraylength"
            or "checkcast" or "instanceof" or "ifnull" or "ifnonnull";
    }

    private static bool is_identifier_start(char c)
    {
        return char.IsLetter(c) || c is '_' or '$' or '<' or '>';
    }

    private static bool is_identifier_part(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$' or '<' or '>' or '.';
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
