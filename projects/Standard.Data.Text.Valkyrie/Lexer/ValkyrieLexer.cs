using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Lexing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Lexer;

/// <summary>
///     Valkyrie 词法分析器，产出 GreenLeafNode 序列
/// </summary>
public sealed class ValkyrieLexer : LexerBase
{
    private readonly ValkyrieLanguage _language;

    /// <summary>
    ///     使用默认语言配置创建词法分析器
    /// </summary>
    public ValkyrieLexer()
    {
        _language = ValkyrieLanguage.standard;
    }

    /// <summary>
    ///     使用诊断接收器创建词法分析器
    /// </summary>
    /// <param name="diagnostics">诊断接收器。</param>
    public ValkyrieLexer(DiagnosticSink? diagnostics)
    {
        _language = ValkyrieLanguage.standard;
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     使用指定语言配置创建词法分析器
    /// </summary>
    /// <param name="language">语言配置。</param>
    public ValkyrieLexer(ValkyrieLanguage language)
    {
        _language = language;
    }

    /// <summary>
    ///     使用指定语言配置和诊断接收器创建词法分析器
    /// </summary>
    /// <param name="language">语言配置。</param>
    /// <param name="diagnostics">诊断接收器。</param>
    public ValkyrieLexer(ValkyrieLanguage language, DiagnosticSink? diagnostics)
    {
        _language = language;
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     将源代码转换为词法单元序列（GreenLeafNode）
    /// </summary>
    /// <param name="source">源代码文本。</param>
    /// <returns>词法单元列表。</returns>
    public override IReadOnlyList<GreenLeafNode> tokenize(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<GreenLeafNode>();

        while (!is_at_end())
        {
            skip_whitespace();

            if (is_at_end()) break;

            var c = peek();

            if (c is '\n' or '\r')
            {
                advance_new_line();
                continue;
            }

            if (c == '<' && peek_next() == '#')
            {
                var start = _position;
                advance(); // <
                advance(); // #
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.comment_l.to_node_kind(), 2, "<#"));

                var contentStart = _position;
                while (!is_at_end())
                {
                    if (peek() == '#' && peek_next() == '>') break;

                    advance();
                }

                if (_position > contentStart)
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.comment_content.to_node_kind(),
                        _position - contentStart,
                        _source.substring(new Range(contentStart, _position))));

                if (!is_at_end())
                {
                    advance(); // #
                    advance(); // >
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.comment_r.to_node_kind(), 2, "#>"));
                }

                continue;
            }

            if (c is '#' or '⍝')
            {
                string commentMarker;
                if (c == '#' && peek_next() == '?')
                {
                    advance();
                    advance();
                    commentMarker = "#?";
                }
                else
                {
                    commentMarker = advance().ToString();
                }

                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.comment_start.to_node_kind(), commentMarker.Length,
                    commentMarker));

                var contentStart = _position;
                while (!is_at_end() && peek() != '\n' && peek() != '\r') advance();

                if (_position > contentStart)
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.comment_content.to_node_kind(),
                        _position - contentStart,
                        _source.substring(new Range(contentStart, _position))));

                continue;
            }

            if (c == '/' && peek_next() == '/')
            {
                var isDocComment = peek(2) == '/';
                var invalidMarker = isDocComment ? "///" : "//";
                _diagnostics?.report_error(
                    default,
                    isDocComment
                        ? "Valkyrie 不支持 `///` 文档注释，请改用 `#?` 或 `///`。"
                        : "Valkyrie 不支持 `//` 行注释，请改用 `#`。");

                advance();
                advance();
                if (isDocComment)
                {
                    advance();
                }

                while (!is_at_end() && peek() != '\n' && peek() != '\r')
                {
                    advance();
                }

                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.error.to_node_kind(), invalidMarker.Length, invalidMarker));
                continue;
            }

            if (c == '"' || c == '\'')
            {
                var start = _position;
                scan_string_content(c);
                var sourceText = _source.substring(new Range(start, _position));
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.@string.to_node_kind(), _position - start,
                    sourceText));
                continue;
            }

            if (c == '`')
            {
                var rawIdentifier = scan_raw_identifier_text();
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.identifier.to_node_kind(), rawIdentifier.Length + 2,
                    rawIdentifier));
                continue;
            }

            if (char.IsDigit(c) || (c == '0' && (peek_next() == 'x' || peek_next() == 'X')))
            {
                var numberText = scan_number_text();
                tokens.Add(new GreenLeafNode(ValkyrieTokenKind.number.to_node_kind(), numberText.Length, numberText));
                continue;
            }

            if (c == '_' || char.IsLetter(c))
            {
                var start = _position;
                var idText = scan_identifier_text();
                if (!is_at_end() && (peek() == '"' || peek() == '\''))
                {
                    scan_string_content(peek());
                    var sourceText = _source.substring(new Range(start, _position));
                    tokens.Add(new GreenLeafNode(ValkyrieTokenKind.@string.to_node_kind(), _position - start,
                        sourceText));
                    continue;
                }

                var kind = classify_identifier(idText);

                tokens.Add(new GreenLeafNode(kind, idText.Length, idText));
                continue;
            }

            if (is_operator_start(c))
            {
                var opText = scan_operator_text();
                var kind = opText switch
                {
                    "+" => ValkyrieTokenKind.plus,
                    "-" => ValkyrieTokenKind.minus,
                    "*" => ValkyrieTokenKind.star,
                    "/" => ValkyrieTokenKind.slash,
                    "%" => ValkyrieTokenKind.percent,
                    "^" => ValkyrieTokenKind.power,
                    "=" => ValkyrieTokenKind.equal,
                    "+=" => ValkyrieTokenKind.plus_equal,
                    "-=" => ValkyrieTokenKind.minus_equal,
                    "*=" => ValkyrieTokenKind.star_equal,
                    "/=" => ValkyrieTokenKind.slash_equal,
                    "%=" => ValkyrieTokenKind.percent_equal,
                    "==" => ValkyrieTokenKind.equal_equal,
                    "!=" => ValkyrieTokenKind.bang_equal,
                    "<" => ValkyrieTokenKind.less,
                    ">" => ValkyrieTokenKind.greater,
                    "<=" => ValkyrieTokenKind.less_equal,
                    ">=" => ValkyrieTokenKind.greater_equal,
                    "&&" => ValkyrieTokenKind.amp_amp,
                    "||" => ValkyrieTokenKind.pipe_pipe,
                    "!" => ValkyrieTokenKind.bang,
                    "&" => ValkyrieTokenKind.amp,
                    "|" => ValkyrieTokenKind.pipe,
                    "~" => ValkyrieTokenKind.tilde,
                    "&=" => ValkyrieTokenKind.amp_equal,
                    "|=" => ValkyrieTokenKind.pipe_equal,
                    "^=" => ValkyrieTokenKind.caret_equal,
                    "<<" => ValkyrieTokenKind.less_less,
                    ">>" => ValkyrieTokenKind.greater_greater,
                    "<<=" => ValkyrieTokenKind.less_less_equal,
                    ">>=" => ValkyrieTokenKind.greater_greater_equal,
                    "->" => ValkyrieTokenKind.arrow,
                    "=>" => ValkyrieTokenKind.fat_arrow,
                    "??" => ValkyrieTokenKind.question_question,
                    "++" => ValkyrieTokenKind.plus_plus,
                    "--" => ValkyrieTokenKind.minus_minus,
                    "?" => ValkyrieTokenKind.question,
                    "." => ValkyrieTokenKind.dot,
                    "..=" => ValkyrieTokenKind.dot_dot_equal,
                    "..." => ValkyrieTokenKind.dot_dot_dot,
                    ".." => ValkyrieTokenKind.dot_dot,
                    ":" => ValkyrieTokenKind.colon,
                    "::" => ValkyrieTokenKind.double_colon,
                    "," => ValkyrieTokenKind.comma,
                    "⁅" => ValkyrieTokenKind.offset_l,
                    "⁆" => ValkyrieTokenKind.offset_r,
                    "⟨" => ValkyrieTokenKind.generic_l,
                    "⟩" => ValkyrieTokenKind.generic_r,
                    "<%" => ValkyrieTokenKind.template_l,
                    "%>" => ValkyrieTokenKind.template_r,
                    _ => ValkyrieTokenKind.error
                };
                tokens.Add(new GreenLeafNode(kind.to_node_kind(), opText.Length, opText));
                continue;
            }

            if (is_delimiter(c))
            {
                var kind = c switch
                {
                    '(' => ValkyrieTokenKind.parenthesis_l,
                    ')' => ValkyrieTokenKind.parenthesis_r,
                    '[' => ValkyrieTokenKind.bracket_l,
                    ']' => ValkyrieTokenKind.bracket_r,
                    '{' => ValkyrieTokenKind.brace_l,
                    '}' => ValkyrieTokenKind.brace_r,
                    ';' => ValkyrieTokenKind.semicolon,
                    _ => ValkyrieTokenKind.error
                };
                advance();
                tokens.Add(new GreenLeafNode(kind.to_node_kind(), 1, c.ToString()));
                continue;
            }

            advance();
            _diagnostics?.report_error(
                default,
                $"意外的字符 '{c}'");
        }

        tokens.Add(new GreenLeafNode(ValkyrieTokenKind.eos.to_node_kind(), 0, ""));
        return tokens;
    }

    /// <summary>
    ///     分类标识符为关键词、字面量或标识符
    /// </summary>
    private NodeKind classify_identifier(string text)
    {
        var kind = text switch
        {
            "namespace" => ValkyrieTokenKind.@namespace,
            "using" => ValkyrieTokenKind.@using,
            "let" => ValkyrieTokenKind.let,
            "micro" => ValkyrieTokenKind.micro,
            "mezzo" => ValkyrieTokenKind.mezzo,
            "macro" => ValkyrieTokenKind.macro,
            "unsafe" => ValkyrieTokenKind.@unsafe,
            "if" => ValkyrieTokenKind.@if,
            "else" => ValkyrieTokenKind.@else,
            "loop" => ValkyrieTokenKind.loop,
            "while" => ValkyrieTokenKind.@while,
            "until" => ValkyrieTokenKind.until,
            "structure" => ValkyrieTokenKind.structure,
            "class" => ValkyrieTokenKind.@class,
            "enums" => ValkyrieTokenKind.enums,
            "flags" => ValkyrieTokenKind.flags,
            "union" => ValkyrieTokenKind.union,
            "unite" => ValkyrieTokenKind.unite,
            "type" => ValkyrieTokenKind.type,
            "where" => ValkyrieTokenKind.where,
            "imply" => ValkyrieTokenKind.imply,
            "trait" => ValkyrieTokenKind.trait,
            "match" => ValkyrieTokenKind.match,
            "case" => ValkyrieTokenKind.@case,
            "end" => ValkyrieTokenKind.end,
            "break" => ValkyrieTokenKind.@break,
            "continue" => ValkyrieTokenKind.@continue,
            "raise" => ValkyrieTokenKind.raise,
            "yield" => ValkyrieTokenKind.yield,
            "try" => ValkyrieTokenKind.@try,
            "return" => ValkyrieTokenKind.@return,
            "resume" => ValkyrieTokenKind.resume,
            "catch" => ValkyrieTokenKind.@catch,
            "in" => ValkyrieTokenKind.@in,
            "is" => ValkyrieTokenKind.@is,
            "as" => ValkyrieTokenKind.@as,

            "true" => ValkyrieTokenKind.@true,
            "false" => ValkyrieTokenKind.@false,
            "null" => ValkyrieTokenKind.@null,

            "component" when _language.support_ecs_extension => ValkyrieTokenKind.component,
            "system" when _language.support_ecs_extension => ValkyrieTokenKind.system,
            "widget" when _language.support_ui_extension => ValkyrieTokenKind.widget,
            "model" when _language.support_schema_extension => ValkyrieTokenKind.model,
            "service" when _language.support_schema_extension => ValkyrieTokenKind.service,
            "message" when _language.support_schema_extension => ValkyrieTokenKind.message,

            "shader" when _language.support_shader_extension => ValkyrieTokenKind.shader,
            "vertex" when _language.support_shader_extension => ValkyrieTokenKind.vertex,
            "fragment" when _language.support_shader_extension => ValkyrieTokenKind.fragment,
            "compute" when _language.support_shader_extension => ValkyrieTokenKind.compute,
            "uniform" when _language.support_shader_extension => ValkyrieTokenKind.uniform,
            "varying" when _language.support_shader_extension => ValkyrieTokenKind.varying,
            "cbuffer" when _language.support_shader_extension => ValkyrieTokenKind.c_buffer,
            "texture" when _language.support_shader_extension => ValkyrieTokenKind.texture,
            "sampler" when _language.support_shader_extension => ValkyrieTokenKind.sampler,
            "discard" when _language.support_shader_extension => ValkyrieTokenKind.discard,
            "raygen" when _language.support_shader_extension => ValkyrieTokenKind.raygen,
            "closesthit" when _language.support_shader_extension => ValkyrieTokenKind.closesthit,
            "anyhit" when _language.support_shader_extension => ValkyrieTokenKind.anyhit,
            "miss" when _language.support_shader_extension => ValkyrieTokenKind.miss,
            "constant" when _language.support_shader_extension => ValkyrieTokenKind.constant,
            "binding" when _language.support_shader_extension => ValkyrieTokenKind.binding,

            "neural" => ValkyrieTokenKind.neural,

            _ => ValkyrieTokenKind.identifier
        };

        return kind.to_node_kind();
    }

    /// <summary>
    ///     扫描标识符文本
    /// </summary>
    private string scan_identifier_text()
    {
        var sb = new StringBuilder();
        while (!is_at_end() && (peek() == '_' || char.IsLetterOrDigit(peek()))) sb.Append(advance());
        return sb.ToString();
    }

    /// <summary>
    ///     扫描数字文本
    /// </summary>
    private string scan_number_text()
    {
        var sb = new StringBuilder();
        var isHex = false;
        var isBinary = false;
        var isOctal = false;

        if (peek() == '0')
        {
            sb.Append(advance());
            if (!is_at_end() && (peek() == 'x' || peek() == 'X'))
            {
                sb.Append(advance());
                isHex = true;
            }
            else if (!is_at_end() && (peek() == 'b' || peek() == 'B'))
            {
                sb.Append(advance());
                isBinary = true;
            }
            else if (!is_at_end() && (peek() == 'o' || peek() == 'O'))
            {
                sb.Append(advance());
                isOctal = true;
            }
        }

        if (isHex)
        {
            scan_number_digits(sb, is_hex_digit);
        }
        else if (isBinary)
        {
            scan_number_digits(sb, c => c == '0' || c == '1');
        }
        else if (isOctal)
        {
            scan_number_digits(sb, c => c is >= '0' and <= '7');
        }
        else
        {
            scan_number_digits(sb, char.IsDigit);

            if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
            {
                sb.Append(advance());
                scan_number_digits(sb, char.IsDigit);
            }

            if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
            {
                sb.Append(advance());
                if (!is_at_end() && (peek() == '+' || peek() == '-')) sb.Append(advance());

                scan_number_digits(sb, char.IsDigit);
            }
        }

        scan_number_suffix(sb);

        return sb.ToString();
    }

    /// <summary>
    ///     扫描允许数字分隔下划线的数字主体。
    /// </summary>
    private void scan_number_digits(StringBuilder sb, Func<char, bool> isValidDigit)
    {
        while (!is_at_end())
        {
            if (isValidDigit(peek()))
            {
                sb.Append(advance());
                continue;
            }

            if (peek() == '_' && !is_at_end() && isValidDigit(peek_next()))
            {
                sb.Append(advance());
                continue;
            }

            break;
        }
    }

    /// <summary>
    ///     扫描数值类型后缀，支持 <c>42i32</c> 与 <c>42_i32</c>。
    /// </summary>
    private void scan_number_suffix(StringBuilder sb)
    {
        if (is_at_end())
        {
            return;
        }

        if (peek() == '_' && char.IsLetter(peek_next()))
        {
            sb.Append(advance());
        }
        else if (!char.IsLetter(peek()))
        {
            return;
        }

        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '_'))
        {
            sb.Append(advance());
        }
    }

    private static bool is_hex_digit(char c)
    {
        return char.IsDigit(c) || c is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    /// <summary>
    ///     扫描字符串内容（不含引号）
    /// </summary>
    private string scan_string_content(char quote)
    {
        advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != quote)
            if (peek() == '\\')
            {
                advance();
                if (is_at_end()) break;

                var escaped = advance();
                sb.Append(escaped switch
                {
                    'n' => '\n', 'r' => '\r', 't' => '\t',
                    '\\' => '\\', '"' => '"', '\'' => '\'', '0' => '\0',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(advance());
            }

        if (!is_at_end())
            advance();
        else
            _diagnostics?.report_error(default, "未闭合的字符串");

        return sb.ToString();
    }

    /// <summary>
    ///     扫描 `r"..."` / `r'...'` 原始字符串内容。
    ///     原始字符串不处理转义，直到遇到匹配的引号才结束。
    /// </summary>
    private void scan_raw_string_content(char quote)
    {
        advance();
        advance();

        while (!is_at_end() && peek() != quote)
        {
            advance();
        }

        if (!is_at_end())
        {
            advance();
        }
        else
        {
            _diagnostics?.report_error(default, "未闭合的原始字符串");
        }
    }

    /// <summary>
    ///     扫描 `raw id` 内容（不含反引号）
    /// </summary>
    private string scan_raw_identifier_text()
    {
        advance();
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '`') sb.Append(advance());

        if (!is_at_end())
            advance();
        else
            _diagnostics?.report_error(default, "未闭合的原始标识符");

        return sb.ToString();
    }

    /// <summary>
    ///     扫描运算符文本
    /// </summary>
    private string scan_operator_text()
    {
        var start = _position;
        var c = advance();

        switch (c)
        {
            case '+':
                if (peek() == '+' || peek() == '=') advance();

                break;
            case '-':
                if (peek() == '-' || peek() == '=' || peek() == '>') advance();

                break;
            case '*':
            case '/':
            case '%':
            case '&':
            case '|':
            case '^':
            case '=':
            case '!':
                if (peek() == '=')
                    advance();
                else if (c == '&' && peek() == '&')
                    advance();
                else if (c == '|' && peek() == '|')
                    advance();
                else if (c == '=' && peek() == '>')
                    advance();
                else if (c == '%' && peek() == '>') advance();

                break;
            case '<':
                if (peek() == '<')
                {
                    advance();
                    if (peek() == '=') advance();
                }
                else if (peek() == '=')
                {
                    advance();
                }
                else if (peek() == '%')
                {
                    advance();
                }

                break;
            case '>':
                if (peek() == '>')
                {
                    advance();
                    if (peek() == '=') advance();
                }
                else if (peek() == '=')
                {
                    advance();
                }

                break;
            case '?':
                if (peek() == '?') advance();

                break;
            case ':':
                if (peek() == ':') advance();

                break;
            case '.':
                if (peek() == '.')
                {
                    advance();
                    if (peek() == '=')
                    {
                        advance();
                    }
                    else if (peek() == '.')
                    {
                        advance();
                    }
                }

                break;
        }

        return _source.substring(new Range(start, _position));
    }

    /// <summary>
    ///     判断字符是否为运算符起始
    /// </summary>
    private static bool is_operator_start(char c)
    {
        return c switch
        {
            '+' or '-' or '*' or '/' or '%' or '&' or '|' or '^' or '~'
                or '<' or '>' or '=' or '!' or '?' or ':' or ',' or '.'
                or '⁅' or '⁆' or '⟨' or '⟩' => true,
            _ => false
        };
    }

    /// <summary>
    ///     判断字符是否为分隔符
    /// </summary>
    private static bool is_delimiter(char c)
    {
        return c is '(' or ')' or '[' or ']' or '{' or '}' or ';';
    }

    /// <summary>
    ///     跳过换行符
    /// </summary>
    private void advance_new_line()
    {
        if (peek() == '\r') advance();

        if (peek() == '\n') advance();
    }
}
