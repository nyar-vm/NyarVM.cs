using System.Text;
using Std.Data.Text.Notedown.Lexing;
using Std.Data.Text.Notedown.Syntax;

namespace Std.Data.Text.Notedown.Parsing;

/// <summary>
///     Notedown 语法分析器，将词法单元序列解析为 NotedownDocument
/// </summary>
public sealed class NotedownParser
{
    private List<NotedownBlock>? _footnote_blocks;
    private int _pos;
    private IReadOnlyList<NotedownToken> _tokens = [];

    /// <summary>
    ///     解析 Notedown 文本为文档 AST
    /// </summary>
    public NotedownDocument parse(string source)
    {
        var lexer = new NotedownLexer(source);
        _tokens = lexer.tokenize();
        _pos = 0;
        _footnote_blocks = null;

        var meta = parse_frontmatter();
        var blocks = parse_blocks();

        if (_footnote_blocks is { Count: > 0 }) blocks.AddRange(_footnote_blocks);

        return new NotedownDocument(blocks);
    }

    private Meta parse_frontmatter()
    {
        if (_pos >= _tokens.Count || _tokens[_pos].kind != NotedownTokenKind.yaml_frontmatter_delimiter)
            return Meta.empty;

        _pos++;

        var values = new Dictionary<string, MetaValue>();
        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind == NotedownTokenKind.yaml_frontmatter_delimiter)
            {
                _pos++;
                break;
            }

            if (token.kind == NotedownTokenKind.paragraph_text) parse_yaml_line(token.text, values);

