using System.Text;
using Std.Data.Text.Markdown.Syntax;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 行内解析器
/// </summary>
internal sealed class InlineParser
{
    private static readonly HashSet<char> _escapable_chars =
        ['\\', '`', '*', '_', '{', '}', '[', ']', '(', ')', '#', '+', '-', '.', '!', '|', '~', '>', '=', '$'];

    private readonly MarkdownLanguageConfig _config;
    private readonly Dictionary<string, MarkdownReferenceLinkDefinition> _reference_links;
    private readonly string _text;
    private int _position;

    public InlineParser(string text, MarkdownLanguageConfig? config = null,
        Dictionary<string, MarkdownReferenceLinkDefinition>? referenceLinks = null)
    {
        _text = text;
        _config = config ?? MarkdownLanguageConfig.@default;
        _reference_links = referenceLinks ??
                           new Dictionary<string, MarkdownReferenceLinkDefinition>(StringComparer.OrdinalIgnoreCase);
        _position = 0;
    }

    public IReadOnlyList<MarkdownNode> parse()
    {
        var nodes = new List<MarkdownNode>();
        var sb = new StringBuilder();

        while (!is_at_end())
        {
            if (try_parse_escape(out var escapedText))
            {
                FlushText();
                nodes.Add(new MarkdownText(escapedText));
                continue;
            }

            if (try_parse_strong(out var strong))
            {
                FlushText();
                nodes.Add(strong!);
                continue;
            }

            if (try_parse_emphasis(out var emphasis))
            {
                FlushText();
                nodes.Add(emphasis!);
                continue;
            }

            if (_config.enable_strikethrough && try_parse_strikethrough(out var strike))
            {
                FlushText();
                nodes.Add(strike!);
                continue;
            }

            if (_config.enable_highlight && try_parse_highlight(out var highlight))
            {
                FlushText();
                nodes.Add(highlight!);
                continue;
            }

            if (try_parse_inline_code(out var code))
            {
                FlushText();
                nodes.Add(code!);
                continue;
            }

            if (try_parse_link(out var link))
            {
                FlushText();
                nodes.Add(link!);
                continue;
            }

            if (try_parse_image(out var image))
            {
                FlushText();
                nodes.Add(image!);
                continue;
            }

            if (_config.enable_footnotes && try_parse_footnote(out var footnote))
            {
                FlushText();
                nodes.Add(footnote!);
                continue;
            }

            if (_config.enable_math && try_parse_math_inline(out var math))
            {
                FlushText();
                nodes.Add(math!);
                continue;
            }

            if (_config.enable_html_inline && try_parse_html_inline(out var htmlInline))
            {
                FlushText();
                nodes.Add(htmlInline!);
                continue;
            }

            if (_config.enable_auto_links && try_parse_auto_link(out var autoLink))
            {
                FlushText();
                nodes.Add(autoLink!);
                continue;
            }

            if (try_parse_line_break())
            {
                FlushText();
                nodes.Add(new MarkdownLineBreak());
                continue;
            }

            if (try_parse_soft_break())
            {
                FlushText();
                nodes.Add(new MarkdownSoftBreak());
                continue;
            }

            sb.Append(advance());
        }

        FlushText();
        return nodes;

        void FlushText()
        {
            if (sb.Length > 0)
            {
                nodes.Add(new MarkdownText(sb.ToString()));
                sb.Clear();
            }
        }
    }

    private bool try_parse_escape(out string escaped)
    {
        escaped = string.Empty;
        var start = _position;

        if (peek() != '\\') return false;

        if (_position + 1 < _text.Length && _escapable_chars.Contains(_text[_position + 1]))
        {
            _position++;
            escaped = advance().ToString();
            return true;
        }

        return false;
    }

