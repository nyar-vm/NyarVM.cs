using System.Text;

namespace Std.Data.Text.CSharp;


/// <summary>

///     C# 语言词法分析器


/// </summary>
public sealed class CsLexer : LexerBase
{
    private static readonly HashSet<string> Keywords =
    [
        new(StringComparer.Ordinal),
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false",
        "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
        "async", "await", "dynamic", "from", "get", "global", "group", "into",
        "join", "let", "orderby", "partial", "remove", "select", "set", "value",
        "var", "where", "yield",
        "record", "init", "with", "and", "or", "not", "nint", "nuint",
        "required", "file", "scoped", "allows", "params"
    ];

    private static readonly HashSet<string> Operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "/", "%", "++", "--",
        "==", "!=", ">", "<", ">=", "<=",
        "&&", "||", "!",
        "&", "|", "^", "~", "<<", ">>>", ">>",
        "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=",
        ".", "??", "??=", "?", ":", "=>",
        "->", "??", "?.", "?[",
        "===", "!=="
    ];

    private static readonly HashSet<char> OperatorStarts =
    [
        '+', '-', '*', '/', '%', '&', '|', '^', '~',
        '<', '>', '=', '!', '?', ':', '.'
    ];

    public CsLexer(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    
/// <summary>
    
///     将源代码转换为词法单元序列（GreenLeafNode）
    

/// </summary>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        var csTokens = TokenizeAsCsTokens(source);
        return csTokens.Select(t => new GreenLeafNode(t.Kind, t.Text.Length, t.Text)).ToList();
    }

    
/// <summary>
    
///     将源代码转换为 C# 词法单元序列
    

