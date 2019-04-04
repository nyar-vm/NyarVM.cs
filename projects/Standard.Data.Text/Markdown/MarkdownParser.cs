using System.Text;
using Std.Data.Text.Markdown.Syntax;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 语法分析器，将词法单元序列解析为 AST
/// </summary>
public sealed class MarkdownParser : IParser<IReadOnlyList<GreenLeafNode>, MarkdownDocument>
{
    private readonly MarkdownLanguageConfig _config;

    private readonly Dictionary<string, MarkdownReferenceLinkDefinition> _reference_links =
        new(StringComparer.OrdinalIgnoreCase);

    private int _position;
    private IReadOnlyList<GreenLeafNode> _tokens = [];


    /// <summary>
    ///     创建 Markdown 语法分析器
    /// </summary>
    public MarkdownParser(MarkdownLanguageConfig? config = null)
    {
        _config = config ?? MarkdownLanguageConfig.@default;
    }


    /// <summary>
    ///     解析词法单元序列为 AST
    /// </summary>
    public MarkdownDocument parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        _tokens = tokens;
        _position = 0;
        _reference_links.Clear();

        var blocks = new List<MarkdownNode>();

        while (!is_at_end())
        {
            skip_empty_lines();

            if (is_at_end()) break;

            var block = parse_block();
            if (block is not null) blocks.Add(block);
        }