    private bool try_parse_strong(out MarkdownStrong? strong)
    {
        strong = null;
        var start = _position;

        if (!match("**")) return false;

        var content = read_until("**");
        if (!match("**"))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _reference_links);
        strong = new MarkdownStrong(innerParser.parse());
        return true;
    }

    private bool try_parse_emphasis(out MarkdownEmphasis? emphasis)
    {
        emphasis = null;
        var start = _position;

        if (!match("*") || peek() == '*')
        {
            _position = start;
            return false;
        }

        var content = read_until("*");
        if (!match("*"))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _reference_links);
        emphasis = new MarkdownEmphasis(innerParser.parse());
        return true;
    }

    private bool try_parse_strikethrough(out MarkdownStrikethrough? strike)
    {
        strike = null;
        var start = _position;

        if (!match("~~")) return false;

        var content = read_until("~~");
        if (!match("~~"))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _reference_links);
        strike = new MarkdownStrikethrough(innerParser.parse());
        return true;
    }

    private bool try_parse_highlight(out MarkdownHighlight? highlight)
    {
        highlight = null;
        var start = _position;

        if (!match("==")) return false;

        var content = read_until("==");
        if (!match("=="))
        {
            _position = start;
            return false;
        }

        var innerParser = new InlineParser(content, _config, _reference_links);
        highlight = new MarkdownHighlight(innerParser.parse());
        return true;
    }

    private bool try_parse_inline_code(out MarkdownInlineCode? code)
    {
        code = null;
        var start = _position;

        if (!match("`")) return false;

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '`') sb.Append(advance());

        if (!match("`"))
        {
            _position = start;
            return false;
        }

        code = new MarkdownInlineCode(sb.ToString());
        return true;
    }

    private bool try_parse_link(out MarkdownLink? link)
    {
        link = null;
        var start = _position;

        if (!match("[")) return false;

        var text = read_until("]");
        if (!match("]"))
        {
            _position = start;
            return false;
        }

        if (match("("))
        {
            var url = read_url();
            string? title = null;

            if (peek() == ' ')
            {
                advance();
                if (peek() == '"')
                {
                    advance();
                    title = read_until("\"");
                    match("\"");
                }
            }

            if (!match(")"))
            {
                _position = start;
                return false;
            }

            var innerParser = new InlineParser(text, _config, _reference_links);
            link = new MarkdownLink(url, title, innerParser.parse());
            return true;
        }

        if (_config.enable_reference_links && match("["))
        {
            var refLabel = read_until("]");
            if (!match("]"))
            {
                _position = start;
                return false;
            }

            var label = string.IsNullOrEmpty(refLabel) ? text : refLabel;
            var innerParser = new InlineParser(text, _config, _reference_links);

            if (_reference_links.TryGetValue(label, out var refDef))
            {
                link = new MarkdownLink(refDef.url, refDef.title, innerParser.parse());
                return true;
            }

            link = new MarkdownLink($"#{label}", null, innerParser.parse());
            return true;
        }

        _position = start;
        return false;
    }

    private bool try_parse_image(out MarkdownImage? image)
    {
        image = null;
        var start = _position;

        if (!match("![")) return false;

        var alt = read_until("]");
        if (!match("]"))
        {
            _position = start;
            return false;
        }

        if (match("("))
        {
            var url = read_url();
            string? title = null;

            if (peek() == ' ')
            {
                advance();
                if (peek() == '"')
                {
                    advance();
                    title = read_until("\"");
                    match("\"");
                }
            }

            if (!match(")"))
            {
                _position = start;
                return false;
            }

            image = new MarkdownImage(url, alt, title);
            return true;
        }

        if (_config.enable_reference_links && match("["))
        {
            var refLabel = read_until("]");
            match("]");

            var label = string.IsNullOrEmpty(refLabel) ? alt : refLabel;

            if (_reference_links.TryGetValue(label, out var refDef))
            {
                image = new MarkdownImage(refDef.url, alt, refDef.title);
                return true;
            }

            image = new MarkdownImage($"#{label}", alt);
            return true;
        }

        _position = start;
        return false;
    }

    private bool try_parse_footnote(out MarkdownFootnote? footnote)
    {
        footnote = null;
        var start = _position;

        if (!match("[^")) return false;

        var label = new StringBuilder();
        while (!is_at_end() && peek() != ']') label.Append(advance());

        if (!match("]"))
        {
            _position = start;
            return false;
        }

        footnote = new MarkdownFootnote(label.ToString());
        return true;
    }

    private bool try_parse_math_inline(out MarkdownMathInline? math)
    {
        math = null;
        var start = _position;

        if (!match("$") || peek() == '$')
        {
            _position = start;
            return false;
        }

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '$') sb.Append(advance());

        if (!match("$"))
        {
            _position = start;
            return false;
        }

        math = new MarkdownMathInline(sb.ToString());
        return true;
    }

    private bool try_parse_html_inline(out MarkdownHtmlInline? htmlInline)
    {
        htmlInline = null;
        var start = _position;

        if (peek() != '<') return false;

        var sb = new StringBuilder();
        sb.Append(advance());

        if (!is_at_end() && (peek() == '/' || char.IsLetter(peek())))
        {
            while (!is_at_end() && peek() != '>') sb.Append(advance());

            if (match(">"))
            {
                sb.Append('>');
                htmlInline = new MarkdownHtmlInline(sb.ToString());
                return true;
            }
        }

        _position = start;
        return false;
    }

    private bool try_parse_auto_link(out MarkdownLink? link)
    {
        link = null;

        if (!(_text.AsSpan(_position).StartsWith("http://") || _text.AsSpan(_position).StartsWith("https://")))
            return false;

        var sb = new StringBuilder();

        while (!is_at_end() && !char.IsWhiteSpace(peek()) && peek() != ')' && peek() != ']') sb.Append(advance());

        var url = sb.ToString();
        link = new MarkdownLink(url, null, new List<MarkdownNode> { new MarkdownText(url) });
        return true;
    }

    private bool try_parse_line_break()
    {
        if (peek() == '\\' && peek_next() == '\n')
        {
            advance();
            advance();
            return true;
        }

        if (peek() == ' ' && peek_next() == ' ' && peek_next_next() == '\n')
        {
            advance();
            advance();
            advance();
            return true;
        }

        return false;
    }

    private bool try_parse_soft_break()
    {
        if (peek() == '\n')
        {
            advance();
            return true;
        }

        return false;
    }

    private string read_url()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && peek() != ')' && peek() != ' ') sb.Append(advance());

        return sb.ToString();
    }

    private string read_until(string terminator)
    {
        var sb = new StringBuilder();

        while (!is_at_end())
        {
            if (_text.AsSpan(_position).StartsWith(terminator)) break;

            sb.Append(advance());
        }

        return sb.ToString();
    }

    private bool match(string expected)
    {
        if (_text.AsSpan(_position).StartsWith(expected))
        {
            _position += expected.Length;
            return true;
        }

        return false;
    }

    private char peek()
    {
        return is_at_end() ? '\0' : _text[_position];
    }

    private char peek_next()
    {
        return _position + 1 >= _text.Length ? '\0' : _text[_position + 1];
    }

    private char peek_next_next()
    {
        return _position + 2 >= _text.Length ? '\0' : _text[_position + 2];
    }

    private char advance()
    {
        return _text[_position++];
    }

    private bool is_at_end()
    {
        return _position >= _text.Length;
    }
}