/// </summary>
    public IReadOnlyList<CsToken> TokenizeAsCsTokens(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var tokens = new List<CsToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd())
            {
                break;
            }

            var c = Peek();

            if (c is '\n' or '\r')
            {
                AdvanceNewLine();
                tokens.Add(new CsToken(CsNodeKind.NewLine, "\n"));
                continue;
            }

            if (c == '#')
            {
                var pp = ScanPreprocessorText();
                if (pp is not null)
                {
                    tokens.Add(new CsToken(CsNodeKind.Preprocessor, pp));
                }

                continue;
            }

            if (c == '/' && PeekNext() == '/')
            {
                if (Peek(2) == '/')
                {
                    var doc = ScanDocCommentText();
                    if (doc is not null)
                    {
                        tokens.Add(new CsToken(CsNodeKind.Comment, doc));
                    }
                }
                else
                {
                    SkipLineComment();
                }

                continue;
            }

            if (c == '/' && PeekNext() == '*')
            {
                var comment = ScanBlockCommentText();
                if (comment is not null)
                {
                    tokens.Add(new CsToken(CsNodeKind.Comment, comment));
                }

                continue;
            }

            var (kind, text) = ScanTokenParts();

            if (kind is not null)
            {
                tokens.Add(new CsToken(kind.Value, text));
            }
        }

        tokens.Add(new CsToken(CsNodeKind.Eof, ""));
        return tokens;
    }

    private (NodeKind? Kind, string Text) ScanTokenParts()
    {
        var c = Peek();

        if (c == '"')
        {
            return (CsNodeKind.String, ScanStringText());
        }

        if (c == '\'' )
        {
            return (CsNodeKind.Char, ScanCharText());
        }

        if (c == '@' && PeekNext() == '"')
        {
            return (CsNodeKind.String, ScanVerbatimStringText());
        }

        if (c == '$' && PeekNext() == '"')
        {
            return (CsNodeKind.String, ScanInterpolatedStringText());
        }

        if (c == '$' && PeekNext() == '@' && Position + 2 < Source.Length && Source[Position + 2] == '"')
        {
            return (CsNodeKind.String, ScanInterpolatedVerbatimStringText());
        }

        if (char.IsDigit(c))
        {
            return (CsNodeKind.Number, ScanNumberText());
        }

        if (c == '_' || char.IsLetter(c))
        {
            return ScanIdentifierOrKeywordText();
        }

        if (OperatorStarts.Contains(c))
        {
            return (CsNodeKind.Operator, ScanOperatorText());
        }

        if (IsDelimiter(c)) { Advance(); return (CsNodeKind.Delimiter, c.ToString()); }
        Advance();
        Diagnostics?.AddError(
            string.Empty,
            default,
            "OCS1001",
            $"意外的字符 '{c}'");

        return (null, "");
    }

    private string ScanStringText()
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd())
                {
                    break;
                }

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '"' => '"', '0' => '\0',
                    'a' => '\a', 'b' => '\b', 'f' => '\f', 'v' => '\v',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd())
        {
            Advance();
        }

        return sb.ToString();
    }

    private string ScanVerbatimStringText()
    {
        Advance();
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (Peek() == '"')
            {
                if (PeekNext() == '"')
                {
                    Advance();
                    Advance();
                    sb.Append('"');
                }
                else
                {
                    Advance();
                    break;
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }

        return sb.ToString();
    }

    private string ScanInterpolatedStringText()
    {
        Advance();
        var sb = new StringBuilder();
        sb.Append("$\"");
        Advance();
        var depth = 0;

        while (!IsAtEnd())
        {
            if (Peek() == '{')
            {
                depth++;
                sb.Append(Advance());
            }
            else if (Peek() == '}')
            {
                if (depth > 0)
                {
                    depth--;
                    sb.Append(Advance());
                }
                else
                {
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
            }
            else if (Peek() == '"')
            {
                Advance();
                sb.Append('"');
                break;
            }
            else if (Peek() == '\\')
            {
                Advance();
                sb.Append('\\');
                if (!IsAtEnd())
                {
                    sb.Append(Advance());
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }

        return sb.ToString();
    }

    private string ScanInterpolatedVerbatimStringText()
    {
        Advance();
        Advance();
        Advance();
        var sb = new StringBuilder();
        sb.Append("$@\"");

        while (!IsAtEnd())
        {
            if (Peek() == '"')
            {
                if (PeekNext() == '"')
                {
                    Advance();
                    Advance();
                    sb.Append("\"\"");
                }
                else
                {
                    Advance();
                    sb.Append('"');
                    break;
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }

        return sb.ToString();
    }

    private string ScanCharText()
    {
        Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '\'')
        {
            if (Peek() == '\\')
            {
                Advance();
                if (IsAtEnd())
                {
                    break;
                }

                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '\'' => '\'', '0' => '\0',
                    'a' => '\a', 'b' => '\b', 'f' => '\f', 'v' => '\v',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd())
        {
            Advance();
        }

        return sb.ToString();
    }

    private string ScanNumberText()
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;

        if (Peek() == '0')
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == 'x' || Peek() == 'X')) { sb.Append(Advance()); isHex = true; }
            else if (!IsAtEnd() && (Peek() == 'b' || Peek() == 'B')) { sb.Append(Advance()); isBinary = true; }
        }

        if (isHex)
        {
            while (!IsAtEnd() && (char.IsDigit(Peek()) || (Peek() >= 'a' && Peek() <= 'f') || (Peek() >= 'A' && Peek() <= 'F')))
                sb.Append(Advance());
        }
        else if (isBinary)
        {
            while (!IsAtEnd() && (Peek() == '0' || Peek() == '1')) sb.Append(Advance());
        }
        else
        {
            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

            if (!IsAtEnd() && Peek() == '.' && PeekNext() != '.')
            {
                sb.Append(Advance());
                while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
            }
        }

        if (!isBinary && !IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        while (!IsAtEnd() && (Peek() == 'u' || Peek() == 'U' || Peek() == 'l' || Peek() == 'L'
                              || Peek() == 'f' || Peek() == 'F' || Peek() == 'd' || Peek() == 'D'
                              || Peek() == 'm' || Peek() == 'M'))
            sb.Append(Advance());

        return sb.ToString();
    }

    private (NodeKind Kind, string Text) ScanIdentifierOrKeywordText()
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek()))) sb.Append(Advance());
        var text = sb.ToString();
        return Keywords.Contains(text) ? (CsNodeKind.Keyword, text) : (CsNodeKind.Identifier, text);
    }

    private string ScanOperatorText()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());

        while (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();
            if (Operators.Contains(candidate))
            {
                sb.Append(Advance());
            }
            else
            {
                break;
            }
        }

        return sb.ToString();
    }

    private string? ScanPreprocessorText()
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\n')
        {
            if (Peek() == '\\' && PeekNext() == '\n')
            {
                sb.Append(Advance());
                sb.Append(Advance());
            }
            else
            {
                sb.Append(Advance());
            }
        }

        var value = sb.ToString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private string? ScanDocCommentText()
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && Peek() != '\n') sb.Append(Advance());
        var value = sb.ToString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private string? ScanBlockCommentText()
    {
        var sb = new StringBuilder();
        sb.Append(Advance());
        sb.Append(Advance());
        while (!IsAtEnd() && !(Peek() == '*' && PeekNext() == '/')) sb.Append(Advance());
        if (!IsAtEnd()) { sb.Append(Advance()); sb.Append(Advance()); }
        var value = sb.ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool IsDelimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ';';
    }

    private void AdvanceNewLine()
    {
        if (Peek() == '\r')
        {
            Advance();
        }

        if (Peek() == '\n')
        {
            Advance();
        }
    }

    private new void SkipWhitespace()
    {
        while (!IsAtEnd() && (Peek() == ' ' || Peek() == '\t' || Peek() == '\f' || Peek() == '\v')) Advance();
    }
}
