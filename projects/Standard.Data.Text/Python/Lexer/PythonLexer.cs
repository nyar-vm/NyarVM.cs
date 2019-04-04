using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Lexing;
using Std.Data.Text.Python.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Python.Lexer;

/// <summary>
///     Python 词法分析器，基于 Oak.Core 的 LexerBase 实现
/// </summary>
public sealed class PythonLexer : LexerBase
{
    private static readonly HashSet<string> _keywords =
    [
        new(StringComparer.Ordinal),
        "False", "None", "True", "and", "as", "assert", "async", "await",
        "break", "class", "continue", "def", "del", "elif", "else", "except",
        "finally", "for", "from", "global", "if", "import", "in", "is",
        "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try",
        "while", "with", "yield"
    ];

    private static readonly HashSet<string> _operators =
    [
        new(StringComparer.Ordinal),
        "+", "-", "*", "**", "/", "//", "%", "@",
        "<<", ">>", "&", "|", "^", "~",
        "<", ">", "<=", ">=", "==", "!=",
        "=", "+=", "-=", "*=", "/=", "//=", "%=", "@=",
        "&=", "|=", "^=", ">>=", "<<=", "**="
    ];

    /// <summary>
    ///     创建 Python 词法分析器
    /// </summary>
    public PythonLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> tokenize(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<GreenLeafNode>();
        var indentStack = new Stack<int>();
        indentStack.Push(0);

        var atLineStart = true;
        var pendingDedents = new List<GreenLeafNode>();

        while (!is_at_end())
        {
            if (atLineStart)
            {
                var indent = 0;
                while (!is_at_end() && (peek() == ' ' || peek() == '\t'))
                {
                    indent++;
                    advance();
                }

                if (is_at_end() || peek() == '\n' || peek() == '\r')
                {
                    if (!is_at_end())
                    {
                        advance_new_line();
                    }

                    continue;
                }

                if (peek() == '#')
                {
                    skip_line_comment();
                    continue;
                }

                var currentIndent = indentStack.Peek();
                if (indent > currentIndent)
                {
                    indentStack.Push(indent);
                    tokens.Add(new GreenLeafNode(PythonNodeKind.indent, 0, string.Empty));
                }
                else if (indent < currentIndent)
                {
                    while (indentStack.Count > 1 && indentStack.Peek() > indent)
                    {
                        indentStack.Pop();
                        pendingDedents.Add(new GreenLeafNode(PythonNodeKind.dedent, 0, string.Empty));
                    }

                    if (indentStack.Peek() != indent)
                    {
                        _diagnostics?.report_error(
                            string.Empty,
                            default,
                            "NPY1001",
                            "缩进不一致");
                    }
                }

                atLineStart = false;
            }

            foreach (var dedent in pendingDedents)
            {
                tokens.Add(dedent);
            }

            pendingDedents.Clear();

            if (is_at_end())
            {
                break;
            }

            skip_python_whitespace();

            if (is_at_end())
            {
                break;
            }

            var c = peek();

            if (c is '\n' or '\r')
            {
                advance_new_line();
                tokens.Add(new GreenLeafNode(PythonNodeKind.new_line, 1, "\n"));
                atLineStart = true;
                continue;
            }

            if (c == '#')
            {
                skip_line_comment();
                continue;
            }

            var token = scan_token();
            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        while (indentStack.Count > 1)
        {
            indentStack.Pop();
            tokens.Add(new GreenLeafNode(PythonNodeKind.dedent, 0, string.Empty));
        }

        tokens.Add(new GreenLeafNode(PythonNodeKind.eof, 0, string.Empty));
        return tokens;
    }

    /// <summary>
    ///     直接生成 GreenLeafNode 列表的词法分析方法
    /// </summary>
    public IReadOnlyList<GreenLeafNode> tokenize_to_green(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var greenNodes = new List<GreenLeafNode>();
        var indentStack = new Stack<int>();
        indentStack.Push(0);

        var atLineStart = true;
        var pendingDedents = new List<GreenLeafNode>();

        while (!is_at_end())
        {
            if (atLineStart)
            {
                var indent = 0;
                while (!is_at_end() && (peek() == ' ' || peek() == '\t'))
                {
                    indent++;
                    advance();
                }

                if (is_at_end() || peek() == '\n' || peek() == '\r')
                {
                    continue;
                }

                if (peek() == '#')
                {
                    skip_line_comment();
                    continue;
                }

                var currentIndent = indentStack.Peek();
                if (indent > currentIndent)
                {
                    indentStack.Push(indent);
                    greenNodes.Add(new GreenLeafNode(PythonNodeKind.indent, 0, string.Empty));
                }
                else if (indent < currentIndent)
                {
                    while (indentStack.Count > 1 && indentStack.Peek() > indent)
                    {
                        indentStack.Pop();
                        pendingDedents.Add(new GreenLeafNode(PythonNodeKind.dedent, 0, string.Empty));
                    }

                    if (indentStack.Peek() != indent)
                    {
                        _diagnostics?.report_error(
                            string.Empty,
                            default,
                            "NPY1001",
                            "缩进不一致");
                    }
                }

                atLineStart = false;
            }

            foreach (var dedent in pendingDedents)
            {
                greenNodes.Add(dedent);
            }

            pendingDedents.Clear();

            if (is_at_end())
            {
                break;
            }

            skip_python_whitespace();

            if (is_at_end())
            {
                break;
            }

            var c = peek();

            if (c is '\n' or '\r')
            {
                advance_new_line();
                greenNodes.Add(new GreenLeafNode(PythonNodeKind.new_line, 1, "\n"));
                atLineStart = true;
                continue;
            }

            if (c == '#')
            {
                skip_line_comment();
                continue;
            }

            var node = scan_green_node();
            if (node is not null)
            {
                greenNodes.Add(node);
            }
        }

        while (indentStack.Count > 1)
        {
            indentStack.Pop();
            greenNodes.Add(new GreenLeafNode(PythonNodeKind.dedent, 0, string.Empty));
        }

        greenNodes.Add(new GreenLeafNode(PythonNodeKind.eof, 0, string.Empty));
        return greenNodes;
    }

    #region 扫描方法

    /// <summary>
    ///     扫描单个 GreenLeafNode
    /// </summary>
    private GreenLeafNode? scan_green_node()
    {
        var c = peek();

        if (c is '"' or '\'')
        {
            return scan_string_green();
        }

        if (char.IsDigit(c))
        {
            return scan_number_green();
        }

        if (c == '_' || char.IsLetter(c))
        {
            return scan_identifier_or_keyword_green();
        }

        if (is_operator_start(c))
        {
            return scan_operator_green();
        }

        if (is_delimiter(c))
        {
            var value = c.ToString();
            advance();
            return new GreenLeafNode(PythonNodeKind.delimiter, value.Length, value);
        }

        advance();
        _diagnostics?.report_error(
            string.Empty,
            default,
            "NPY1002",
            $"意外的字符 '{c}'");

        return null;
    }

    /// <summary>
    ///     扫描字符串字面量为 GreenLeafNode
    /// </summary>
    private GreenLeafNode scan_string_green()
    {
        var quote = advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != quote)
        {
            if (peek() == '\\')
            {
                advance();
                if (is_at_end())
                {
                    break;
                }

                var escaped = advance();
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
                sb.Append(advance());
            }
        }

        if (!is_at_end())
        {
            advance();
        }

        var value = sb.ToString();
        return new GreenLeafNode(PythonNodeKind.@string, value.Length + 2, value);
    }

