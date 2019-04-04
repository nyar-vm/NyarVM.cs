using System.Globalization;
using System.Text;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;
using Std.Data.Text.Toml.Ast;

namespace Std.Data.Text.Toml;

/// <summary>
///     TOML 语言配置
/// </summary>
public sealed class TomlLanguage : Language
{
    private readonly TomlParser _parser;

    /// <summary>
    ///     创建 TOML 语言实例
    /// </summary>
    public TomlLanguage()
    {
        _parser = new TomlParser();
    }

    /// <inheritdoc />
    public override string name => "TOML";

    /// <summary>
    ///     是否允许内联表
    /// </summary>
    public bool allow_inline_tables { get; init; } = true;

    /// <summary>
    ///     是否允许多行字符串
    /// </summary>
    public bool allow_multiline_strings { get; init; } = true;

    /// <summary>
    ///     是否允许表达式
    /// </summary>
    public bool allow_expressions { get; init; }

    /// <summary>
    ///     是否允许日期时间字面量
    /// </summary>
    public bool allow_date_time { get; init; } = true;

    /// <summary>
    ///     解析 TOML 文本为 AST
    /// </summary>
    public TomlTable? parse(string source)
    {
        var result = _parser.parse(source);
        return result.root;
    }

    /// <summary>
    ///     将 TOML AST 转换为 Notedown IR
    /// </summary>
    public override NotedownDocument to_notedown(object ast)
    {
        var table = (TomlTable)ast;
        if (table is null) return NotedownDocument.empty;

        var blocks = convert_table(table);
        return new NotedownDocument(blocks);
    }

    /// <summary>
    ///     将 Notedown IR 转换为 TOML 文本
    /// </summary>
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();

        if (document.meta.values.Count > 0)
            write_meta(sb, document.meta);
        else
            write_blocks(sb, document.blocks, string.Empty);

