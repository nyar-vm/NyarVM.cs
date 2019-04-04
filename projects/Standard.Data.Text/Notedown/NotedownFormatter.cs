using System.Text;
using Std.Data.Text.Notedown.Syntax;

namespace Std.Data.Text.Notedown;

/// <summary>
///     Notedown 文本格式化器，将 Notedown AST 序列化为 Notedown 文本格式
///     Notedown 文本格式是 Markdown 的超集，能完整表达所有 AST 构造
/// </summary>
public sealed class NotedownFormatter
{
    private readonly NotedownLanguageConfig _config;
    private readonly StringBuilder _sb = new();
    private int _list_depth;

    /// <summary>
    ///     创建 Notedown 格式化器
    /// </summary>
    public NotedownFormatter(NotedownLanguageConfig? config = null)
    {
        _config = config ?? NotedownLanguageConfig.@default;
    }

    /// <summary>
    ///     将 Notedown 文档格式化为文本
    /// </summary>
    public string format(NotedownDocument document)
    {
        _sb.Clear();

        write_meta(document.meta);

        for (var i = 0; i < document.blocks.Count; i++)
        {
            if (i > 0) _sb.AppendLine();

            write_block(document.blocks[i]);
        }

        if (_sb.Length == 0 || _sb[^1] != '\n') _sb.AppendLine();

        return _sb.ToString();
    }

    #region 元数据

    private void write_meta(Meta meta)
    {
        if (meta.values.Count == 0) return;

        _sb.AppendLine("---");
        foreach (var (key, value) in meta.values) write_meta_value(key, value);

        _sb.AppendLine("---");
        _sb.AppendLine();
    }

    private void write_meta_value(string key, MetaValue value)
    {
        switch (value)
        {
            case MetaValue.MetaString str:
                _sb.Append(key);
                _sb.Append(": ");
                _sb.AppendLine(str.value);
                break;
            case MetaValue.MetaBool b:
                _sb.Append(key);
                _sb.Append(": ");
                _sb.AppendLine(b.value ? "true" : "false");
                break;
            case MetaValue.MetaList list:
                _sb.Append(key);
                _sb.Append(":\n");
                foreach (var item in list.values)
                {
                    _sb.Append("  - ");
                    write_meta_scalar(item);
                    _sb.AppendLine();
                }

                break;
            case MetaValue.MetaMap map:
                _sb.Append(key);
                _sb.AppendLine(":");
                foreach (var (k, v) in map.values)
                {
                    _sb.Append("  ");
                    write_meta_value(k, v);
                }

                break;
        }
    }

    private void write_meta_scalar(MetaValue value)
    {
        switch (value)
        {
            case MetaValue.MetaString str:
                _sb.Append(str.value);
                break;
            case MetaValue.MetaBool b:
                _sb.Append(b.value ? "true" : "false");
                break;
            default:
                _sb.Append(value);
                break;
        }
    }

    #endregion

    #region 块级写入

    private void write_block(NotedownBlock block)
    {
        switch (block)
        {
            case Header header:
                write_header(header);
                break;
            case Para para:
                write_para(para);
                break;
            case Plain plain:
                write_inlines(plain.inlines);
                break;
            case LineBlock lineBlock:
                write_line_block(lineBlock);
                break;
            case CodeBlock codeBlock:
                write_code_block(codeBlock);
                break;
            case RawBlock rawBlock:
                write_raw_block(rawBlock);
                break;
            case BlockQuote blockQuote:
                write_block_quote(blockQuote);
                break;
            case OrderedList orderedList:
                write_ordered_list(orderedList);
                break;
            case BulletList bulletList:
                write_bullet_list(bulletList);
                break;
            case DefinitionList definitionList:
                write_definition_list(definitionList);
                break;
            case HorizontalRule:
                _sb.AppendLine("---");
                break;
            case Table table:
                write_table(table);
                break;
            case Div div:
                write_div(div);
                break;
            case Null:
                break;
        }
    }

