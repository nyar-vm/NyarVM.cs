using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Msil;

/// <summary>
///     ILASM 词法分析器
///     将 ILASM 源码文本分解为 Token 流
/// </summary>
public sealed class MsilLexer
{
    private static readonly HashSet<string> _access_modifiers =
    [
        "public", "private", "family", "assembly", "famandassem",
        "famorassem", "privatescope", "static", "instance", "virtual",
        "abstract", "sealed", "final", "specialname", "rtspecialname",
        "initonly", "literal", "notserialized", "value", "enum",
        "interface", "sequential", "auto", "explicit", "ansi",
        "unicode", "autochar", "beforefieldinit", "cil", "managed",
        "unmanaged", "forwardref", "preservesig", "internalcall",
        "synchronized", "noinlining", "aggressiveinlining", "optil",
        "nooptimization"
    ];

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = "";


    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">ILASM 源码文本。</param>
    /// <param name="diagnostics">诊断接收器。</param>
    /// <returns>Token 列表。</returns>
    public IReadOnlyList<MsilToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<MsilToken>();

        while (!is_at_end())
        {
            skip_whitespace();
            if (is_at_end()) break;

            var token = scan_token();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new MsilToken(MsilTokenType.eof, "", _line, _column));
        return tokens;
    }


    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private MsilToken? scan_token()
    {
        var startLine = _line;
        var startColumn = _column;

        if (peek() == '/' && peek(1) == '/') return scan_comment(startLine, startColumn);

        if (peek() == '"') return scan_string(startLine, startColumn);

        if (peek() == '.')
        {
            if (peek(1) is not '\0' && char.IsLetter(peek(1))) return scan_directive_or_opcode(startLine, startColumn);

            advance();
            return null;
        }

        if (peek() is '{' or '}' or '(' or ')' or ';' or ':' or ',' or '[' or ']' or '=')
        {
            var c = advance();
            return new MsilToken(MsilTokenType.punctuation, c.ToString(), startLine, startColumn);
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
    private MsilToken scan_comment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && peek() != '\n') sb.Append(advance());

        return new MsilToken(MsilTokenType.comment, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描字符串字面量
    /// </summary>
    private MsilToken scan_string(int startLine, int startColumn)
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

        return new MsilToken(MsilTokenType.string_literal, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描指令关键字或操作码（. 开头）
    /// </summary>
    private MsilToken scan_directive_or_opcode(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() is '.' or '_')) sb.Append(advance());

        var value = sb.ToString();

        if (value.StartsWith(".")) return new MsilToken(MsilTokenType.directive, value, startLine, startColumn);

        return new MsilToken(MsilTokenType.opcode, value, startLine, startColumn);
    }


    /// <summary>
    ///     扫描数字
    /// </summary>
    private MsilToken scan_number(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (peek() == '-') sb.Append(advance());

        while (!is_at_end() && (char.IsDigit(peek()) || peek() == 'x' || is_hex_digit(peek()))) sb.Append(advance());

        return new MsilToken(MsilTokenType.number, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描单词
    /// </summary>
    private MsilToken scan_word(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (peek() == 'I' && peek(1) == 'L' && peek(2) == '_')
        {
            sb.Append(advance());
            sb.Append(advance());
            sb.Append(advance());

            while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());

            if (!is_at_end() && peek() == ':') sb.Append(advance());

            return new MsilToken(MsilTokenType.il_label, sb.ToString(), startLine, startColumn);
        }

        while (!is_at_end() && is_identifier_part(peek())) sb.Append(advance());

        var value = sb.ToString();

        if (_access_modifiers.Contains(value))
            return new MsilToken(MsilTokenType.access_modifier, value, startLine, startColumn);

        if (is_msil_opcode(value)) return new MsilToken(MsilTokenType.opcode, value, startLine, startColumn);

        if (is_type_reference(value)) return new MsilToken(MsilTokenType.type_reference, value, startLine, startColumn);

        return new MsilToken(MsilTokenType.identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool is_msil_opcode(string value)
    {
        return value.Contains('.') && !value.StartsWith(".") && char.IsLetter(value[0]);
    }

    private static bool is_type_reference(string value)
    {
        return value is "void" or "bool" or "int8" or "int16" or "int32" or "int64"
            or "unsigned.int8" or "unsigned.int16" or "unsigned.int32" or "unsigned.int64"
            or "float32" or "float64" or "string" or "object" or "native" or "typedref";
    }

    private static bool is_identifier_start(char c)
    {
        return char.IsLetter(c) || c is '_' or '$' or '<' or '>';
    }

    private static bool is_identifier_part(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$' or '<' or '>' or '.' or '`';
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
