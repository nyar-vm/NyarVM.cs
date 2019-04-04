using System.Text;

namespace Std.Data.Text.Verse.Lexer;


/// <summary>

///     Verse 词法分析器


/// </summary>
public sealed class VerseLexer : LexerBase
{
    private static readonly HashSet<string> Keywords =
    [
        new(StringComparer.Ordinal),
        "scene", "label", "jump", "call", "return",
        "if", "elif", "else", "endif",
        "set", "let", "var",
        "menu", "endmenu",
        "pause", "wait",
        "true", "false", "null",
        "narrator", "bg", "char", "fg", "sfx", "bgm", "voice"
    ];

    private static readonly HashSet<string> Operators =
    [
        new(StringComparer.Ordinal),
        "=", "==", "!=", "<", ">", "<=", ">=",
        "+", "-", "*", "/", "%",
        "+=", "-=", "*=", "/=",
        "&&", "||", "!"
    ];

    private static readonly HashSet<char> Delimiters = ['(', ')', '{', '}', ',', ';', ':'];

    public VerseLexer(DiagnosticSink? diagnostics = null)
    {
        Diagnostics = diagnostics;
    }

    
/// <summary>
    
///     将源代码转换为词法单元序列
    

/// </summary>
    public override IReadOnlyList<GreenLeafNode> Tokenize(string source)
    {
        Source = new StringSource(source);
        Reset();
        Diagnostics ??= new DiagnosticSink();

        var nodes = new List<GreenLeafNode>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd())
            {
                break;
            }

            var node = ScanNode();

