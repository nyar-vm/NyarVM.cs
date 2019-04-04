using System.Text;
using Std.Data.Text.Diagnostics;

namespace Oak.Wat;

/// <summary>
///     WAT 词法分析器
///     将 WAT 源码文本分解为 Token 流
/// </summary>
public sealed class WatLexer
{
    private static readonly HashSet<string> Keywords =
    [
        "module", "func", "import", "export", "memory", "table",
        "global", "data", "type", "param", "result", "local",
        "block", "loop", "if", "then", "else", "end", "start",
        "elem", "offset", "item", "mut"
    ];

    private static readonly HashSet<string> ValueTypes = ["i32", "i64", "f32", "f64", "funcref", "externref", "v128"];


    /// <summary>
    ///     不含点号的 WAT 操作码集合
    /// </summary>
    private static readonly HashSet<string> PlainOpcodes =
    [
        "call", "call_indirect", "nop", "drop", "select",
        "return", "unreachable", "br", "br_if", "br_table"
    ];

    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = "";


    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">WAT 源码文本。</param>
    /// <param name="diagnostics">诊断接收器。</param>
    /// <returns>Token 列表。</returns>
    public IReadOnlyList<WatToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<WatToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new WatToken(WatTokenType.Eof, "", _line, _column));
        return tokens;
    }


    /// <summary>
    ///     扫描下一个 Token
    /// </summary>
    private WatToken? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;

        if (Peek() == ';' && Peek(1) == ';') return ScanComment(startLine, startColumn);

        if (Peek() == '"') return ScanString(startLine, startColumn);

        if (Peek() is '(' or ')')
        {
            var c = Advance();
            return new WatToken(WatTokenType.Punctuation, c.ToString(), startLine, startColumn);
        }

        if (Peek() == '$') return ScanIdentifier(startLine, startColumn);

        if (char.IsDigit(Peek()) || (Peek() == '-' && Peek(1) is not '\0' &&
                                     (char.IsDigit(Peek(1)) || Peek(1) == 'i' || Peek(1) == 'f' || Peek(1) == 'n')))
            return ScanNumber(startLine, startColumn);

        if (char.IsLetter(Peek()) || Peek() == '_') return ScanWord(startLine, startColumn);

        _diagnostics?.report_warning("", default, "WAT1001", $"未识别的字符 '{Peek()}'");
        Advance();
        return null;
    }


    /// <summary>
    ///     扫描注释（;; 行注释或 (; 块注释 ;)
    /// </summary>
    private WatToken ScanComment(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        sb.Append(Advance());

        if (sb.ToString() == "(;")
        {
            var depth = 1;
            while (!IsAtEnd() && depth > 0)
                if (Peek() == '(' && Peek(1) == ';')
                {
                    depth++;
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
                else if (Peek() == ';' && Peek(1) == ')')
                {
                    depth--;
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
                else
                {
                    sb.Append(Advance());
                }
        }
        else
        {
            while (!IsAtEnd() && Peek() != '\n') sb.Append(Advance());
        }

        return new WatToken(WatTokenType.Comment, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描字符串字面量
    /// </summary>
    private WatToken ScanString(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd() && Peek() != '"')
            if (Peek() == '\\')
            {
                sb.Append(Advance());
                if (!IsAtEnd()) sb.Append(Advance());
            }
            else
            {
                sb.Append(Advance());
            }

        if (!IsAtEnd()) sb.Append(Advance());

        return new WatToken(WatTokenType.StringLiteral, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描标识符（$name 形式）
    /// </summary>
    private WatToken ScanIdentifier(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() is '_' or '.' or '-' or '>' or '<'))
            sb.Append(Advance());

        return new WatToken(WatTokenType.Identifier, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描数字
    /// </summary>
    private WatToken ScanNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        if (Peek() == '-') sb.Append(Advance());

        while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() is '.' or 'x' or 'e' or 'E' or '+' or '-' or 'p' or 'P' ||
                              IsHexDigit(Peek()))) sb.Append(Advance());

        return new WatToken(WatTokenType.Number, sb.ToString(), startLine, startColumn);
    }


    /// <summary>
    ///     扫描单词（关键字、操作码、类型名）
    /// </summary>
    private WatToken ScanWord(int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() is '_' or '.' or '/')) sb.Append(Advance());

        var value = sb.ToString();

        if (Keywords.Contains(value)) return new WatToken(WatTokenType.Keyword, value, startLine, startColumn);

        if (ValueTypes.Contains(value)) return new WatToken(WatTokenType.ValueType, value, startLine, startColumn);

        if (PlainOpcodes.Contains(value) || IsWatOpcode(value))
            return new WatToken(WatTokenType.Opcode, value, startLine, startColumn);

        return new WatToken(WatTokenType.Identifier, value, startLine, startColumn);
    }

    #region 辅助方法

    private static bool IsWatOpcode(string value)
    {
        return value.Contains('.') && !value.StartsWith(".") && char.IsLetter(value[0]);
    }

    private static bool IsHexDigit(char c)
    {
        return c is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 0;
            }

            Advance();
        }
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _source.Length ? _source[index] : '\0';
    }

    private char Advance()
    {
        var c = _source[_position++];
        _column++;
        return c;
    }

    #endregion
}