        return sb.ToString();
    }

    #region ToNotedown

    private static List<NotedownBlock> convert_table(TomlTable table)
    {
        var blocks = new List<NotedownBlock>();

        if (!string.IsNullOrEmpty(table.name)) blocks.Add(new Header(2, [new Str(table.name)]));

        foreach (var (key, value) in table.entries)
            if (value.type == TomlValueType.inline_table)
            {
                blocks.Add(new Header(3, [new Str(key)]));
                var inlineTable = (Dictionary<string, TomlValue>)value.raw_value!;
                foreach (var (ik, iv) in inlineTable) blocks.Add(format_key_value(ik, iv));
            }
            else if (value.type == TomlValueType.array)
            {
                blocks.Add(new Header(3, [new Str(key)]));
                var items = (TomlValue[])value.raw_value!;
                blocks.AddRange(convert_array(items));
            }
            else
            {
                blocks.Add(format_key_value(key, value));
            }

        foreach (var (key, childTable) in table.tables) blocks.AddRange(convert_table(childTable));

        return blocks;
    }

    private static NotedownBlock format_key_value(string key, TomlValue value)
    {
        var inlines = new List<NotedownInline>
        {
            new Strong([new Str(key)]),
            new Str(": "),
            new Str(toml_value_to_text(value))
        };
        return new Para(inlines);
    }

    private static List<NotedownBlock> convert_array(TomlValue[] items)
    {
        if (items.Length == 0) return [new Para([new Str("[]")])];

        var listItems = new List<IReadOnlyList<NotedownBlock>>();

        foreach (var item in items)
            if (item.type == TomlValueType.inline_table)
            {
                var inlineTable = (Dictionary<string, TomlValue>)item.raw_value!;
                var subBlocks = new List<NotedownBlock>();
                foreach (var (ik, iv) in inlineTable) subBlocks.Add(format_key_value(ik, iv));

                listItems.Add(subBlocks);
            }
            else
            {
                listItems.Add([new Plain([new Str(toml_value_to_text(item))])]);
            }

        return [new BulletList(listItems)];
    }

    private static string toml_value_to_text(TomlValue value)
    {
        return value.type switch
        {
            TomlValueType.@string => (string)value.raw_value!,
            TomlValueType.integer => ((long)value.raw_value!).ToString(CultureInfo.InvariantCulture),
            TomlValueType.@float => ((double)value.raw_value!).ToString(CultureInfo.InvariantCulture),
            TomlValueType.boolean => (bool)value.raw_value! ? "true" : "false",
            TomlValueType.date_time or TomlValueType.date or TomlValueType.time => value.raw_value?.ToString() ??
                string.Empty,
            TomlValueType.array => "[...]",
            TomlValueType.inline_table => "{...}",
            _ => string.Empty
        };
    }

    #endregion

    #region FromNotedown

    private static void write_meta(StringBuilder sb, Meta meta)
    {
        foreach (var (key, value) in meta.values)
        {
            sb.Append(key);
            sb.Append(" = ");
            write_meta_value(sb, value);
            sb.AppendLine();
        }
    }

    private static void write_meta_value(StringBuilder sb, MetaValue value)
    {
        switch (value)
        {
            case MetaValue.MetaString str:
                sb.Append(toml_escape_string(str.value));
                break;
            case MetaValue.MetaBool b:
                sb.Append(b.value ? "true" : "false");
                break;
            case MetaValue.MetaList list:
                sb.Append('[');
                for (var i = 0; i < list.values.Count; i++)
                {
                    if (i > 0) sb.Append(", ");

                    write_meta_value(sb, list.values[i]);
                }

                sb.Append(']');
                break;
            case MetaValue.MetaMap map:
                sb.Append('{');
                var first = true;
                foreach (var (k, v) in map.values)
                {
                    if (!first) sb.Append(", ");

                    first = false;
                    sb.Append(k);
                    sb.Append(" = ");
                    write_meta_value(sb, v);
                }

                sb.Append('}');
                break;
        }
    }

    private static void write_blocks(StringBuilder sb, IReadOnlyList<NotedownBlock> blocks, string currentSection)
    {
        foreach (var block in blocks) write_block(sb, block, ref currentSection);
    }

    private static void write_block(StringBuilder sb, NotedownBlock block, ref string currentSection)
    {
        switch (block)
        {
            case Header header:
            {
                var sectionName = inlines_to_text(header.inlines);
                if (header.level <= 2)
                {
                    if (!string.IsNullOrEmpty(currentSection)) sb.AppendLine();

                    currentSection = sectionName;
                    sb.Append('[');
                    sb.Append(sectionName);
                    sb.AppendLine("]");
                }
                else
                {
                    var key = sectionName;
                    sb.Append(key);
                    sb.Append(" = { }");
                    sb.AppendLine();
                    sb.Append('[');
                    sb.Append(currentSection);
                    sb.Append('.');
                    sb.Append(key);
                    sb.AppendLine("]");
                }

                break;
            }
            case Para para:
            {
                var text = inlines_to_text(para.inlines);
                var colonIndex = text.IndexOf(": ", StringComparison.Ordinal);
                if (colonIndex > 0)
                {
                    var key = text[..colonIndex];
                    var value = text[(colonIndex + 2)..];
                    sb.Append(key);
                    sb.Append(" = ");
                    sb.AppendLine(toml_escape_string(value));
                }
                else
                {
                    sb.Append("# ");
                    sb.AppendLine(text);
                }

                break;
            }
            case Plain plain:
            {
                var text = inlines_to_text(plain.inlines);
                sb.Append("# ");
                sb.AppendLine(text);
                break;
            }
            case CodeBlock codeBlock:
                if (!string.IsNullOrEmpty(codeBlock.text))
                {
                    sb.Append("# ");
                    if (!string.IsNullOrEmpty(codeBlock.language))
                    {
                        sb.Append('[');
                        sb.Append(codeBlock.language);
                        sb.Append("] ");
                    }

                    sb.AppendLine(codeBlock.text.Replace("\n", "\n# "));
                }

                break;
            case BulletList bulletList:
                foreach (var item in bulletList.items)
                foreach (var itemBlock in item)
                    if (itemBlock is Plain itemPlain)
                    {
                        var itemText = inlines_to_text(itemPlain.inlines);
                        sb.Append("# - ");
                        sb.AppendLine(itemText);
                    }

                break;
            case HorizontalRule:
                sb.AppendLine("# ---");
                break;
            case Table table:
                write_table_as_toml(sb, table, currentSection);
                break;
        }
    }

    private static void write_table_as_toml(StringBuilder sb, Table table, string currentSection)
    {
        if (!string.IsNullOrEmpty(currentSection)) sb.AppendLine();

        var allRows = new List<TableRow>();
        allRows.AddRange(table.head.rows);
        foreach (var body in table.bodies)
        {
            allRows.AddRange(body.head_rows);
            allRows.AddRange(body.rows);
        }

        allRows.AddRange(table.foot.rows);

        if (allRows.Count == 0) return;

        var headerRow = allRows[0];
        var headers = headerRow.cells.Select(cells_to_text).ToList();

        sb.Append("[[");
        sb.Append(currentSection);
        sb.AppendLine(".");

        for (var i = 1; i < allRows.Count; i++)
        {
            sb.Append('[');
            sb.Append(currentSection);
            sb.Append('.');
            sb.Append(headers[0]);
            sb.Append(']');
            sb.AppendLine();

            for (var j = 0; j < allRows[i].cells.Count && j < headers.Count; j++)
            {
                sb.Append(headers[j]);
                sb.Append(" = ");
                sb.AppendLine(toml_escape_string(cells_to_text(allRows[i].cells[j])));
            }

            sb.AppendLine();
        }
    }

    private static string inlines_to_text(IReadOnlyList<NotedownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines)
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
                    sb.Append(inlines_to_text(emph.inlines));
                    break;
                case Strong strong:
                    sb.Append(inlines_to_text(strong.inlines));
                    break;
                case Link link:
                    sb.Append(inlines_to_text(link.inlines));
                    break;
            }

        return sb.ToString();
    }

    private static string cells_to_text(IReadOnlyList<NotedownInline> cells)
    {
        return inlines_to_text(cells);
    }

    private static string toml_escape_string(string value)
    {
        if (value.Contains('"') || value.Contains('\\') || value.Contains('\n'))
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")}\"";

        return $"\"{value}\"";
    }

    #endregion
}