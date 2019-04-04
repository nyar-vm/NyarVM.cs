using System.Text;
using System.Xml;
using System.Xml.Linq;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Xml;

/// <summary>
///     XML 语言前端，封装解析以及与 Notedown IR 的双向转换
/// </summary>
public sealed class XmlLanguage : Language
{
    /// <summary>
    ///     创建 XML 语言实例（使用默认配置）
    /// </summary>
    public XmlLanguage()
        : this(XmlLanguageConfig.@default)
    {
    }

    /// <summary>
    ///     创建 XML 语言实例
    /// </summary>
    public XmlLanguage(XmlLanguageConfig config)
    {
        this.config = config;
    }

    /// <inheritdoc />
    public override string name => "XML";

    /// <summary>
    ///     语言配置
    /// </summary>
    public XmlLanguageConfig config { get; }

    /// <inheritdoc />
    public override NotedownDocument to_notedown(object ast)
    {
        var source = (string)ast;
        try
        {
            var doc = XDocument.Parse(source);
            if (doc.Root is null) return NotedownDocument.empty;

            var blocks = convert_element(doc.Root, 1);
            return new NotedownDocument(blocks);
        }
        catch (XmlException)
        {
            return NotedownDocument.empty;
        }
    }

    /// <inheritdoc />
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();

        if (config.pretty_print) sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

        sb.Append('<');
        sb.Append(config.root_element_name);
        sb.AppendLine(">");

        foreach (var block in document.blocks) write_block_as_xml(sb, block, 1);

        sb.Append("</");
        sb.Append(config.root_element_name);
        sb.Append('>');

        if (config.pretty_print) sb.AppendLine();