    private void write_header(Header header)
    {
        _sb.Append(new string('#', header.level));
        if (!header.attr.is_empty && !string.IsNullOrEmpty(header.attr.id))
        {
            _sb.Append(" {#");
            _sb.Append(header.attr.id);
            _sb.Append('}');
        }

        _sb.Append(' ');
        write_inlines(header.inlines);
        _sb.AppendLine();
    }

    private void write_para(Para para)
    {
        write_inlines(para.inlines);
        _sb.AppendLine();
    }

    private void write_line_block(LineBlock lineBlock)
    {
        foreach (var line in lineBlock.lines)
        {
            _sb.Append("| ");
            write_inlines(line);
            _sb.AppendLine();
        }
    }

    private void write_code_block(CodeBlock codeBlock)
    {
        if (!codeBlock.attr.is_empty)
        {
            _sb.Append("``` {");
            if (!string.IsNullOrEmpty(codeBlock.attr.id))
            {
                _sb.Append('#');
                _sb.Append(codeBlock.attr.id);
                _sb.Append(' ');
            }

            foreach (var cls in codeBlock.attr.classes)
            {
                _sb.Append('.');
                _sb.Append(cls);
                _sb.Append(' ');
            }

            _sb.AppendLine("}");
        }
        else if (!string.IsNullOrEmpty(codeBlock.language))
        {
            _sb.Append("```");
            _sb.AppendLine(codeBlock.language);
        }
        else
        {
            _sb.AppendLine("```");
        }

        _sb.AppendLine(codeBlock.text);
        _sb.AppendLine("```");
    }

    private void write_raw_block(RawBlock rawBlock)
    {
        _sb.Append("```{=");
        _sb.Append(rawBlock.format);
        _sb.AppendLine("}");
        _sb.AppendLine(rawBlock.text);
        _sb.AppendLine("```");
    }

    private void write_block_quote(BlockQuote blockQuote)
    {
        foreach (var child in blockQuote.children)
        {
            _sb.Append("> ");
            write_block_inline(child);
        }
    }

    private void write_block_inline(NotedownBlock block)
    {
        switch (block)
        {
            case Para para:
                write_inlines(para.inlines);
                _sb.AppendLine();
                break;
            case Plain plain:
                write_inlines(plain.inlines);
                _sb.AppendLine();
                break;
            case Header header:
                _sb.Append(new string('#', header.level));
                _sb.Append(' ');
                write_inlines(header.inlines);
                _sb.AppendLine();
                break;
            case CodeBlock codeBlock:
                _sb.Append("[代码块: ");
                _sb.Append(codeBlock.language);
                _sb.AppendLine("]");
                break;
            default:
                _sb.AppendLine("[块]");
                break;
        }
    }

    private void write_ordered_list(OrderedList orderedList)
    {
        var prevDepth = _list_depth;
        _list_depth++;

        var startNum = orderedList.list_attributes.start_number;
        for (var i = 0; i < orderedList.items.Count; i++)
        {
            var num = startNum + i;
            var marker = $"{num}. ";
            write_list_item(orderedList.items[i], marker);
        }

        _list_depth = prevDepth;
    }

    private void write_bullet_list(BulletList bulletList)
    {
        var prevDepth = _list_depth;
        _list_depth++;

        foreach (var item in bulletList.items) write_list_item(item, "- ");

        _list_depth = prevDepth;
    }

    private void write_list_item(IReadOnlyList<NotedownBlock> blocks, string marker)
    {
        var indent = new string(' ', marker.Length);

        for (var i = 0; i < blocks.Count; i++)
            if (i == 0)
            {
                _sb.Append(marker);
                switch (blocks[i])
                {
                    case Para para:
                        write_inlines(para.inlines);
                        _sb.AppendLine();
                        break;
                    case Plain plain:
                        write_inlines(plain.inlines);
                        _sb.AppendLine();
                        break;
                    default:
                        _sb.AppendLine();
                        _sb.Append(indent);
                        write_block(blocks[i]);
                        break;
                }
            }
            else
            {
                _sb.Append(indent);
                write_block(blocks[i]);
            }
    }

