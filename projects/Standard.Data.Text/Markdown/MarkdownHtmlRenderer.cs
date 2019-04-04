using System.Text;
using Std.Data.Text.Markdown.Syntax;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 转 HTML 渲染器
/// </summary>
public sealed class MarkdownHtmlRenderer
{
    private readonly List<MarkdownFootnoteDefinition> _footnotes;
    private readonly MarkdownHtmlOptions _options;
    private readonly List<TocItem> _toc_items;
    private int _footnote_counter;
    private bool _has_ka_te_x_math;
    private int _heading_counter;

    public MarkdownHtmlRenderer(MarkdownHtmlOptions? options = null)
    {
        _options = options ?? new MarkdownHtmlOptions();
        _toc_items = [];
        _footnotes = [];
        _heading_counter = 0;
        _footnote_counter = 0;
    }


    /// <summary>
    ///     获取或设置 KaTeX 服务端渲染服务实例
    ///     当 MathMode 为 KaTeX 时，若此属性为 null 则自动创建默认实例
    /// </summary>
    public KaTeXService? ka_te_x_service { get; set; }


    /// <summary>
    ///     渲染 Markdown 文档为 HTML
    /// </summary>
    public MarkdownHtmlResult render(MarkdownDocument document)
    {
        _toc_items.Clear();
        _footnotes.Clear();
        _heading_counter = 0;
        _footnote_counter = 0;
        _has_ka_te_x_math = false;

        var html = render_nodes(document.children);
        var footnotesHtml = render_footnotes();

        var finalHtml = html + footnotesHtml;

        return new MarkdownHtmlResult
        {
            html = finalHtml,
            toc_items = [.. _toc_items],
            footnotes = [.. _footnotes],
            has_ka_te_x_math = _has_ka_te_x_math
        };
    }


    /// <summary>
    ///     渲染 Markdown 文本为 HTML
    /// </summary>
    public MarkdownHtmlResult render(string markdown)
    {
        var lexer = new MarkdownLexer();
        var parser = new MarkdownParser();
        var tokens = lexer.tokenize(markdown);
        var document = parser.parse(tokens);
        return render(document);
    }

    private string render_nodes(IReadOnlyList<MarkdownNode> nodes)
    {
        var sb = new StringBuilder();

        foreach (var node in nodes) sb.Append(render_node(node));

        return sb.ToString();
    }

    private string render_node(MarkdownNode node)
    {
        return node switch
        {
            MarkdownHeading heading => render_heading(heading),
            MarkdownParagraph paragraph => render_paragraph(paragraph),
            MarkdownCodeBlock codeBlock => render_code_block(codeBlock),
            MarkdownIndentedCodeBlock indentedCode => render_indented_code_block(indentedCode),
            MarkdownInlineCode inlineCode => render_inline_code(inlineCode),
            MarkdownBlockquote blockquote => render_blockquote(blockquote),
            MarkdownList list => render_list(list),
            MarkdownHorizontalRule => "<hr />\n",
            MarkdownLink link => render_link(link),
            MarkdownImage image => render_image(image),
            MarkdownStrong strong => $"<strong>{render_nodes(strong.children)}</strong>",
            MarkdownEmphasis emphasis => $"<em>{render_nodes(emphasis.children)}</em>",
            MarkdownStrikethrough strikethrough => $"<del>{render_nodes(strikethrough.children)}</del>",
            MarkdownHighlight highlight => $"<mark>{render_nodes(highlight.children)}</mark>",
            MarkdownText text => escape_html(text.content),
            MarkdownLineBreak => "<br />\n",
            MarkdownSoftBreak softBreak => render_soft_break(softBreak),
            MarkdownTable table => render_table(table),
            MarkdownTaskListItem taskItem => render_task_list_item(taskItem),
            MarkdownFootnote footnote => render_footnote(footnote),
            MarkdownFootnoteDefinition footnoteDef => render_footnote_definition(footnoteDef),
            MarkdownMathInline mathInline => render_math_inline(mathInline),
            MarkdownMathBlock mathBlock => render_math_block(mathBlock),
            MarkdownHtmlBlock htmlBlock => htmlBlock.content + "\n",
            MarkdownHtmlInline htmlInline => htmlInline.content,
            MarkdownReferenceLinkDefinition => string.Empty,
            _ => string.Empty
        };
    }

    private string render_heading(MarkdownHeading heading)
    {
        var content = render_nodes(heading.children);
        var id = "";

        if (_options.generate_heading_ids)
        {
            _heading_counter++;
            id = generate_heading_id(content, heading.level);
            _toc_items.Add(new TocItem { level = heading.level, text = content, id = id });
        }

        var idAttr = string.IsNullOrEmpty(id) ? "" : $" id=\"{id}\"";
        return $"<h{heading.level}{idAttr}>{content}</h{heading.level}>\n";
    }

    private string generate_heading_id(string text, int level)
    {
        var id = new StringBuilder();
        var lastWasDash = false;

        foreach (var c in text)
            if (char.IsLetterOrDigit(c))
            {
                id.Append(char.ToLowerInvariant(c));
                lastWasDash = false;
            }
            else if (c is ' ' or '-' or '_')
            {
                if (!lastWasDash && id.Length > 0)
                {
                    id.Append('-');
                    lastWasDash = true;
                }
            }

        if (id.Length == 0) id.Append($"heading-{_heading_counter}");

        return id.ToString();
    }

    private string render_paragraph(MarkdownParagraph paragraph)
    {
        return $"<p>{render_nodes(paragraph.children)}</p>\n";
    }