        return new MarkdownDocument(blocks);
    }

    #region 行内解析

    private IReadOnlyList<MarkdownNode> parse_inline(string text)
    {
        var reader = new InlineParser(text, _config, _reference_links);
        return reader.parse();
    }

    #endregion

    #region 块级解析

    private MarkdownNode? parse_block()
    {
        if (is_at_end()) return null;

        if (try_parse_horizontal_rule(out var hr)) return hr;

        if (try_parse_heading(out var heading)) return heading;

        if (try_parse_code_block(out var codeBlock)) return codeBlock;

        if (_config.enable_indented_code_blocks && try_parse_indented_code_block(out var indentedCode))
            return indentedCode;

        if (try_parse_blockquote(out var blockquote)) return blockquote;

        if (try_parse_list(out var list)) return list;

        if (_config.enable_tables && try_parse_table(out var table)) return table;

        if (_config.enable_footnotes && try_parse_footnote_definition(out var footnoteDef)) return footnoteDef;

        if (_config.enable_reference_links && try_parse_reference_link_definition(out var refLink)) return refLink;

        if (_config.enable_html_blocks && try_parse_html_block(out var htmlBlock)) return htmlBlock;

        if (_config.enable_math && try_parse_math_block(out var mathBlock)) return mathBlock;

        if (_config.enable_setext_headings && try_parse_setext_heading(out var setextHeading)) return setextHeading;

        return parse_paragraph();
    }

    private bool try_parse_horizontal_rule(out MarkdownHorizontalRule? hr)
    {
        hr = null;
        var start = _position;

        if (match(MarkdownNodeKind.horizontal_rule))
        {
            skip_to_end_of_line();
            hr = new MarkdownHorizontalRule();
            return true;
        }

        if (match(MarkdownNodeKind.text))
        {
            var text = previous().text.Trim();
            if (is_horizontal_rule_text(text))
            {
                hr = new MarkdownHorizontalRule();
                return true;
            }

            _position = start;
        }

        return false;
    }

    private static bool is_horizontal_rule_text(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < 3) return false;

        var ch = trimmed[0];
        if (ch != '-' && ch != '*' && ch != '_') return false;

        foreach (var c in trimmed)
            if (c != ch && !char.IsWhiteSpace(c))
                return false;

        return true;
    }

    private bool try_parse_heading(out MarkdownHeading? heading)
    {
        heading = null;
        var start = _position;

        if (!match(MarkdownNodeKind.heading_marker)) return false;

        var marker = previous().text;
        var level = marker.Trim().Length;

        if (level is < 1 or > 6)
        {
            _position = start;
            return false;
        }

        var text = read_line_text();
        var children = parse_inline(text);
        heading = new MarkdownHeading(level, children);
        return true;
    }

    private bool try_parse_setext_heading(out MarkdownHeading? heading)
    {
        heading = null;
        var start = _position;

        if (!check(MarkdownNodeKind.text)) return false;

        var textLines = new List<string> { advance().text.Trim() };

        while (!is_at_end() && check(MarkdownNodeKind.new_line))
        {
            var newlinePos = _position;
            advance();

            if (check(MarkdownNodeKind.setext_heading_marker))
            {
                var marker = advance().text.Trim();
                var level = marker[0] == '=' ? 1 : 2;
                skip_to_end_of_line();

                var text = string.Join(" ", textLines);
                var children = parse_inline(text);
                heading = new MarkdownHeading(level, children);
                return true;
            }

            if (check(MarkdownNodeKind.text))
            {
                textLines.Add(advance().text.Trim());
            }
            else
            {
                _position = newlinePos + 1;
                break;
            }
        }

        _position = start;
        return false;
    }

    private bool try_parse_code_block(out MarkdownCodeBlock? codeBlock)
    {
        codeBlock = null;
        var start = _position;

        if (!match(MarkdownNodeKind.code_block_marker)) return false;

        var language = string.Empty;
        var remaining = read_line_text().Trim();

        if (!string.IsNullOrEmpty(remaining)) language = remaining;

        var contentLines = new List<string>();

        while (!is_at_end())
        {
            if (check(MarkdownNodeKind.code_block_marker))
            {
                advance();
                skip_to_end_of_line();
                break;
            }

            if (check(MarkdownNodeKind.new_line))
            {
                contentLines.Add(string.Empty);
                advance();
            }
            else if (check(MarkdownNodeKind.text) || check(MarkdownNodeKind.whitespace))
            {
                contentLines.Add(read_line_text());
            }
            else
            {
                var token = advance();
                if (token.kind != MarkdownNodeKind.eof) contentLines.Add(token.text);
            }
        }

        var content = string.Join("\n", contentLines).TrimEnd('\n');
        codeBlock = new MarkdownCodeBlock(
            string.IsNullOrEmpty(language) ? null : language,
            content);
        return true;
    }

    private bool try_parse_indented_code_block(out MarkdownIndentedCodeBlock? codeBlock)
    {
        codeBlock = null;
        var start = _position;

        if (!check(MarkdownNodeKind.indented_code_marker)) return false;

        var lines = new List<string>();

        while (!is_at_end())
            if (check(MarkdownNodeKind.indented_code_marker))
            {
                advance();
                var line = read_line_text().TrimStart();
                lines.Add(line);
            }
            else if (check(MarkdownNodeKind.new_line))
            {
                advance();
            }
            else
            {
                break;
            }

        if (lines.Count == 0)
        {
            _position = start;
            return false;
        }

        var content = string.Join("\n", lines).TrimEnd('\n');
        codeBlock = new MarkdownIndentedCodeBlock(content);
        return true;
    }

    private bool try_parse_blockquote(out MarkdownBlockquote? blockquote)
    {
        blockquote = null;
        var start = _position;

        if (!check(MarkdownNodeKind.blockquote_marker)) return false;

        var lines = new List<string>();

        while (!is_at_end() && check(MarkdownNodeKind.blockquote_marker))
        {
            advance();
            lines.Add(read_line_text());
        }

        if (lines.Count == 0)
        {
            _position = start;
            return false;
        }

        var content = string.Join("\n", lines);
        var innerParser = new MarkdownParser(_config);
        var innerLexer = new MarkdownLexer(_config);
        var tokens = innerLexer.tokenize(content);
        var innerDoc = innerParser.parse(tokens);

        blockquote = new MarkdownBlockquote(innerDoc.children);
        return true;
    }

    private bool try_parse_list(out MarkdownList? list)
    {
        list = null;
        var start = _position;

        var isOrdered = check(MarkdownNodeKind.ordered_list_marker);
        var isUnordered = check(MarkdownNodeKind.unordered_list_marker);

        if (!isOrdered && !isUnordered) return false;

        var items = new List<MarkdownNode>();

        while (!is_at_end())
        {
            if (check(MarkdownNodeKind.task_list_marker))
            {
                var taskMarker = advance().text;
                var isChecked = taskMarker.Contains('x') || taskMarker.Contains('X');

                var itemLines = new List<string> { read_line_text() };

                while (!is_at_end() && !is_list_item_start() && !is_block_end()) itemLines.Add(read_line_text());

                var itemContent = string.Join("\n", itemLines);
                var innerParser = new MarkdownParser(_config);
                var innerLexer = new MarkdownLexer(_config);
                var tokens = innerLexer.tokenize(itemContent);
                var itemDoc = innerParser.parse(tokens);

                items.Add(new MarkdownTaskListItem(isChecked, itemDoc.children));
                continue;
            }

            if (isOrdered && !check(MarkdownNodeKind.ordered_list_marker)) break;

            if (isUnordered && !check(MarkdownNodeKind.unordered_list_marker)) break;

            advance();

            var itemLines2 = new List<string> { read_line_text() };

            while (!is_at_end() && !is_list_item_start() && !is_block_end()) itemLines2.Add(read_line_text());

            var itemContent2 = string.Join("\n", itemLines2);
            var innerParser2 = new MarkdownParser(_config);
            var innerLexer2 = new MarkdownLexer(_config);
            var tokens2 = innerLexer2.tokenize(itemContent2);
            var itemDoc2 = innerParser2.parse(tokens2);

            items.Add(new MarkdownListItem(itemDoc2.children));
        }

        if (items.Count == 0)
        {
            _position = start;
            return false;
        }

        list = new MarkdownList(isOrdered, [.. items]);
        return true;
    }

    private bool try_parse_table(out MarkdownTable? table)
    {
        table = null;
        var start = _position;

        var headerRow = try_parse_table_row();
        if (headerRow is null) return false;

        skip_empty_lines();

        if (!is_at_end() && check(MarkdownNodeKind.table_delimiter))
        {
            skip_to_end_of_line();
        }
        else
        {
            _position = start;
            return false;
        }

        var rows = new List<MarkdownTableRow>();

        while (!is_at_end())
        {
            skip_empty_lines();

            if (is_at_end() || is_block_end()) break;

            var row = try_parse_table_row();
            if (row is not null)
                rows.Add(row);
            else
                break;
        }

        table = new MarkdownTable(headerRow, rows);
        return true;
    }

    private MarkdownTableRow? try_parse_table_row()
    {
        var start = _position;
        var cells = new List<MarkdownTableCell>();

        if (check(MarkdownNodeKind.table_delimiter)) advance();

        while (!is_at_end() && !check(MarkdownNodeKind.new_line))
        {
            var cellText = read_until(MarkdownNodeKind.table_delimiter, MarkdownNodeKind.new_line);

            if (!string.IsNullOrWhiteSpace(cellText))
            {
                var children = parse_inline(cellText.Trim());
                cells.Add(new MarkdownTableCell(children));
            }

            if (check(MarkdownNodeKind.table_delimiter))
                advance();
            else
                break;
        }

        if (cells.Count == 0)
        {
            _position = start;
            return null;
        }

        skip_to_end_of_line();
        return new MarkdownTableRow(cells);
    }

    private bool try_parse_footnote_definition(out MarkdownFootnoteDefinition? footnoteDef)
    {
        footnoteDef = null;
        var start = _position;

        if (!check(MarkdownNodeKind.footnote_marker)) return false;

        advance();

        var label = new StringBuilder();
        while (!is_at_end() && !check(MarkdownNodeKind.link_close)) label.Append(advance().text);

        if (!match(MarkdownNodeKind.link_close))
        {
            _position = start;
            return false;
        }

        if (!match(MarkdownNodeKind.colon))
        {
            _position = start;
            return false;
        }

        var content = read_line_text().Trim();
        var children = parse_inline(content);

        footnoteDef = new MarkdownFootnoteDefinition(label.ToString().Trim(), children);
        return true;
    }

    private bool try_parse_reference_link_definition(out MarkdownReferenceLinkDefinition? refLink)
    {
        refLink = null;
        var start = _position;

        if (!match(MarkdownNodeKind.link_open)) return false;

        var label = new StringBuilder();
        while (!is_at_end() && !check(MarkdownNodeKind.link_close)) label.Append(advance().text);

        if (!match(MarkdownNodeKind.link_close))
        {
            _position = start;
            return false;
        }

        if (!match(MarkdownNodeKind.colon))
        {
            _position = start;
            return false;
        }

        skip_whitespace_tokens();

        var url = new StringBuilder();
        while (!is_at_end() && !check(MarkdownNodeKind.new_line) && !check(MarkdownNodeKind.whitespace))
            url.Append(advance().text);

        string? title = null;
        if (check(MarkdownNodeKind.whitespace))
        {
            advance();
            if (check(MarkdownNodeKind.text))
            {
                var titleText = advance().text.Trim();
                if (titleText.StartsWith('"') && titleText.EndsWith('"'))
                    title = titleText[1..^1];
                else
                    title = titleText;
            }
        }

        skip_to_end_of_line();

        var labelStr = label.ToString().Trim();
        var urlStr = url.ToString().Trim();

        refLink = new MarkdownReferenceLinkDefinition(labelStr, urlStr, title);
        _reference_links[labelStr] = refLink;
        return true;
    }

    private bool try_parse_html_block(out MarkdownHtmlBlock? htmlBlock)
    {
        htmlBlock = null;
        var start = _position;

        if (!check(MarkdownNodeKind.html_tag)) return false;

        var sb = new StringBuilder();

        while (!is_at_end() && check(MarkdownNodeKind.html_tag))
        {
            sb.Append(advance().text);

            while (!is_at_end() && !check(MarkdownNodeKind.new_line) && !check(MarkdownNodeKind.html_tag))
                sb.Append(advance().text);

            if (check(MarkdownNodeKind.new_line))
                sb.Append(advance().text);
            else
                break;
        }

        if (sb.Length == 0)
        {
            _position = start;
            return false;
        }

        htmlBlock = new MarkdownHtmlBlock(sb.ToString().TrimEnd('\n'));
        return true;
    }

    private bool try_parse_math_block(out MarkdownMathBlock? mathBlock)
    {
        mathBlock = null;
        var start = _position;

        if (!check(MarkdownNodeKind.math_marker)) return false;

        var marker = current().text;
        if (marker.Length < 2) return false;

        advance();

        var sb = new StringBuilder();

        while (!is_at_end())
        {
            if (check(MarkdownNodeKind.math_marker) && current().text.Length >= 2)
            {
                advance();
                skip_to_end_of_line();
                break;
            }

            if (check(MarkdownNodeKind.new_line))
            {
                advance();
                sb.Append('\n');
                continue;
            }

            sb.Append(advance().text);
        }

        mathBlock = new MarkdownMathBlock(sb.ToString().Trim('\n', '\r'));
        return true;
    }

    private MarkdownParagraph parse_paragraph()
    {
        var lines = new List<string>();

        while (!is_at_end() && !is_block_end())
        {
            lines.Add(read_line_text());

            if (is_at_end() || check(MarkdownNodeKind.new_line))
                if (peek_next_non_new_line() is null || is_block_end(true))
                    break;
        }

        var text = string.Join(" ", lines);
        var children = parse_inline(text);

        return new MarkdownParagraph(children);
    }

    #endregion

    #region 辅助方法

    private string read_line_text()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && !check(MarkdownNodeKind.new_line)) sb.Append(advance().text);

        if (check(MarkdownNodeKind.new_line)) advance();

        return sb.ToString();
    }

    private string read_line_text_before_marker()
    {
        var sb = new StringBuilder();

        while (!is_at_end() && !check(MarkdownNodeKind.math_marker) && !check(MarkdownNodeKind.new_line))
            sb.Append(advance().text);

        return sb.ToString();
    }

    private string read_until(params NodeKind[] stopTypes)
    {
        var sb = new StringBuilder();

        while (!is_at_end() && !stopTypes.Contains(current().kind)) sb.Append(advance().text);

        return sb.ToString();
    }

    private void skip_empty_lines()
    {
        while (!is_at_end() && check(MarkdownNodeKind.new_line)) advance();
    }

    private void skip_whitespace_tokens()
    {
        while (!is_at_end() && check(MarkdownNodeKind.whitespace)) advance();
    }

    private void skip_to_end_of_line()
    {
        while (!is_at_end() && !check(MarkdownNodeKind.new_line)) advance();

        if (check(MarkdownNodeKind.new_line)) advance();
    }

    private bool is_list_item_start()
    {
        return check(MarkdownNodeKind.ordered_list_marker)
               || check(MarkdownNodeKind.unordered_list_marker)
               || check(MarkdownNodeKind.task_list_marker);
    }

    private bool is_block_end(bool skipNewLines = false)
    {
        if (is_at_end()) return true;

        if (check(MarkdownNodeKind.eof)) return true;

        if (check(MarkdownNodeKind.horizontal_rule)) return true;

        if (check(MarkdownNodeKind.heading_marker)) return true;

        if (check(MarkdownNodeKind.code_block_marker)) return true;

        if (check(MarkdownNodeKind.blockquote_marker)) return true;

        if (is_list_item_start()) return true;

        if (check(MarkdownNodeKind.table_delimiter) && skipNewLines) return true;

        if (check(MarkdownNodeKind.indented_code_marker) && skipNewLines) return true;

        if (check(MarkdownNodeKind.html_tag) && _config.enable_html_blocks) return true;

        if (_config.enable_math && check(MarkdownNodeKind.math_marker) && current().text.Length >= 2) return true;

        return false;
    }

    private GreenLeafNode? peek_next_non_new_line()
    {
        var pos = _position;

        while (pos < _tokens.Count && _tokens[pos].kind == MarkdownNodeKind.new_line) pos++;

        if (pos < _tokens.Count) return _tokens[pos];

        return null;
    }

    private bool is_at_end()
    {
        return _position >= _tokens.Count || current().kind == MarkdownNodeKind.eof;
    }

    private GreenLeafNode current()
    {
        if (_position < _tokens.Count) return _tokens[_position];

        return _tokens[_tokens.Count - 1];
    }

    private GreenLeafNode advance()
    {
        var token = current();
        _position++;
        return token;
    }

    private bool check(NodeKind type)
    {
        if (is_at_end()) return false;

        return current().kind == type;
    }

    private bool match(NodeKind type)
    {
        if (check(type))
        {
            advance();
            return true;
        }

        return false;
    }

    private GreenLeafNode previous()
    {
        return _tokens[_position - 1];
    }

    #endregion
}