    private void write_definition_list(DefinitionList definitionList)
    {
        foreach (var item in definitionList.items)
        {
            write_inlines(item.term);
            _sb.AppendLine();

            foreach (var def in item.definitions)
            {
                _sb.Append(":   ");
                if (def.Count > 0 && def[0] is Plain or Para)
                    write_block_inline(def[0]);
                else
                    foreach (var block in def)
                    {
                        _sb.Append("    ");
                        write_block(block);
                    }
            }
        }
    }

    private void write_table(Table table)
    {
        if (table.caption is not null)
        {
            _sb.Append("Table: ");
            write_inlines(table.caption.inlines);
            _sb.AppendLine();
        }

        var allRows = new List<TableRow>();
        allRows.AddRange(table.head.rows);
        foreach (var body in table.bodies)
        {
            allRows.AddRange(body.head_rows);
            allRows.AddRange(body.rows);
        }

        allRows.AddRange(table.foot.rows);

        if (allRows.Count == 0) return;

        var colWidths = calculate_column_widths(allRows);

        write_table_row(allRows[0], colWidths);
        write_table_delimiter(colWidths);

        for (var i = 1; i < allRows.Count; i++) write_table_row(allRows[i], colWidths);

        write_table_delimiter(colWidths);
    }

    private int[] calculate_column_widths(IReadOnlyList<TableRow> rows)
    {
        var maxCols = 0;
        foreach (var row in rows) maxCols = System.Math.Max(maxCols, row.cells.Count);

        var widths = new int[maxCols];
        foreach (var row in rows)
            for (var i = 0; i < row.cells.Count && i < maxCols; i++)
            {
                var text = inlines_to_text(row.cells[i]);
                widths[i] = System.Math.Max(widths[i], text.Length);
            }

        return widths;
    }

    private void write_table_row(TableRow row, int[] colWidths)
    {
        _sb.Append("| ");
        for (var i = 0; i < colWidths.Length; i++)
        {
            if (i < row.cells.Count)
            {
                var text = inlines_to_text(row.cells[i]);
                _sb.Append(text.PadRight(colWidths[i]));
            }
            else
            {
                _sb.Append(new string(' ', colWidths[i]));
            }

            _sb.Append(" | ");
        }

        _sb.AppendLine();
    }

    private void write_table_delimiter(int[] colWidths)
    {
        _sb.Append("| ");
        foreach (var width in colWidths)
        {
            _sb.Append(new string('-', System.Math.Max(width, 3)));
            _sb.Append(" | ");
        }

        _sb.AppendLine();
    }

    private string inlines_to_text(IReadOnlyList<NotedownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines) sb.Append(inline_to_text(inline));