            if (node is not null)
            {
                nodes.Add(node);
            }
        }

        nodes.Add(new GreenLeafNode(VerseNodeKind.Eof, 0, string.Empty));
        return nodes;
    }

    private GreenLeafNode? ScanNode()
    {

        var c = Peek();

        if (c == '@')
        {
            return ScanCommand();
        }

        if (c == '*')
        {
            var next = PeekNext();

            if (next == '_' || char.IsLetter(next))
            {
                return ScanLabel();
            }

            if (next == '=')
            {
                return ScanOperator();
            }

            Advance();
            return new GreenLeafNode(VerseNodeKind.ChoiceMarker, 1, "*");
        }

        if (c == '-')
        {
            var next = PeekNext();

            if (char.IsWhiteSpace(next) || next == '>' || next == '\0')
            {
                Advance();
                return new GreenLeafNode(VerseNodeKind.ChoiceMarker, 1, "-");
            }

            return ScanOperator();
        }

        if (c == '~')
        {
            return ScanNarration();
        }

        if (c is '"' or '\'')
        {
            return ScanString();
        }

        if (char.IsDigit(c))
        {
            return ScanNumber();
        }

        if (IsCJKLetter(c) || IsCJKPunctuation(c))
        {
            return ScanCJKText();
        }

        if (c == '_' || char.IsLetter(c))
        {
            return ScanIdentifierOrKeyword();
        }

        if (IsOperatorStart(c))
        {
            return ScanOperator();
        }

        if (Delimiters.Contains(c))
        {
            Advance();
            return new GreenLeafNode(VerseNodeKind.Delimiter, 1, c.ToString());
        }

        if (c == '.')
        {
            Advance();

            if (Peek() == '.')
            {
                Advance();
                return new GreenLeafNode(VerseNodeKind.Operator, 2, "..");
            }

            return new GreenLeafNode(VerseNodeKind.Delimiter, 1, ".");
        }

        if (IsCJKCharacter(c))
        {
            return ScanCJKText();
        }

        Advance();
        Diagnostics?.AddError(
            string.Empty,
            default,
            "VERSE1001",
            $"意外的字符 '{c}'");

        return null;
    }

    private static bool IsCJKCharacter(char c)
    {
        return IsCJKLetter(c) || IsCJKPunctuation(c);
    }

    private static bool IsCJKLetter(char c)
    {
        if (c < 0x2E80)
        {
            return false;
        }

        var cat = char.GetUnicodeCategory(c);
        return cat == System.Globalization.UnicodeCategory.OtherLetter;
    }

    private static bool IsCJKPunctuation(char c)
    {
        if (c < 0x2000)
        {
            return false;
        }

        var cat = char.GetUnicodeCategory(c);
        return cat is System.Globalization.UnicodeCategory.OtherPunctuation
            or System.Globalization.UnicodeCategory.InitialQuotePunctuation
            or System.Globalization.UnicodeCategory.FinalQuotePunctuation;
    }

    private GreenLeafNode ScanCJKText()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            var c = Peek();

            if (IsCJKLetter(c) || IsCJKPunctuation(c))
            {
                sb.Append(Advance());
            }
            else if (c == ' ')
            {
                sb.Append(Advance());
            }
            else
            {
                break;
            }
        }

        var text = sb.ToString().Trim();
        return new GreenLeafNode(VerseNodeKind.String, text.Length, text);
    }

    private GreenLeafNode ScanCommand()
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || (char.IsLetterOrDigit(Peek()) && !IsCJKLetter(Peek()) && !IsCJKPunctuation(Peek())))) sb.Append(Advance());

        var commandName = sb.ToString();

        return new GreenLeafNode(VerseNodeKind.CommandPrefix, commandName.Length, commandName);
    }

    private GreenLeafNode ScanLabel()
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || (char.IsLetterOrDigit(Peek()) && !IsCJKLetter(Peek()) && !IsCJKPunctuation(Peek())))) sb.Append(Advance());

        return new GreenLeafNode(VerseNodeKind.LabelMarker, sb.Length, sb.ToString());
    }

    private GreenLeafNode ScanNarration()
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r') sb.Append(Advance());

        var text = sb.ToString().Trim();
        return new GreenLeafNode(VerseNodeKind.String, text.Length, text);
    }

    private GreenLeafNode ScanString()
    {
        var quote = Advance();
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
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
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }

        if (IsAtEnd())
        {
            Diagnostics?.AddError(
                string.Empty,
                default,
                "VERSE1002",
                "未闭合的字符串字面量");
        }
        else
        {
            Advance();
        }

        var value = sb.ToString();
        return new GreenLeafNode(VerseNodeKind.String, value.Length, value);
    }

    private GreenLeafNode ScanNumber()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());

            while (!IsAtEnd() && char.IsDigit(Peek())) sb.Append(Advance());
        }

        if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F'))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();
        return new GreenLeafNode(VerseNodeKind.Number, value.Length, value);
    }

    private GreenLeafNode ScanIdentifierOrKeyword()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || (char.IsLetterOrDigit(Peek()) && !IsCJKLetter(Peek()) && !IsCJKPunctuation(Peek())))) sb.Append(Advance());

        var text = sb.ToString();

        if (Keywords.Contains(text))
        {
            if (text is "true" or "false" or "null")
            {
                return new GreenLeafNode(VerseNodeKind.Literal, text.Length, text);
            }

            return new GreenLeafNode(VerseNodeKind.Keyword, text.Length, text);
        }

        return new GreenLeafNode(VerseNodeKind.Identifier, text.Length, text);
    }

    private bool IsOperatorStart(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' => true,
            _ => false
        };
    }

    private GreenLeafNode ScanOperator()
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

        var op = sb.ToString();

        if (op is ":")
        {
            return new GreenLeafNode(VerseNodeKind.Punctuation, op.Length, op);
        }

        return new GreenLeafNode(VerseNodeKind.Operator, op.Length, op);
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var c = Peek();

            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    Advance();
                    break;
                case '#':
                    SkipLineComment();
                    break;
                case '/':
                    if (PeekNext() == '/')
                    {
                        SkipLineComment();
                    }
                    else if (PeekNext() == '*')
                    {
                        SkipBlockComment();
                    }
                    else
                    {
                        return;
                    }

                    break;
                default:
                    return;
            }
        }
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n') Advance();
    }

    private void SkipBlockComment()
    {
        Advance();
        Advance();

        var depth = 1;

        while (!IsAtEnd() && depth > 0)
            if (Peek() == '/' && PeekNext() == '*')
            {
                Advance();
                Advance();
                depth++;
            }
            else if (Peek() == '*' && PeekNext() == '/')
            {
                Advance();
                Advance();
                depth--;
            }
            else
            {
                Advance();
            }

        if (depth > 0)
        {
            Diagnostics?.AddWarning(
                string.Empty,
                default,
                "VERSE1003",
                "未闭合的块注释");
        }
    }
}
