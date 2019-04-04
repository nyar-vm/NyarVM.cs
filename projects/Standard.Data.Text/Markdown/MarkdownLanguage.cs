using System.Text;
using Std.Data.Text.Markdown.Syntax;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;
using NotedownBlock = Std.Data.Text.Notedown.Syntax.NotedownBlock;
using NotedownInline = Std.Data.Text.Notedown.Syntax.NotedownInline;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 语言前端，封装词法分析、语法分析管线以及 Notedown IR 双向转换
/// </summary>
public sealed class MarkdownLanguage : Language
{
    private readonly MarkdownLexer _lexer;
    private readonly MarkdownParser _parser;

    /// <summary>
    ///     创建 Markdown 语言实例（使用默认配置）
    /// </summary>
    public MarkdownLanguage()
        : this(MarkdownLanguageConfig.@default)
    {
    }

    /// <summary>
    ///     创建 Markdown 语言实例
    /// </summary>
    public MarkdownLanguage(MarkdownLanguageConfig config)
    {
        this.config = config;
        _lexer = new MarkdownLexer(config);
        _parser = new MarkdownParser(config);
    }

    /// <inheritdoc />
    public override string name => "Markdown";

    /// <summary>
    ///     语言配置
    /// </summary>
    public MarkdownLanguageConfig config { get; }

    /// <summary>
    ///     将 Markdown 源码解析为 AST
    /// </summary>
    public MarkdownDocument parse(string source)
    {
        var tokens = _lexer.tokenize(source);
        return _parser.parse(tokens);
    }

    /// <summary>
    ///     将 Markdown 源码解析并渲染为 HTML
    /// </summary>
    public string render_to_html(string source, MarkdownHtmlOptions? options = null)
    {
        var document = parse(source);
        var renderer = new MarkdownHtmlRenderer(options);
        var result = renderer.render(document);
        return result.html;
    }

    /// <summary>
    ///     将 Markdown AST 转换为 Notedown IR
    /// </summary>
    public override NotedownDocument to_notedown(object ast)
    {
        var mdDoc = (MarkdownDocument)ast;
        var blocks = new List<NotedownBlock>();
        foreach (var child in mdDoc.children)
        {
            var block = convert_block(child);
            if (block is not null) blocks.Add(block);
        }

        return new NotedownDocument(blocks);
    }