        return sb.ToString();
    }

    #region 解析

    private static List<NotedownBlock> convert_element(XElement element, int level)
    {
        var blocks = new List<NotedownBlock>();

        var hasChildElements = false;
        foreach (var node in element.Nodes())
            if (node is XElement)
            {
                hasChildElements = true;
                break;
            }

        var directText = get_direct_text(element);
        if (hasChildElements)
        {
            if (!string.IsNullOrEmpty(directText)) blocks.Add(new Para([new Str(directText)]));

            foreach (var childElement in element.Elements())
            {
                var childLevel = System.Math.Min(level + 1, 6);
                var childBlocks = convert_element(childElement, childLevel);

                foreach (var childBlock in childBlocks) blocks.Add(childBlock);
            }
        }
        else
        {
            var tagName = element.Name.LocalName;

            if (string.IsNullOrEmpty(directText))
            {
                blocks.Add(new Header(level, [new Str(tagName)]));
            }
            else
            {
                var inlines = new List<NotedownInline>
                {
                    new Strong([new Str(tagName)]),
                    new Str(": "),
                    new Str(directText)
                };
                blocks.Add(new Para(inlines));
            }
        }

        return blocks;
    }

    private static string get_direct_text(XElement element)
    {
        var sb = new StringBuilder();
        foreach (var node in element.Nodes())
            if (node is XText textNode)
                sb.Append(textNode.Value);

        return sb.ToString().Trim();
    }

    #endregion

    #region 格式化

    private static void write_block_as_xml(StringBuilder sb, NotedownBlock block, int indent)
    {
        switch (block)
        {
            case Header header:
            {
                var tag = sanitize_tag_name(inlines_to_text(header.inlines));
                write_indent(sb, indent);
                sb.Append('<');
                sb.Append(tag);
                sb.AppendLine(">");

                var hasContent = false;
                for (var i = 0; i < header.inlines.Count; i++)
                    if (header.inlines[i] is Str or Code or Emph or Strong)
                    {
                        hasContent = true;
                        break;
                    }

                if (hasContent)
                {
                    write_indent(sb, indent + 1);
                    sb.AppendLine(xml_escape(inlines_to_text(header.inlines)));
                }

                write_indent(sb, indent);
                sb.Append("</");
                sb.Append(tag);
                sb.AppendLine(">");
                break;
            }
            case Para para:
            {
                var text = inlines_to_text(para.inlines);
                var colonIdx = text.IndexOf(": ", StringComparison.Ordinal);
                if (colonIdx > 0)
                {
                    var key = sanitize_tag_name(text[..colonIdx]);
                    var value = text[(colonIdx + 2)..];
                    write_indent(sb, indent);
                    sb.Append('<');
                    sb.Append(key);
                    sb.Append('>');
                    sb.Append(xml_escape(value));
                    sb.Append("</");
                    sb.Append(key);
                    sb.AppendLine(">");
                }
                else
                {
                    write_indent(sb, indent);
                    sb.Append("<p>");
                    sb.Append(xml_escape(text));
                    sb.AppendLine("</p>");
                }

                break;
            }
            case Plain plain:
            {
                write_indent(sb, indent);
                sb.Append("<p>");
                sb.Append(xml_escape(inlines_to_text(plain.inlines)));
                sb.AppendLine("</p>");
                break;
            }
            case CodeBlock codeBlock:
            {
                write_indent(sb, indent);
                sb.Append('<');
                sb.Append(string.IsNullOrEmpty(codeBlock.language) ? "code" : sanitize_tag_name(codeBlock.language));
                sb.Append('>');
                sb.Append(xml_escape(codeBlock.text));
                sb.Append("</");
                sb.Append(string.IsNullOrEmpty(codeBlock.language) ? "code" : sanitize_tag_name(codeBlock.language));
                sb.AppendLine(">");
                break;
            }
            case BulletList bulletList:
            {
                write_indent(sb, indent);
                sb.AppendLine("<list>");
                foreach (var item in bulletList.items)
                {
                    write_indent(sb, indent + 1);
                    sb.AppendLine("<item>");
                    foreach (var itemBlock in item) write_block_as_xml(sb, itemBlock, indent + 2);

                    write_indent(sb, indent + 1);
                    sb.AppendLine("</item>");
                }

                write_indent(sb, indent);
                sb.AppendLine("</list>");
                break;
            }
            case OrderedList orderedList:
            {
                write_indent(sb, indent);
                sb.AppendLine("<list>");
                foreach (var item in orderedList.items)
                {
                    write_indent(sb, indent + 1);
                    sb.AppendLine("<item>");
                    foreach (var itemBlock in item) write_block_as_xml(sb, itemBlock, indent + 2);

                    write_indent(sb, indent + 1);
                    sb.AppendLine("</item>");
                }

                write_indent(sb, indent);
                sb.AppendLine("</list>");
                break;
            }
            case Table table:
                write_table_as_xml(sb, table, indent);
                break;
            case HorizontalRule:
                write_indent(sb, indent);
                sb.AppendLine("<hr />");
                break;
            case BlockQuote blockQuote:
            {
                write_indent(sb, indent);
                sb.AppendLine("<blockquote>");
                foreach (var child in blockQuote.children) write_block_as_xml(sb, child, indent + 1);

                write_indent(sb, indent);
                sb.AppendLine("</blockquote>");
                break;
            }
        }
    }

    private static void write_table_as_xml(StringBuilder sb, Table table, int indent)
    {
        write_indent(sb, indent);
        sb.AppendLine("<table>");

        var allRows = new List<TableRow>();
        allRows.AddRange(table.head.rows);
        foreach (var body in table.bodies)
        {
            allRows.AddRange(body.head_rows);
            allRows.AddRange(body.rows);
        }

        allRows.AddRange(table.foot.rows);

        foreach (var row in allRows)
        {
            write_indent(sb, indent + 1);
            sb.AppendLine("<row>");
            foreach (var cell in row.cells)
            {
                write_indent(sb, indent + 2);
                sb.Append("<cell>");
                sb.Append(xml_escape(cells_to_text(cell)));
                sb.AppendLine("</cell>");
            }

            write_indent(sb, indent + 1);
            sb.AppendLine("</row>");
        }

        write_indent(sb, indent);
        sb.AppendLine("</table>");
    }

    private static string inlines_to_text(IReadOnlyList<NotedownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines) append_inline_text(sb, inline);

        return sb.ToString();
    }

    private static string cells_to_text(IReadOnlyList<NotedownInline> cells)
    {
        return inlines_to_text(cells);
    }

    private static void append_inline_text(StringBuilder sb, NotedownInline inline)
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
                sb.Append('\n');
                break;
            case LineBreak:
                sb.Append("\n\n");
                break;
            case Code code:
                sb.Append(code.text);
                break;
            case Emph emph:
                foreach (var child in emph.inlines) append_inline_text(sb, child);

                break;
            case Strong strong:
                foreach (var child in strong.inlines) append_inline_text(sb, child);

                break;
            case Link link:
                foreach (var child in link.inlines) append_inline_text(sb, child);

                break;
        }
    }

    private static string sanitize_tag_name(string name)
    {
        var sb = new StringBuilder();
        foreach (var ch in name)
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')
                sb.Append(ch);

        if (sb.Length == 0) return "item";

        if (char.IsDigit(sb[0])) sb.Insert(0, '_');

        return sb.ToString();
    }

    private static string xml_escape(string value)
    {
        var sb = new StringBuilder();
        foreach (var ch in value)
            switch (ch)
            {
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '&':
                    sb.Append("&amp;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&apos;");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }

        return sb.ToString();
    }

    private static void write_indent(StringBuilder sb, int level)
    {
        sb.Append(new string(' ', level * 2));
    }

    #endregion
}