            _pos++;
        }

        if (values.Count == 0) return Meta.empty;

        var meta = Meta.empty;
        foreach (var (key, value) in values) meta = meta.with_value(key, value);

        return meta;
    }

    private static void parse_yaml_line(string line, Dictionary<string, MetaValue> values)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex < 0) return;

        var key = line[..colonIndex].Trim();
        var valueStr = colonIndex + 1 < line.Length ? line[(colonIndex + 1)..].Trim() : string.Empty;

        if (string.IsNullOrEmpty(valueStr))
            values[key] = MetaValue.from_string(string.Empty);
        else if (bool.TryParse(valueStr, out var boolVal))
            values[key] = MetaValue.from_bool(boolVal);
        else
            values[key] = MetaValue.from_string(valueStr);
    }

    #region 脚注

    private void parse_footnote_definition(NotedownToken token)
    {
        _pos++;
        _footnote_blocks ??= [];

        var data = (FootnoteData)token.data!;
        var inlines = parse_inlines(data.content);
        _footnote_blocks.Add(new Para(inlines));
    }

    #endregion

    #region 块级解析

    private List<NotedownBlock> parse_blocks()
    {
        var blocks = new List<NotedownBlock>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            switch (token.kind)
            {
                case NotedownTokenKind.blank_line:
                    _pos++;
                    break;

                case NotedownTokenKind.atx_header:
                    blocks.Add(parse_atx_header(token));
                    break;

                case NotedownTokenKind.setext_underline:
                    blocks.Add(parse_setext_header(token));
                    break;

                case NotedownTokenKind.code_fence_start:
                    blocks.Add(parse_code_block());
                    break;

                case NotedownTokenKind.block_quote:
                    blocks.Add(parse_block_quote());
                    break;

                case NotedownTokenKind.unordered_list_item:
                case NotedownTokenKind.task_list_item:
                    blocks.Add(parse_bullet_list());
                    break;

                case NotedownTokenKind.ordered_list_item:
                    blocks.Add(parse_ordered_list());
                    break;

                case NotedownTokenKind.definition_term:
                    blocks.Add(parse_definition_list());
                    break;

                case NotedownTokenKind.horizontal_rule:
                    blocks.Add(new HorizontalRule());
                    _pos++;
                    break;

                case NotedownTokenKind.table_row:
                    blocks.Add(parse_table());
                    break;

                case NotedownTokenKind.div_fence_start:
                    blocks.Add(parse_div());
                    break;

                case NotedownTokenKind.line_block_line:
                    blocks.Add(parse_line_block());
                    break;

                case NotedownTokenKind.footnote_definition:
                    parse_footnote_definition(token);
                    break;

                case NotedownTokenKind.paragraph_text:
                    blocks.Add(parse_paragraph());
                    break;

                case NotedownTokenKind.html_comment:
                    _pos++;
                    break;

                case NotedownTokenKind.end_of_file:
                    _pos++;
                    break;

                default:
                    _pos++;
                    break;
            }
        }

        return blocks;
    }

    private Header parse_atx_header(NotedownToken token)
    {
        _pos++;
        var data = (AtxHeaderData)token.data!;
        var content = data.content;
        var attr = Attr.empty;

        var braceStart = content.IndexOf(" {");
        if (braceStart >= 0)
        {
            var braceEnd = content.IndexOf('}', braceStart);
            if (braceEnd > braceStart)
            {
                attr = NotedownLexer.parse_attr(content[(braceStart + 2)..braceEnd]);
                content = content[..braceStart];
            }
        }

        var inlines = parse_inlines(content.TrimEnd('#').TrimEnd());
        return new Header(data.level, attr, inlines);
    }

    private Header parse_setext_header(NotedownToken token)
    {
        _pos++;

        var level = token.text.TrimStart()[0] == '=' ? 1 : 2;

        if (_pos >= 2) return new Header(level, [new Str("")]);

        var prevToken = _tokens[_pos - 2];
        var inlines = parse_inlines(prevToken.text.Trim());
        return new Header(level, inlines);
    }

    private CodeBlock parse_code_block()
    {
        var startToken = _tokens[_pos];
        _pos++;

        var info = startToken.data as string ?? string.Empty;
        var language = info;
        var attrStr = string.Empty;

        var braceStart = info.IndexOf('{');
        if (braceStart >= 0)
        {
            language = info[..braceStart].Trim();
            var braceEnd = info.IndexOf('}', braceStart);
            if (braceEnd > braceStart) attrStr = info[(braceStart + 1)..braceEnd];
        }

        var sb = new StringBuilder();
        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind == NotedownTokenKind.code_fence_end)
            {
                _pos++;
                break;
            }

            if (sb.Length > 0) sb.AppendLine();

            sb.Append(token.text);
            _pos++;
        }

        var attr = string.IsNullOrEmpty(attrStr)
            ? new Attr(string.Empty, string.IsNullOrEmpty(language) ? [] : [language])
            : NotedownLexer.parse_attr(attrStr);
        var text = sb.ToString();

        return new CodeBlock(attr, text);
    }

    private BlockQuote parse_block_quote()
    {
        var children = new List<NotedownBlock>();
        var paraLines = new List<string>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind == NotedownTokenKind.block_quote)
            {
                var content = token.data as string ?? string.Empty;

                if (content.Length == 0)
                {
                    if (paraLines.Count > 0)
                    {
                        children.Add(build_paragraph(paraLines));
                        paraLines.Clear();
                    }

                    children.Add(new HorizontalRule());
                }
                else
                {
                    paraLines.Add(content);
                }

                _pos++;
            }
            else if (token.kind == NotedownTokenKind.blank_line)
            {
                if (paraLines.Count > 0)
                {
                    children.Add(build_paragraph(paraLines));
                    paraLines.Clear();
                }

                _pos++;
            }
            else
            {
                break;
            }
        }

        if (paraLines.Count > 0) children.Add(build_paragraph(paraLines));

        return new BlockQuote(children);
    }

    private OrderedList parse_ordered_list()
    {
        var items = new List<IReadOnlyList<NotedownBlock>>();
        var startNumber = 1;

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind != NotedownTokenKind.ordered_list_item) break;

            var data = (OrderedListItemData)token.data!;
            if (items.Count == 0) startNumber = data.start_number;

            _pos++;

            var inlines = parse_inlines(data.content);
            items.Add([new Plain(inlines)]);

            while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.blank_line) _pos++;
        }

        var attrs = new ListAttributes { start_number = startNumber };
        return new OrderedList(attrs, items);
    }

    private BulletList parse_bullet_list()
    {
        var items = new List<IReadOnlyList<NotedownBlock>>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind is not (NotedownTokenKind.unordered_list_item or NotedownTokenKind.task_list_item)) break;

            _pos++;

            if (token.kind == NotedownTokenKind.task_list_item)
            {
                var taskData = (TaskListItemData)token.data!;
                var checkMark = taskData.is_checked ? "x" : " ";
                var inlines = parse_inlines(taskData.content);
                items.Add([new Plain([new Str($"[{checkMark}] "), .. inlines])]);
            }
            else
            {
                var content = token.data as string ?? string.Empty;
                var inlines = parse_inlines(content);
                items.Add([new Plain(inlines)]);
            }

            while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.blank_line) _pos++;
        }

        return new BulletList(items);
    }

    private DefinitionList parse_definition_list()
    {
        var items = new List<DefinitionItem>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind != NotedownTokenKind.definition_term) break;

            var termInlines = parse_inlines(token.text.Trim());
            _pos++;

            var definitions = new List<IReadOnlyList<NotedownBlock>>();
            while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.definition_description)
            {
                var defToken = _tokens[_pos];
                var defContent = defToken.data as string ?? string.Empty;
                var defInlines = parse_inlines(defContent);
                definitions.Add([new Plain(defInlines)]);
                _pos++;

                while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.blank_line) _pos++;
            }

            items.Add(new DefinitionItem { term = termInlines, definitions = definitions });

            while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.blank_line) _pos++;
        }

        return new DefinitionList(items);
    }

    private Table parse_table()
    {
        var allRows = new List<TableRow>();
        var captions = new List<string>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind == NotedownTokenKind.table_row)
            {
                var cells = token.data as IReadOnlyList<string> ?? [];
                var cellInlines = cells.Select(c => parse_inlines(c)).ToList();
                allRows.Add(new TableRow(cellInlines));
                _pos++;

                while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.blank_line) _pos++;
            }
            else if (token.kind == NotedownTokenKind.paragraph_text && token.text.StartsWith("Table:"))
            {
                captions.Add(token.text[6..].Trim());
                _pos++;

                while (_pos < _tokens.Count && _tokens[_pos].kind == NotedownTokenKind.blank_line) _pos++;
            }
            else
            {
                break;
            }
        }

        if (allRows.Count == 0)
            return new Table(
                Attr.empty,
                null,
                [],
                new TableHead(Attr.empty, []),
                [],
                new TableFoot(Attr.empty, []));

        var headRows = allRows.Count > 0 ? new List<TableRow> { allRows[0] } : [];
        var bodyRows = allRows.Count > 1 ? allRows.Skip(1).ToList() : [];

        var caption = captions.Count > 0
            ? new Caption(parse_inlines(captions[0]))
            : null;

        return new Table(
            Attr.empty,
            caption,
            [],
            new TableHead(Attr.empty, headRows),
            [new TableBody(Attr.empty, new RowHeadColumns(0), [], bodyRows)],
            new TableFoot(Attr.empty, []));
    }

    private Div parse_div()
    {
        var startToken = _tokens[_pos];
        _pos++;

        var attr = startToken.data as Attr? ?? Attr.empty;
        var children = new List<NotedownBlock>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind == NotedownTokenKind.div_fence_end)
            {
                _pos++;
                break;
            }

            if (token.kind == NotedownTokenKind.blank_line)
            {
                _pos++;
                continue;
            }

            var block = parse_single_block(token);
            if (block is not null) children.Add(block);
        }

        return new Div(attr, children);
    }

    private NotedownBlock? parse_single_block(NotedownToken token)
    {
        switch (token.kind)
        {
            case NotedownTokenKind.atx_header:
                return parse_atx_header(token);

            case NotedownTokenKind.paragraph_text:
                return parse_paragraph();

            case NotedownTokenKind.unordered_list_item:
                return parse_bullet_list();

            case NotedownTokenKind.ordered_list_item:
                return parse_ordered_list();

            default:
                _pos++;
                return null;
        }
    }

    private LineBlock parse_line_block()
    {
        var lines = new List<IReadOnlyList<NotedownInline>>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind != NotedownTokenKind.line_block_line) break;

            var content = token.data as string ?? string.Empty;
            lines.Add(parse_inlines(content));
            _pos++;
        }

        return new LineBlock(lines);
    }

    private NotedownBlock parse_paragraph()
    {
        var lines = new List<string>();

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];
            if (token.kind == NotedownTokenKind.paragraph_text)
            {
                lines.Add(token.text);
                _pos++;
            }
            else if (token.kind == NotedownTokenKind.blank_line)
            {
                _pos++;
                break;
            }
            else
            {
                break;
            }
        }

        return build_paragraph(lines);
    }

    private NotedownBlock build_paragraph(List<string> lines)
    {
        if (lines.Count == 0) return new Para([]);

        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.Append(' ');

            sb.Append(lines[i]);
        }

        var text = sb.ToString();
        if (text.StartsWith("Table:")) return new Para(parse_inlines(text));

        return new Para(parse_inlines(text));
    }

    #endregion

    #region 行内解析

    /// <summary>
    ///     解析行内内容，将文本转为 NotedownInline 序列
    /// </summary>
    public static List<NotedownInline> parse_inlines(string text)
    {
        var inlines = new List<NotedownInline>();
        if (string.IsNullOrEmpty(text)) return inlines;

        var i = 0;
        var currentText = new StringBuilder();

        while (i < text.Length)
        {
            var ch = text[i];

            if (ch == '\\' && i + 1 < text.Length)
            {
                currentText.Append(text[i + 1]);
                i += 2;
                continue;
            }

            if (ch == '*' || ch == '_')
            {
                var delimiterResult = try_parse_delimiter_run(text, i, ch);
                if (delimiterResult is not null)
                {
                    FlushText();
                    inlines.Add(delimiterResult.inline);
                    i = delimiterResult.new_position;
                    continue;
                }
            }

            if (ch == '`')
            {
                var codeResult = try_parse_code_span(text, i);
                if (codeResult is not null)
                {
                    FlushText();
                    inlines.Add(codeResult.inline);
                    i = codeResult.new_position;
                    continue;
                }
            }

            if (ch == '$')
            {
                var mathResult = try_parse_math(text, i);
                if (mathResult is not null)
                {
                    FlushText();
                    inlines.Add(mathResult.inline);
                    i = mathResult.new_position;
                    continue;
                }
            }

            if (ch == '~' && i + 1 < text.Length)
            {
                if (text[i + 1] == '~')
                {
                    var strikeResult = try_parse_wrapping(text, i, "~~", "~~", inlines => new Strikeout(inlines));
                    if (strikeResult is not null)
                    {
                        FlushText();
                        inlines.Add(strikeResult.inline);
                        i = strikeResult.new_position;
                        continue;
                    }
                }
                else
                {
                    var subResult = try_parse_wrapping(text, i, "~", "~", inlines => new Subscript(inlines));
                    if (subResult is not null)
                    {
                        FlushText();
                        inlines.Add(subResult.inline);
                        i = subResult.new_position;
                        continue;
                    }
                }
            }

            if (ch == '^')
            {
                var supResult = try_parse_wrapping(text, i, "^", "^", inlines => new Superscript(inlines));
                if (supResult is not null)
                {
                    FlushText();
                    inlines.Add(supResult.inline);
                    i = supResult.new_position;
                    continue;
                }
            }

            if (ch == '[')
            {
                var bracketResult = try_parse_bracket_construct(text, i);
                if (bracketResult is not null)
                {
                    FlushText();
                    inlines.AddRange(bracketResult.inlines);
                    i = bracketResult.new_position;
                    continue;
                }
            }

            if (ch == '!' && i + 1 < text.Length && text[i + 1] == '[')
            {
                var imageResult = try_parse_image(text, i);
                if (imageResult is not null)
                {
                    FlushText();
                    inlines.Add(imageResult.inline);
                    i = imageResult.new_position;
                    continue;
                }
            }

            if (ch == '\n')
            {
                FlushText();
                inlines.Add(new SoftBreak());
                i++;
                continue;
            }

            if (ch == ' ')
            {
                FlushText();
                inlines.Add(new Space());
                i++;
                continue;
            }

            currentText.Append(ch);
            i++;
        }

        FlushText();
        return inlines;

        void FlushText()
        {
            if (currentText.Length > 0)
            {
                inlines.Add(new Str(currentText.ToString()));
                currentText.Clear();
            }
        }
    }

    private static DelimiterParseResult? try_parse_delimiter_run(string text, int start, char delimiter)
    {
        var count = 0;
        var i = start;
        while (i < text.Length && text[i] == delimiter)
        {
            count++;
            i++;
        }

        if (count == 0 || count > 3) return null;

        var openStr = new string(delimiter, count);
        var closeIndex = find_closing_delimiter(text, i, delimiter, count);
        if (closeIndex < 0) return null;

        var inner = text[i..closeIndex];
        var innerInlines = parse_inlines(inner);

        NotedownInline result = count switch
        {
            1 => new Emph(innerInlines),
            2 => new Strong(innerInlines),
            3 => new Strong(new List<NotedownInline> { new Emph(innerInlines) }),
            _ => new Emph(innerInlines)
        };

        return new DelimiterParseResult(result, closeIndex + count);
    }

    private static int find_closing_delimiter(string text, int start, char delimiter, int count)
    {
        var i = start;
        while (i < text.Length)
            if (text[i] == delimiter)
            {
                var matchCount = 0;
                var j = i;
                while (j < text.Length && text[j] == delimiter && matchCount < count)
                {
                    matchCount++;
                    j++;
                }

                if (matchCount == count)
                    if (j >= text.Length || text[j] != delimiter)
                        return i;

                i = j;
            }
            else if (text[i] == '\\' && i + 1 < text.Length)
            {
                i += 2;
            }
            else
            {
                i++;
            }

        return -1;
    }

    private static WrappingParseResult? try_parse_code_span(string text, int start)
    {
        var backtickCount = count_leading(text, start, '`');
        if (backtickCount == 0) return null;

        var closeIndex = find_closing_backticks(text, start + backtickCount, backtickCount);
        if (closeIndex < 0) return null;

        var code = text[(start + backtickCount)..closeIndex];
        return new WrappingParseResult(new Code(code.Trim()), closeIndex + backtickCount);
    }

    private static int count_leading(string text, int start, char ch)
    {
        var count = 0;
        var i = start;
        while (i < text.Length && text[i] == ch)
        {
            count++;
            i++;
        }

        return count;
    }

    private static int find_closing_backticks(string text, int start, int count)
    {
        var i = start;
        while (i < text.Length)
        {
            var matchCount = 0;
            var j = i;
            while (j < text.Length && text[j] == '`' && matchCount < count)
            {
                matchCount++;
                j++;
            }

            if (matchCount == count && (j >= text.Length || text[j] != '`')) return i;

            i++;
        }

        return -1;
    }

    private static WrappingParseResult? try_parse_math(string text, int start)
    {
        if (start + 1 >= text.Length) return null;

        if (text[start + 1] == '$')
        {
            var closeIndex = text.IndexOf("$$", start + 2, StringComparison.Ordinal);
            if (closeIndex < 0) return null;

            var mathText = text[(start + 2)..closeIndex];
            return new WrappingParseResult(new Syntax.Math(MathType.display, mathText.Trim()), closeIndex + 2);
        }

        var closeIndex2 = text.IndexOf('$', start + 1);
        if (closeIndex2 < 0) return null;

        var mathText2 = text[(start + 1)..closeIndex2];
        return new WrappingParseResult(new Syntax.Math(MathType.inline, mathText2.Trim()), closeIndex2 + 1);
    }

    private static WrappingParseResult? try_parse_wrapping(
        string text,
        int start,
        string open,
        string close,
        Func<IReadOnlyList<NotedownInline>, NotedownInline> factory)
    {
        if (start + open.Length >= text.Length) return null;

        var innerStart = start + open.Length;
        var closeIndex = text.IndexOf(close, innerStart, StringComparison.Ordinal);
        if (closeIndex < 0) return null;

        var inner = text[innerStart..closeIndex];
        var inlines = parse_inlines(inner);
        var result = factory(inlines);
        return new WrappingParseResult(result, closeIndex + close.Length);
    }

    private static BracketParseResult? try_parse_bracket_construct(string text, int start)
    {
        if (start + 1 >= text.Length) return null;

        if (text[start + 1] == '@') return try_parse_citation(text, start);

        if (text[start + 1] == '^') return try_parse_footnote_ref(text, start);

        if (text[start + 1] == '.' && text.IndexOf("smallcaps]", start, StringComparison.Ordinal) > start)
            return try_parse_small_caps(text, start);

        if (text[start + 1] == '#' || text[start + 1] == '.') return try_parse_span(text, start);

        return try_parse_link_or_span(text, start);
    }

    private static BracketParseResult? try_parse_citation(string text, int start)
    {
        var closeBracket = text.IndexOf(']', start + 2);
        if (closeBracket < 0) return null;

        var ids = text[(start + 2)..closeBracket].Split(';', StringSplitOptions.RemoveEmptyEntries);
        var citations = new List<Citation>();
        foreach (var id in ids) citations.Add(new Citation(id.Trim().TrimStart('@')));

        var cite = new Cite(citations, []);
        return new BracketParseResult([cite], closeBracket + 1);
    }

    private static BracketParseResult? try_parse_footnote_ref(string text, int start)
    {
        var closeBracket = text.IndexOf(']', start + 2);
        if (closeBracket < 0) return null;

        var note = new Note([]);
        return new BracketParseResult([note], closeBracket + 1);
    }

    private static BracketParseResult? try_parse_small_caps(string text, int start)
    {
        var openEnd = text.IndexOf(']', start);
        if (openEnd < 0) return null;

        var closeTag = "[/.smallcaps]";
        var closeIndex = text.IndexOf(closeTag, openEnd + 1, StringComparison.Ordinal);
        if (closeIndex < 0) return null;

        var inner = text[(openEnd + 1)..closeIndex];
        var inlines = parse_inlines(inner);
        var smallCaps = new SmallCaps(inlines);
        return new BracketParseResult([smallCaps], closeIndex + closeTag.Length);
    }

    private static BracketParseResult? try_parse_span(string text, int start)
    {
        var closeBracket = text.IndexOf(']', start + 1);
        if (closeBracket < 0) return null;

        var attrStr = text[(start + 1)..closeBracket];
        var attr = NotedownLexer.parse_attr(attrStr);

        var rest = text[(closeBracket + 1)..];
        var restInlines = parse_inlines(rest);

        var span = new Span(attr, restInlines);
        return new BracketParseResult([span], text.Length);
    }

    private static BracketParseResult? try_parse_link_or_span(string text, int start)
    {
        var closeBracket = text.IndexOf(']', start + 1);
        if (closeBracket < 0) return null;

        var linkText = text[(start + 1)..closeBracket];

        if (closeBracket + 1 < text.Length && text[closeBracket + 1] == '(')
            return try_parse_link(text, start, closeBracket, linkText);

        return null;
    }

    private static BracketParseResult? try_parse_link(string text, int start, int closeBracket, string linkText)
    {
        var parenStart = closeBracket + 1;
        var closeParen = text.IndexOf(')', parenStart);
        if (closeParen < 0) return null;

        var targetStr = text[(parenStart + 1)..closeParen];
        var url = targetStr;
        var title = string.Empty;

        var titleStart = targetStr.IndexOf(" \"", StringComparison.Ordinal);
        if (titleStart >= 0)
        {
            url = targetStr[..titleStart];
            var titleEnd = targetStr.IndexOf('"', titleStart + 2);
            if (titleEnd > titleStart) title = targetStr[(titleStart + 2)..titleEnd];
        }

        var linkInlines = parse_inlines(linkText);
        var target = new Target(url.Trim(), string.IsNullOrEmpty(title) ? null : title);

        var nextPos = closeParen + 1;
        var attr = Attr.empty;

        if (nextPos < text.Length && text[nextPos] == '{')
        {
            var attrEnd = text.IndexOf('}', nextPos);
            if (attrEnd > nextPos)
            {
                attr = NotedownLexer.parse_attr(text[(nextPos + 1)..attrEnd]);
                nextPos = attrEnd + 1;
            }
        }

        var link = new Link(attr, linkInlines, target);
        return new BracketParseResult([link], nextPos);
    }

    private static WrappingParseResult? try_parse_image(string text, int start)
    {
        var closeBracket = text.IndexOf(']', start + 2);
        if (closeBracket < 0) return null;

        if (closeBracket + 1 >= text.Length || text[closeBracket + 1] != '(') return null;

        var altText = text[(start + 2)..closeBracket];
        var parenStart = closeBracket + 1;
        var closeParen = text.IndexOf(')', parenStart);
        if (closeParen < 0) return null;

        var targetStr = text[(parenStart + 1)..closeParen];
        var url = targetStr;
        var title = string.Empty;

        var titleStart = targetStr.IndexOf(" \"", StringComparison.Ordinal);
        if (titleStart >= 0)
        {
            url = targetStr[..titleStart];
            var titleEnd = targetStr.IndexOf('"', titleStart + 2);
            if (titleEnd > titleStart) title = targetStr[(titleStart + 2)..titleEnd];
        }

        var altInlines = parse_inlines(altText);
        var target = new Target(url.Trim(), string.IsNullOrEmpty(title) ? null : title);

        var nextPos = closeParen + 1;
        var attr = Attr.empty;

        if (nextPos < text.Length && text[nextPos] == '{')
        {
            var attrEnd = text.IndexOf('}', nextPos);
            if (attrEnd > nextPos)
            {
                attr = NotedownLexer.parse_attr(text[(nextPos + 1)..attrEnd]);
                nextPos = attrEnd + 1;
            }
        }

        var image = new Image(attr, altInlines, target);
        return new WrappingParseResult(image, nextPos);
    }

    #endregion
}