    /// <summary>
    ///     将 Notedown IR 转换为 Markdown 文本
    /// </summary>
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < document.blocks.Count; i++)
        {
            if (i > 0) sb.AppendLine();

            write_block(sb, document.blocks[i]);
        }

        if (sb.Length > 0 && sb[^1] != '\n') sb.AppendLine();

        return sb.ToString();
    }

    #region ToNotedown 转换

    private static NotedownBlock? convert_block(MarkdownNode node)
    {
        return node switch
        {
            MarkdownHeading heading => new Header(heading.level, convert_inlines(heading.children)),
            MarkdownParagraph paragraph => new Para(convert_inlines(paragraph.children)),
            MarkdownCodeBlock codeBlock => new CodeBlock(codeBlock.language ?? string.Empty, codeBlock.content),
            MarkdownList list => convert_list(list),
            MarkdownBlockquote blockquote => new BlockQuote(convert_blocks(blockquote.children)),
            MarkdownHorizontalRule => new HorizontalRule(),
            MarkdownTable table => convert_markdown_table(table),
            MarkdownHtmlBlock htmlBlock => new RawBlock("html", htmlBlock.content),
            _ => null
        };
    }

    private static List<NotedownBlock> convert_blocks(IReadOnlyList<MarkdownNode> nodes)
    {
        var blocks = new List<NotedownBlock>();
        foreach (var node in nodes)
        {
            var block = convert_block(node);
            if (block is not null) blocks.Add(block);
        }

        return blocks;
    }

    private static NotedownBlock convert_list(MarkdownList list)
    {
        var items = new List<IReadOnlyList<NotedownBlock>>();
        foreach (var item in list.items)
            if (item is MarkdownListItem listItem)
            {
                items.Add(convert_blocks(listItem.children));
            }
            else if (item is MarkdownTaskListItem taskItem)
            {
                var checkMark = taskItem.is_checked ? "x" : " ";
                items.Add([new Plain([new Str($"[{checkMark}] "), .. convert_inlines(taskItem.children)])]);
            }

        if (list.is_ordered) return new OrderedList(ListAttributes.@default, items);

        return new BulletList(items);
    }

    private static NotedownBlock convert_markdown_table(MarkdownTable table)
    {
        var headRow = new TableRow([.. table.header.cells.Select(c => convert_inlines(c.children))]);
        var bodyRows = table.rows.Select(r =>
            new TableRow([.. r.cells.Select(c => convert_inlines(c.children))])).ToList();

        return new Table(
            Attr.empty,
            null,
            [],
            new TableHead(Attr.empty, [headRow]),
            [new TableBody(Attr.empty, new RowHeadColumns(0), [], bodyRows)],
            new TableFoot(Attr.empty, []));
    }

    private static List<NotedownInline> convert_inlines(IReadOnlyList<MarkdownNode> nodes)
    {
        var inlines = new List<NotedownInline>();
        foreach (var node in nodes)
        {
            var converted = convert_inline(node);
            if (converted is not null) inlines.AddRange(converted);
        }

        return inlines;
    }

    private static IReadOnlyList<NotedownInline>? convert_inline(MarkdownNode node)
    {
        switch (node)
        {
            case MarkdownText text:
                return [new Str(text.content)];

            case MarkdownEmphasis emph:
                return [new Emph(convert_inlines(emph.children))];

            case MarkdownStrong strong:
                return [new Strong(convert_inlines(strong.children))];

            case MarkdownStrikethrough strikethrough:
                return [new Strikeout(convert_inlines(strikethrough.children))];

            case MarkdownInlineCode inlineCode:
                return [new Code(inlineCode.content)];

            case MarkdownLink link:
                return [new Link(convert_inlines(link.children), new Target(link.url, link.title))];

            case MarkdownImage image:
                return [new Image([new Str(image.alt)], new Target(image.url, image.title))];

            case MarkdownLineBreak:
                return [new LineBreak()];

            case MarkdownSoftBreak:
                return [new SoftBreak()];

            case MarkdownMathInline mathInline:
                return [new Notedown.Syntax.Math(MathType.inline, mathInline.content)];

            case MarkdownMathBlock mathBlock:
                return [new Notedown.Syntax.Math(MathType.display, mathBlock.content)];

            case MarkdownHighlight highlight:
                return [new Span(new Attr(string.Empty, ["highlight"]), convert_inlines(highlight.children))];

            case MarkdownHtmlInline htmlInline:
                return [new RawInline("html", htmlInline.content)];

            default:
                return null;
        }
    }

    #endregion

    #region FromNotedown 转换

    private static void write_block(StringBuilder sb, NotedownBlock block)
    {
        switch (block)
        {
            case Header header:
                sb.Append(new string('#', header.level));
                sb.Append(' ');
                write_inlines(sb, header.inlines);
                sb.AppendLine();
                break;
            case Para para:
                write_inlines(sb, para.inlines);
                sb.AppendLine();
                break;
            case Plain plain:
                write_inlines(sb, plain.inlines);
                sb.AppendLine();
                break;
            case CodeBlock codeBlock:
                var language = codeBlock.language;
                if (string.IsNullOrEmpty(language))
                {
                    sb.AppendLine("```");
                }
                else
                {
                    sb.Append("```");
                    sb.AppendLine(language);
                }

                sb.AppendLine(codeBlock.text);
                sb.AppendLine("```");
                break;
            case BlockQuote blockQuote:
                foreach (var child in blockQuote.children)
                {
                    sb.Append("> ");
                    write_block_inline(sb, child);
                }

                break;
            case OrderedList orderedList:
                var startNum = orderedList.list_attributes.start_number;
                for (var i = 0; i < orderedList.items.Count; i++)
                {
                    var num = startNum + i;
                    sb.Append($"{num}. ");
                    write_list_item_content(sb, orderedList.items[i]);
                }

                sb.AppendLine();
                break;
            case BulletList bulletList:
                foreach (var item in bulletList.items)
                {
                    sb.Append("- ");
                    write_list_item_content(sb, item);
                }

                sb.AppendLine();
                break;
            case HorizontalRule:
                sb.AppendLine("---");
                break;
            case Table table:
                write_table(sb, table);
                break;
            case RawBlock rawBlock:
                if (rawBlock.format == "html") sb.AppendLine(rawBlock.text);

                break;
            case LineBlock lineBlock:
                foreach (var line in lineBlock.lines)
                {
                    write_inlines(sb, line);
                    sb.AppendLine("  ");
                }

                sb.AppendLine();
                break;
        }
    }

    private static void write_block_inline(StringBuilder sb, NotedownBlock block)
    {
        switch (block)
        {
            case Para para:
                write_inlines(sb, para.inlines);
                sb.AppendLine();
                break;
            case Plain plain:
                write_inlines(sb, plain.inlines);
                sb.AppendLine();
                break;
            case Header header:
                sb.Append(new string('#', header.level));
                sb.Append(' ');
                write_inlines(sb, header.inlines);
                sb.AppendLine();
                break;
            case CodeBlock codeBlock:
                if (string.IsNullOrEmpty(codeBlock.language))
                {
                    sb.AppendLine("```");
                }
                else
                {
                    sb.Append("```");
                    sb.AppendLine(codeBlock.language);
                }

                sb.AppendLine(codeBlock.text);
                sb.AppendLine("```");
                break;
            default:
                sb.AppendLine();
                break;
        }
    }

    private static void write_list_item_content(StringBuilder sb, IReadOnlyList<NotedownBlock> blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            if (i > 0) sb.Append("  ");

            switch (blocks[i])
            {
                case Para para:
                    write_inlines(sb, para.inlines);
                    sb.AppendLine();
                    break;
                case Plain plain:
                    write_inlines(sb, plain.inlines);
                    sb.AppendLine();
                    break;
                case BulletList nestedList:
                    foreach (var nestedItem in nestedList.items)
                    {
                        sb.Append("  - ");
                        write_list_item_content(sb, nestedItem);
                    }

                    break;
                default:
                    sb.AppendLine();
                    break;
            }
        }
    }

    private static void write_table(StringBuilder sb, Table table)
    {
        var allRows = new List<TableRow>();
        allRows.AddRange(table.head.rows);
        foreach (var body in table.bodies)
        {
            allRows.AddRange(body.head_rows);
            allRows.AddRange(body.rows);
        }

        allRows.AddRange(table.foot.rows);

        if (allRows.Count == 0) return;

        var colCount = allRows.Max(r => r.cells.Count);

        for (var i = 0; i < allRows.Count; i++)
        {
            sb.Append("| ");
            for (var j = 0; j < colCount; j++)
            {
                if (j < allRows[i].cells.Count) write_inlines(sb, allRows[i].cells[j]);

                sb.Append(" | ");
            }

            sb.AppendLine();

            if (i == 0)
            {
                sb.Append("| ");
                for (var j = 0; j < colCount; j++) sb.Append("--- | ");

                sb.AppendLine();
            }
        }
    }

    private static void write_inlines(StringBuilder sb, IReadOnlyList<NotedownInline> inlines)
    {
        foreach (var inline in inlines) write_inline(sb, inline);
    }

    private static void write_inline(StringBuilder sb, NotedownInline inline)
    {
        switch (inline)
        {
            case Str str:
                sb.Append(str.text);
                break;
            case Space:
                sb.Append(' ');
                break;
            case SoftBreak:
                sb.AppendLine();
                break;
            case LineBreak:
                sb.AppendLine();
                break;
            case Emph emph:
                sb.Append('*');
                write_inlines(sb, emph.inlines);
                sb.Append('*');
                break;
            case Strong strong:
                sb.Append("**");
                write_inlines(sb, strong.inlines);
                sb.Append("**");
                break;
            case Strikeout strikeout:
                sb.Append("~~");
                write_inlines(sb, strikeout.inlines);
                sb.Append("~~");
                break;
            case Code code:
                sb.Append('`');
                sb.Append(code.text);
                sb.Append('`');
                break;
            case Notedown.Syntax.Math math:
                if (math.math_type == MathType.display)
                {
                    sb.Append("$$");
                    sb.Append(math.text);
                    sb.Append("$$");
                }
                else
                {
                    sb.Append('$');
                    sb.Append(math.text);
                    sb.Append('$');
                }

                break;
            case Link link:
                sb.Append('[');
                write_inlines(sb, link.inlines);
                sb.Append("](");
                sb.Append(link.target.url);
                if (!string.IsNullOrEmpty(link.target.title))
                {
                    sb.Append(" \"");
                    sb.Append(link.target.title);
                    sb.Append('"');
                }

                sb.Append(')');
                break;
            case Image image:
                sb.Append("![");
                write_inlines(sb, image.inlines);
                sb.Append("](");
                sb.Append(image.target.url);
                if (!string.IsNullOrEmpty(image.target.title))
                {
                    sb.Append(" \"");
                    sb.Append(image.target.title);
                    sb.Append('"');
                }

                sb.Append(')');
                break;
            case RawInline rawInline:
                if (rawInline.format == "html") sb.Append(rawInline.text);

                break;
            case Superscript superscript:
                sb.Append('^');
                write_inlines(sb, superscript.inlines);
                sb.Append('^');
                break;
            case Subscript subscript:
                sb.Append('~');
                write_inlines(sb, subscript.inlines);
                sb.Append('~');
                break;
            case SmallCaps smallCaps:
                write_inlines(sb, smallCaps.inlines);
                break;
            case Cite cite:
                sb.Append('[');
                foreach (var citation in cite.citations)
                {
                    sb.Append('@');
                    sb.Append(citation.id);
                }

                sb.Append(']');
                break;
        }
    }

    #endregion
}