        return sb.ToString();
    }

    private string inline_to_text(NotedownInline inline)
    {
        return inline switch
        {
            Str str => str.text,
            Code code => code.text,
            Syntax.Math math => math.text,
            Space => " ",
            _ => string.Empty
        };
    }

    private void write_div(Div div)
    {
        if (!div.attr.is_empty)
        {
            _sb.Append(":::{");
            if (!string.IsNullOrEmpty(div.attr.id))
            {
                _sb.Append('#');
                _sb.Append(div.attr.id);
                _sb.Append(' ');
            }

            foreach (var cls in div.attr.classes)
            {
                _sb.Append('.');
                _sb.Append(cls);
                _sb.Append(' ');
            }

            _sb.Append('}');
        }
        else
        {
            _sb.Append(":::");
        }

        _sb.AppendLine();

        foreach (var child in div.children) write_block(child);

        _sb.AppendLine(":::");
    }

    #endregion

    #region 行内写入

    private void write_inlines(IReadOnlyList<NotedownInline> inlines)
    {
        foreach (var inline in inlines) write_inline(inline);
    }

    private void write_inline(NotedownInline inline)
    {
        switch (inline)
        {
            case Str str:
                _sb.Append(str.text);
                break;
            case Space:
                _sb.Append(' ');
                break;
            case SoftBreak:
                _sb.AppendLine();
                break;
            case LineBreak:
                _sb.AppendLine();
                _sb.AppendLine();
                break;
            case Emph emph:
                _sb.Append('*');
                write_inlines(emph.inlines);
                _sb.Append('*');
                break;
            case Strong strong:
                _sb.Append("**");
                write_inlines(strong.inlines);
                _sb.Append("**");
                break;
            case Strikeout strikeout:
                _sb.Append("~~");
                write_inlines(strikeout.inlines);
                _sb.Append("~~");
                break;
            case Superscript superscript:
                _sb.Append('^');
                write_inlines(superscript.inlines);
                _sb.Append('^');
                break;
            case Subscript subscript:
                _sb.Append('~');
                write_inlines(subscript.inlines);
                _sb.Append('~');
                break;
            case SmallCaps smallCaps:
                _sb.Append("[.smallcaps]");
                write_inlines(smallCaps.inlines);
                _sb.Append("[/.smallcaps]");
                break;
            case Quoted quoted:
                _sb.Append(quoted.quote_type == QuoteType.double_quote ? '"' : '\'');
                write_inlines(quoted.inlines);
                _sb.Append(quoted.quote_type == QuoteType.double_quote ? '"' : '\'');
                break;
            case Cite cite:
                _sb.Append('[');
                foreach (var citation in cite.citations)
                {
                    _sb.Append('@');
                    _sb.Append(citation.id);
                }

                _sb.Append(']');
                break;
            case Code code:
                if (!code.attr.is_empty && !string.IsNullOrEmpty(code.attr.id))
                {
                    _sb.Append('`');
                    _sb.Append(code.text);
                    _sb.Append("`{#");
                    _sb.Append(code.attr.id);
                    _sb.Append('}');
                }
                else
                {
                    _sb.Append('`');
                    _sb.Append(code.text);
                    _sb.Append('`');
                }

                break;
            case Syntax.Math math:
                if (math.math_type == MathType.display)
                {
                    _sb.Append("$$");
                    _sb.Append(math.text);
                    _sb.Append("$$");
                }
                else
                {
                    _sb.Append('$');
                    _sb.Append(math.text);
                    _sb.Append('$');
                }

                break;
            case RawInline rawInline:
                _sb.Append("`");
                _sb.Append(rawInline.text);
                _sb.Append("`{=");
                _sb.Append(rawInline.format);
                _sb.Append('}');
                break;
            case Link link:
                _sb.Append('[');
                write_inlines(link.inlines);
                _sb.Append("](");
                _sb.Append(link.target.url);
                if (!string.IsNullOrEmpty(link.target.title))
                {
                    _sb.Append(" \"");
                    _sb.Append(link.target.title);
                    _sb.Append('"');
                }

                _sb.Append(')');

                if (!link.attr.is_empty && !string.IsNullOrEmpty(link.attr.id))
                {
                    _sb.Append("{#");
                    _sb.Append(link.attr.id);
                    _sb.Append(" .");
                    _sb.Append(string.Join(" .", link.attr.classes));
                    _sb.Append('}');
                }

                break;
            case Image image:
                _sb.Append("![");
                write_inlines(image.inlines);
                _sb.Append("](");
                _sb.Append(image.target.url);
                if (!string.IsNullOrEmpty(image.target.title))
                {
                    _sb.Append(" \"");
                    _sb.Append(image.target.title);
                    _sb.Append('"');
                }

                _sb.Append(')');

                if (!image.attr.is_empty && !string.IsNullOrEmpty(image.attr.id))
                {
                    _sb.Append("{#");
                    _sb.Append(image.attr.id);
                    _sb.Append(" .");
                    _sb.Append(string.Join(" .", image.attr.classes));
                    _sb.Append('}');
                }

                break;
            case Note note:
                _sb.Append("[^fn]");
                break;
            case Span span:
                if (!span.attr.is_empty)
                {
                    _sb.Append('[');
                    if (!string.IsNullOrEmpty(span.attr.id))
                    {
                        _sb.Append('#');
                        _sb.Append(span.attr.id);
                    }

                    foreach (var cls in span.attr.classes)
                    {
                        _sb.Append('.');
                        _sb.Append(cls);
                    }

                    _sb.Append(']');
                }

                write_inlines(span.inlines);
                break;
        }
    }

    #endregion
}