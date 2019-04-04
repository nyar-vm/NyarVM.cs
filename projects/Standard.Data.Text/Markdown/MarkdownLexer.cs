using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Lexing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 词法E析器
/// </summary>
public sealed class MarkdownLexer : LexerBase
{
    private static readonly HashSet<string> _html_block_tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "base", "basefont", "blockquote", "body",
        "caption", "center", "col", "colgroup", "dd", "details", "dialog", "dir",
        "div", "dl", "dt", "fieldset", "figcaption", "figure", "footer", "form",
        "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header",
        "hr", "html", "iframe", "legend", "li", "link", "main", "menu", "menuitem",
        "nav", "noframes", "ol", "optgroup", "option", "p", "param", "section",
        "source", "summary", "table", "tbody", "td", "tfoot", "th", "thead",
        "title", "tr", "track", "ul"
    };

    private readonly MarkdownLanguageConfig _config;


    /// <summary>
    ///     创建 Markdown 词法E析器
    /// </summary>
    public MarkdownLexer(MarkdownLanguageConfig? config = null)
    {
        _config = config ?? MarkdownLanguageConfig.@default;
    }


    /// <summary>
    ///     封E代码转换为词法单允EE
    /// </summary>
    public override IReadOnlyList<GreenLeafNode> tokenize(string source)
    {
        _source = new StringSource(source);
        reset();
        _diagnostics ??= new DiagnosticSink();

        var tokens = new List<GreenLeafNode>();

        while (!is_at_end())
        {
            var token = scan_token();
            if (token is not null) tokens.Add(token);
        }

        tokens.Add(new GreenLeafNode(MarkdownNodeKind.eof, string.Empty.Length, string.Empty));
        return tokens;
    }

    private GreenLeafNode? scan_token()
    {
        if (is_at_end()) return null;

        var c = peek();

        if (c is '\n' or '\r') return scan_new_line();

        if (c is ' ' or '\t') return scan_whitespace();

        if (c == '#') return scan_heading_or_text();

        if (c == '>') return scan_blockquote_marker();

        if (c is '-' or '*' or '+') return scan_list_or_hr_or_text();

        if (char.IsDigit(c)) return scan_ordered_list_or_text();

        if (c == '`') return scan_code_marker();

        if (c == '[') return scan_bracket_open();

        if (c == ']')
            return new GreenLeafNode(MarkdownNodeKind.link_close, advance().ToString().Length, advance().ToString());

        if (c == '(')
            return new GreenLeafNode(MarkdownNodeKind.url_open, advance().ToString().Length, advance().ToString());

        if (c == ')')
            return new GreenLeafNode(MarkdownNodeKind.url_close, advance().ToString().Length, advance().ToString());

        if (c == '!') return scan_image_or_text();

        if (c == '|')
            return new GreenLeafNode(MarkdownNodeKind.table_delimiter, advance().ToString().Length,
                advance().ToString());

        if (c == '\\') return scan_escape();

        if (c == '~')
        {
            if (_config.enable_strikethrough) return scan_strikethrough_or_text();

            return scan_text();
        }

        if (c == '=' && _config.enable_setext_headings) return scan_setext_heading_or_text();

        if (c == '=' && _config.enable_highlight) return scan_highlight_or_text();

        if (c == '$' && _config.enable_math) return scan_math_marker();

        if (c == ':' && _config.enable_footnotes)
            return new GreenLeafNode(MarkdownNodeKind.colon, advance().ToString().Length, advance().ToString());

        if (c == '<' && (_config.enable_html_inline || _config.enable_html_blocks))
        {
            var htmlToken = try_scan_html();
            if (htmlToken is not null) return htmlToken;
        }

        if (_config.enable_auto_links && c is 'h' or 'H')
        {
            var autoLink = try_scan_auto_link();
            if (autoLink is not null) return autoLink;
        }

        return scan_text();
    }

    private GreenLeafNode scan_new_line()
    {
        if (peek() == '\r') advance();

        if (peek() == '\n') advance();

        return new GreenLeafNode(MarkdownNodeKind.new_line, "\n".Length, "\n");
    }

    private GreenLeafNode scan_whitespace()
    {
        var atLineStart = is_at_line_start();
        var sb = new StringBuilder();
        var spaceCount = 0;

        while (!is_at_end() && (peek() == ' ' || peek() == '\t'))
        {
            if (peek() == ' ')
                spaceCount++;
            else
                spaceCount += 4;

            sb.Append(advance());
        }

        if (_config.enable_indented_code_blocks && spaceCount >= 4 && atLineStart)
            return new GreenLeafNode(MarkdownNodeKind.indented_code_marker, sb.ToString().Length, sb.ToString());

        return new GreenLeafNode(MarkdownNodeKind.whitespace, sb.ToString().Length, sb.ToString());
    }

    private bool is_at_line_start()
    {
        return _position == 0 || _source[_position - 1] == '\n';
    }

    private GreenLeafNode scan_heading_or_text()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!is_at_end() && peek() == '#' && count < 6)
        {
            sb.Append(advance());
            count++;
        }

        if (count > 0 && (is_at_end() || char.IsWhiteSpace(peek())))
            return new GreenLeafNode(MarkdownNodeKind.heading_marker, sb.ToString().Length, sb.ToString());

        while (!is_at_end() && peek() != '\n' && peek() != '\r') sb.Append(advance());

        return new GreenLeafNode(MarkdownNodeKind.text, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_blockquote_marker()
    {
        advance();

        if (!is_at_end() && peek() == ' ')
        {
            advance();
            return new GreenLeafNode(MarkdownNodeKind.blockquote_marker, "> ".Length, "> ");
        }

        return new GreenLeafNode(MarkdownNodeKind.blockquote_marker, ">".Length, ">");
    }

    private GreenLeafNode scan_list_or_hr_or_text()
    {
        var marker = advance();
        var sb = new StringBuilder();
        sb.Append(marker);

        if (!is_at_end() && peek() == marker)
        {
            var count = 1;
            while (!is_at_end() && peek() == marker)
            {
                sb.Append(advance());
                count++;
            }

            if (count >= 2)
            {
                while (!is_at_end() && peek() != '\n' && peek() != '\r')
                {
                    if (!char.IsWhiteSpace(peek())) return scan_text_from(sb.ToString());

                    sb.Append(advance());
                }

                return new GreenLeafNode(MarkdownNodeKind.horizontal_rule, sb.ToString().Length, sb.ToString());
            }
        }

        if (!is_at_end() && char.IsWhiteSpace(peek()))
        {
            if (peek() == ' ')
            {
                advance();
                var listMarker = $"{marker} ";

                if (_config.enable_task_lists && marker == '-' && !is_at_end() && peek() == '[')
                {
                    var taskToken = try_scan_task_list_marker(listMarker);
                    if (taskToken is not null) return taskToken;
                }

                return new GreenLeafNode(MarkdownNodeKind.unordered_list_marker, listMarker.Length, listMarker);
            }

            return new GreenLeafNode(MarkdownNodeKind.unordered_list_marker, marker.ToString().Length,
                marker.ToString());
        }

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode? try_scan_task_list_marker(string listPrefix)
    {
        var tempPos = _position;

        var sb = new StringBuilder(listPrefix);
        sb.Append(advance());

        if (is_at_end() || (peek() != ' ' && peek() != 'x' && peek() != 'X'))
        {
            _position = tempPos;
            return null;
        }

        var checkChar = advance();
        sb.Append(checkChar);

        if (is_at_end() || peek() != ']')
        {
            _position = tempPos;
            return null;
        }

        sb.Append(advance());

        if (!is_at_end() && peek() == ' ') sb.Append(advance());

        var isChecked = checkChar is 'x' or 'X';
        return new GreenLeafNode(MarkdownNodeKind.task_list_marker, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_ordered_list_or_text()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && char.IsDigit(peek())) sb.Append(advance());

        if (!is_at_end() && peek() == '.')
        {
            sb.Append(advance());

            if (!is_at_end() && char.IsWhiteSpace(peek()))
            {
                if (peek() == ' ')
                {
                    advance();
                    return new GreenLeafNode(MarkdownNodeKind.ordered_list_marker, $"{sb} ".Length, $"{sb} ");
                }

                return new GreenLeafNode(MarkdownNodeKind.ordered_list_marker, sb.ToString().Length, sb.ToString());
            }
        }

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode scan_code_marker()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!is_at_end() && peek() == '`')
        {
            sb.Append(advance());
            count++;
        }

        if (count >= 3)
            return new GreenLeafNode(MarkdownNodeKind.code_block_marker, sb.ToString().Length, sb.ToString());

        return new GreenLeafNode(MarkdownNodeKind.inline_code_marker, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_bracket_open()
    {
        if (_config.enable_footnotes && !is_at_end() && peek_next() == '^')
        {
            advance();
            advance();
            return new GreenLeafNode(MarkdownNodeKind.footnote_marker, "[^".Length, "[^");
        }

        return new GreenLeafNode(MarkdownNodeKind.link_open, advance().ToString().Length, advance().ToString());
    }

    private GreenLeafNode scan_image_or_text()
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        if (!is_at_end() && peek() == '[')
        {
            sb.Append(advance());
            return new GreenLeafNode(MarkdownNodeKind.image_marker, sb.ToString().Length, sb.ToString());
        }

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode scan_escape()
    {
        var sb = new StringBuilder();
        sb.Append(advance());

        if (!is_at_end()) sb.Append(advance());

        return new GreenLeafNode(MarkdownNodeKind.escape, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_strikethrough_or_text()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!is_at_end() && peek() == '~')
        {
            sb.Append(advance());
            count++;
        }

        if (count >= 2)
            return new GreenLeafNode(MarkdownNodeKind.strikethrough_marker, sb.ToString().Length, sb.ToString());

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode scan_setext_heading_or_text()
    {
        var sb = new StringBuilder();
        var ch = peek();
        var count = 0;

        while (!is_at_end() && peek() == ch)
        {
            sb.Append(advance());
            count++;
        }

        if (count >= 1)
        {
            while (!is_at_end() && peek() != '\n' && peek() != '\r')
            {
                if (!char.IsWhiteSpace(peek())) return scan_text_from(sb.ToString());

                sb.Append(advance());
            }

            return new GreenLeafNode(MarkdownNodeKind.setext_heading_marker, sb.ToString().Length, sb.ToString());
        }

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode scan_highlight_or_text()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!is_at_end() && peek() == '=')
        {
            sb.Append(advance());
            count++;
        }

        if (count >= 2)
            return new GreenLeafNode(MarkdownNodeKind.highlight_marker, sb.ToString().Length, sb.ToString());

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode scan_math_marker()
    {
        var sb = new StringBuilder();
        var count = 0;

        while (!is_at_end() && peek() == '$')
        {
            sb.Append(advance());
            count++;
        }

        if (count >= 2) return new GreenLeafNode(MarkdownNodeKind.math_marker, sb.ToString().Length, sb.ToString());

        if (count == 1) return new GreenLeafNode(MarkdownNodeKind.math_marker, sb.ToString().Length, sb.ToString());

        return scan_text_from(sb.ToString());
    }

    private GreenLeafNode? try_scan_html()
    {
        var tempPos = _position;

        var sb = new StringBuilder();
        sb.Append(advance());

        if (is_at_end())
        {
            _position = tempPos;
            return null;
        }

        var isClosing = false;
        if (peek() == '/')
        {
            isClosing = true;
            sb.Append(advance());
        }

        var tagName = new StringBuilder();
        while (!is_at_end() && (char.IsLetterOrDigit(peek()) || peek() == '-')) tagName.Append(advance());

        var tag = tagName.ToString();

        if (string.IsNullOrEmpty(tag))
        {
            _position = tempPos;
            return null;
        }

        if (_config.enable_html_blocks && !isClosing && _html_block_tags.Contains(tag))
        {
            while (!is_at_end() && peek() != '>') sb.Append(advance());

            if (!is_at_end()) sb.Append(advance());

            while (!is_at_end() && peek() != '\n') sb.Append(advance());

            return new GreenLeafNode(MarkdownNodeKind.html_tag, sb.ToString().Length, sb.ToString());
        }

        if (_config.enable_html_inline)
        {
            while (!is_at_end() && peek() != '>') sb.Append(advance());

            if (!is_at_end()) sb.Append(advance());

            return new GreenLeafNode(MarkdownNodeKind.html_tag, sb.ToString().Length, sb.ToString());
        }

        _position = tempPos;
        return null;
    }

    private GreenLeafNode? try_scan_auto_link()
    {
        var sb = new StringBuilder();
        var tempPos = _position;

        var prefix = "http";
        foreach (var ch in prefix)
        {
            if (is_at_end() || char.ToLower(peek()) != ch)
            {
                _position = tempPos;
                return null;
            }

            sb.Append(advance());
        }

        if (!is_at_end() && peek() == 's') sb.Append(advance());

        if (!is_at_end() && peek() == ':' && peek_next() == '/')
        {
            sb.Append(advance());
            sb.Append(advance());
        }
        else
        {
            _position = tempPos;
            return null;
        }

        while (!is_at_end() && !char.IsWhiteSpace(peek()) && peek() != ')' && peek() != ']') sb.Append(advance());

        return new GreenLeafNode(MarkdownNodeKind.auto_link, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_text()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '\n' && peek() != '\r')
        {
            if (peek() == '$' && _config.enable_math) break;

            sb.Append(advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.text, sb.ToString().Length, sb.ToString());
    }

    private GreenLeafNode scan_text_from(string prefix)
    {
        var sb = new StringBuilder(prefix);

        while (!is_at_end() && peek() != '\n' && peek() != '\r')
        {
            if (peek() == '$' && _config.enable_math) break;

            sb.Append(advance());
        }

        return new GreenLeafNode(MarkdownNodeKind.text, sb.ToString().Length, sb.ToString());
    }
}