    private string render_code_block(MarkdownCodeBlock codeBlock)
    {
        var escapedContent = escape_html(codeBlock.content);
        var languageClass = "";

        if (_options.highlight_code && !string.IsNullOrEmpty(codeBlock.language))
            languageClass = $" class=\"{_options.code_block_class_prefix}{escape_html(codeBlock.language)}\"";

        return $"<pre><code{languageClass}>{escapedContent}</code></pre>\n";
    }

    private string render_indented_code_block(MarkdownIndentedCodeBlock codeBlock)
    {
        var escapedContent = escape_html(codeBlock.content);
        return $"<pre><code>{escapedContent}</code></pre>\n";
    }

    private string render_inline_code(MarkdownInlineCode inlineCode)
    {
        return $"<code>{escape_html(inlineCode.content)}</code>";
    }

    private string render_blockquote(MarkdownBlockquote blockquote)
    {
        return $"<blockquote>\n{render_nodes(blockquote.children)}</blockquote>\n";
    }

    private string render_list(MarkdownList list)
    {
        var tag = list.is_ordered ? "ol" : "ul";
        var sb = new StringBuilder();
        sb.Append($"<{tag}>\n");

        foreach (var item in list.items) sb.Append(render_list_item(item));

        sb.Append($"</{tag}>\n");
        return sb.ToString();
    }

    private string render_list_item(MarkdownNode item)
    {
        if (item is MarkdownTaskListItem taskItem) return render_task_list_item(taskItem);

        if (item is MarkdownListItem listItem) return $"<li>{render_nodes(listItem.children)}</li>\n";

        return $"<li>{render_node(item)}</li>\n";
    }

    private string render_task_list_item(MarkdownTaskListItem taskItem)
    {
        var checkedAttr = taskItem.is_checked ? " checked" : "";
        var disabledAttr = " disabled";
        var checkbox = $"<input type=\"checkbox\"{checkedAttr}{disabledAttr} />";
        return $"<li>{checkbox}{render_nodes(taskItem.children)}</li>\n";
    }

    private string render_link(MarkdownLink link)
    {
        var content = render_nodes(link.children);
        var titleAttr = string.IsNullOrEmpty(link.title) ? "" : $" title=\"{escape_html(link.title)}\"";
        var externalAttr = "";

        if (_options.external_link_new_tab && is_external_link(link.url))
            externalAttr = " target=\"_blank\" rel=\"noopener noreferrer\"";

        return $"<a href=\"{escape_html(link.url)}\"{titleAttr}{externalAttr}>{content}</a>";
    }

    private string render_image(MarkdownImage image)
    {
        var titleAttr = string.IsNullOrEmpty(image.title) ? "" : $" title=\"{escape_html(image.title)}\"";
        return $"<img src=\"{escape_html(image.url)}\" alt=\"{escape_html(image.alt)}\"{titleAttr} />";
    }

    private string render_footnote(MarkdownFootnote footnote)
    {
        _footnote_counter++;
        var id = _footnote_counter;
        return
            $"<sup class=\"footnote-ref\" id=\"fnref-{escape_html(footnote.label)}\"><a href=\"#fn-{escape_html(footnote.label)}\">{id}</a></sup>";
    }

    private string render_footnote_definition(MarkdownFootnoteDefinition footnoteDef)
    {
        _footnotes.Add(footnoteDef);
        return string.Empty;
    }

    private string render_footnotes()
    {
        if (_footnotes.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("<section class=\"footnotes\">\n<ol>\n");

        foreach (var footnote in _footnotes)
        {
            var content = render_nodes(footnote.children);
            sb.Append($"<li id=\"fn-{escape_html(footnote.label)}\">{content}</li>\n");
        }

        sb.Append("</ol>\n</section>\n");
        return sb.ToString();
    }

    private string render_math_inline(MarkdownMathInline math)
    {
        return _options.math_mode switch
        {
            MathRenderMode.ka_te_x => render_ka_te_x_inline(math.content),
            MathRenderMode.math_jax => $"\\({escape_html(math.content)}\\)",
            MathRenderMode.raw => $"${escape_html(math.content)}$",
            _ => $"${escape_html(math.content)}$"
        };
    }

    private string render_math_block(MarkdownMathBlock math)
    {
        return _options.math_mode switch
        {
            MathRenderMode.ka_te_x => render_ka_te_x_block(math.content),
            MathRenderMode.math_jax => $"$${escape_html(math.content)}$$\n",
            MathRenderMode.raw => $"$${escape_html(math.content)}$$\n",
            _ => $"$${escape_html(math.content)}$$\n"
        };
    }

    private string render_ka_te_x_inline(string latex)
    {
        ka_te_x_service ??= new KaTeXService();
        _has_ka_te_x_math = true;
        return ka_te_x_service.render_to_string(latex, false);
    }

    private string render_ka_te_x_block(string latex)
    {
        ka_te_x_service ??= new KaTeXService();
        _has_ka_te_x_math = true;
        return ka_te_x_service.render_to_string(latex, true) + "\n";
    }

    private string render_soft_break(MarkdownSoftBreak _)
    {
        return _options.soft_break_as_line_break ? "<br />\n" : "\n";
    }

    private string render_table(MarkdownTable table)
    {
        var sb = new StringBuilder();
        sb.Append("<table>\n<thead>\n<tr>\n");

        foreach (var cell in table.header.cells) sb.Append($"<th>{render_nodes(cell.children)}</th>\n");

        sb.Append("</tr>\n</thead>\n<tbody>\n");

        foreach (var row in table.rows)
        {
            sb.Append("<tr>\n");

            foreach (var cell in row.cells) sb.Append($"<td>{render_nodes(cell.children)}</td>\n");

            sb.Append("</tr>\n");
        }

        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    private bool is_external_link(string url)
    {
        foreach (var prefix in _options.external_link_prefixes)
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private static string escape_html(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}