    /// <summary>
    ///     扫描数字字面量为 GreenLeafNode
    /// </summary>
    private GreenLeafNode scan_number_green()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && char.IsDigit(peek()))
        {
            sb.Append(advance());
        }

        if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
        {
            sb.Append(advance());

            while (!is_at_end() && char.IsDigit(peek()))
            {
                sb.Append(advance());
            }
        }

        if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
        {
            sb.Append(advance());

            if (!is_at_end() && (peek() == '+' || peek() == '-'))
            {
                sb.Append(advance());
            }

            while (!is_at_end() && char.IsDigit(peek()))
            {
                sb.Append(advance());
            }
        }

        var value = sb.ToString();
        return new GreenLeafNode(PythonNodeKind.number, value.Length, value);
    }

    /// <summary>
    ///     扫描标识符或关键字为 GreenLeafNode
    /// </summary>
    private GreenLeafNode scan_identifier_or_keyword_green()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek())))
        {
            sb.Append(advance());
        }

        var text = sb.ToString();
        var nodeKind = _keywords.Contains(text) ? PythonNodeKind.keyword : PythonNodeKind.identifier;

        return new GreenLeafNode(nodeKind, text.Length, text);
    }

    /// <summary>
    ///     扫描运算符为 GreenLeafNode
    /// </summary>
    private GreenLeafNode scan_operator_green()
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        while (!is_at_end())
        {
            var candidate = sb.ToString() + peek();

            if (_operators.Contains(candidate))
            {
                sb.Append(advance());
            }
            else
            {
                break;
            }
        }

        var value = sb.ToString();
        return new GreenLeafNode(PythonNodeKind.@operator, value.Length, value);
    }

    private GreenLeafNode? scan_token()
    {
        var c = peek();

        if (c is '"' or '\'')
        {
            return scan_string();
        }

        if (char.IsDigit(c))
        {
            return scan_number();
        }

        if (c == '_' || char.IsLetter(c))
        {
            return scan_identifier_or_keyword();
        }

        if (is_operator_start(c))
        {
            return scan_operator();
        }

        if (is_delimiter(c))
        {
            advance();
            return new GreenLeafNode(PythonNodeKind.delimiter, c.ToString().Length, c.ToString());
        }

        advance();
        _diagnostics?.report_error(
            string.Empty,
            default,
            "NPY1002",
            $"意外的字符 '{c}'");

        return null;
    }

    private GreenLeafNode scan_string()
    {
        var quote = advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != quote)
        {
            if (peek() == '\\')
            {
                advance();
                if (is_at_end())
                {
                    break;
                }

                var escaped = advance();
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
                sb.Append(advance());
            }
        }

        if (!is_at_end())
        {
            advance();
        }

        return new GreenLeafNode(PythonNodeKind.@string, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_number()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && char.IsDigit(peek()))
        {
            sb.Append(advance());
        }

        if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
        {
            sb.Append(advance());

            while (!is_at_end() && char.IsDigit(peek()))
            {
                sb.Append(advance());
            }
        }

        if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
        {
            sb.Append(advance());

            if (!is_at_end() && (peek() == '+' || peek() == '-'))
            {
                sb.Append(advance());
            }

            while (!is_at_end() && char.IsDigit(peek()))
            {
                sb.Append(advance());
            }
        }

        return new GreenLeafNode(PythonNodeKind.number, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_identifier_or_keyword()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek())))
        {
            sb.Append(advance());
        }

        var text = sb.ToString();

        if (_keywords.Contains(text))
        {
            return new GreenLeafNode(PythonNodeKind.keyword, text.Length, text);
        }

        return new GreenLeafNode(PythonNodeKind.identifier, text.Length, text);
    }

    private GreenLeafNode scan_operator()
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        while (!is_at_end())
        {
            var candidate = sb.ToString() + peek();

            if (_operators.Contains(candidate))
            {
                sb.Append(advance());
            }
            else
            {
                break;
            }
        }

        return new GreenLeafNode(PythonNodeKind.@operator, sb.ToString().Length, sb.ToString());
    }

    #endregion

    #region 辅助方法

    private static bool is_operator_start(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '@' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' => true,
            _ => false
        };
    }

    private static bool is_delimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ':' or ';' or '.';
    }

    /// <summary>
    ///     跳过行注释
    /// </summary>
    private void skip_line_comment()
    {
        while (!is_at_end() && peek() != '\n')
        {
            advance();
        }
    }

    /// <summary>
    ///     前进到新行
    /// </summary>
    private void advance_new_line()
    {
        if (peek() == '\r')
        {
            advance();
        }

        if (peek() == '\n')
        {
            advance();
        }
    }

    /// <summary>
    ///     跳过空白字符（不包含换行符，换行符由缩进逻辑处理）
    /// </summary>
    private void skip_python_whitespace()
    {
        while (!is_at_end() && (peek() == ' ' || peek() == '\t'))
        {
            advance();
        }
    }

    #endregion
}
