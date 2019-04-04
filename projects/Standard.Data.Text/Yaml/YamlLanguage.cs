using System.Globalization;
using System.Text;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 语言前端，封装解析以及与 Notedown IR 的双向转换
/// </summary>
public sealed class YamlLanguage : Language
{
    private readonly YamlParser _parser;

    /// <summary>
    ///     创建 YAML 语言实例
    /// </summary>
    public YamlLanguage()
    {
        _parser = new YamlParser();
    }

    /// <inheritdoc />
    public override string name => "YAML";

    /// <summary>
    ///     解析 YAML 文本为 YamlValue AST
    /// </summary>
    public YamlValue parse(string source)
    {
        var result = _parser.parse(source);
        return result.value ?? YamlNull.instance;
    }

    /// <inheritdoc />
    public override NotedownDocument to_notedown(object ast)
    {
        var value = (YamlValue)ast;
        var meta = convert_to_meta(value);
        var blocks = convert_value_to_blocks(value);
        return new NotedownDocument(meta, blocks);
    }

    /// <inheritdoc />
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();
        write_meta(sb, document.meta, 0);
        if (sb.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        write_blocks(sb, document.blocks, 0);
        return sb.ToString();
    }

    #region ToNotedown

    private static Meta convert_to_meta(YamlValue value)
    {
        if (value is not YamlMapping mapping) return Meta.empty;

        var meta = Meta.empty;
        foreach (var (key, val) in mapping.properties) meta = meta.with_value(key, convert_to_meta_value(val));

        return meta;
    }

    private static MetaValue convert_to_meta_value(YamlValue value)
    {
        return value switch
        {
            YamlNull => MetaValue.from_string("null"),
            YamlBoolean b => MetaValue.from_bool(b.value),
            YamlNumber n => MetaValue.from_string(n.value.ToString(CultureInfo.InvariantCulture)),
            YamlString s => MetaValue.from_string(s.value),
            YamlSequence seq => new MetaValue.MetaList([.. seq.items.Select(convert_to_meta_value)]),
            YamlMapping map => new MetaValue.MetaMap(
                map.properties.ToDictionary(p => p.Key, p => convert_to_meta_value(p.Value))),
            _ => MetaValue.from_string(string.Empty)
        };
    }

    private static List<NotedownBlock> convert_value_to_blocks(YamlValue value)
    {
        return value switch
        {
            YamlMapping mapping => convert_mapping(mapping),
            YamlSequence sequence => convert_sequence(sequence),
            _ => [new Para([new Str(value_to_text(value))])]
        };
    }

    private static List<NotedownBlock> convert_mapping(YamlMapping mapping)
    {
        var blocks = new List<NotedownBlock>();

        foreach (var (key, val) in mapping.properties)
            if (val is YamlMapping nestedMap)
            {
                blocks.Add(new Header(2, [new Str(key)]));
                blocks.AddRange(convert_mapping(nestedMap));
            }
            else if (val is YamlSequence nestedSeq)
            {
                blocks.Add(new Header(2, [new Str(key)]));
                blocks.AddRange(convert_sequence(nestedSeq));
            }
            else
            {
                var inlines = new List<NotedownInline>
                {
                    new Strong([new Str(key)]),
                    new Str(": "),
                    new Str(value_to_text(val))
                };
                blocks.Add(new Para(inlines));
            }

        return blocks;
    }

    private static List<NotedownBlock> convert_sequence(YamlSequence sequence)
    {
        if (sequence.items.Count == 0) return [new Para([new Str("[]")])];

        var items = new List<IReadOnlyList<NotedownBlock>>();

        foreach (var item in sequence.items)
            if (item is YamlMapping map)
                items.Add(convert_mapping(map));
            else
                items.Add([new Plain([new Str(value_to_text(item))])]);

        return [new BulletList(items)];
    }

    private static string value_to_text(YamlValue value)
    {
        return value switch
        {
            YamlNull => "null",
            YamlBoolean b => b.value ? "true" : "false",
            YamlNumber n => n.value.ToString(CultureInfo.InvariantCulture),
            YamlString s => s.value,
            YamlSequence => "[...]",
            YamlMapping => "{...}",
            _ => string.Empty
        };
    }

    #endregion

    #region FromNotedown

    private static void write_meta(StringBuilder sb, Meta meta, int indent)
    {
        foreach (var (key, value) in meta.values)
        {
            write_indent(sb, indent);
            sb.Append(key);
            sb.Append(": ");
            write_meta_value(sb, value, indent);
        }
    }

    private static void write_meta_value(StringBuilder sb, MetaValue value, int indent)
    {
        switch (value)
        {
            case MetaValue.MetaString str:
                sb.AppendLine(yaml_escape(str.value));
                break;
            case MetaValue.MetaBool b:
                sb.AppendLine(b.value ? "true" : "false");
                break;
            case MetaValue.MetaList list:
                sb.AppendLine();
                foreach (var item in list.values)
                {
                    write_indent(sb, indent + 1);
                    sb.Append("- ");
                    write_meta_value(sb, item, indent + 1);
                }

                break;
            case MetaValue.MetaMap map:
                sb.AppendLine();
                foreach (var (k, v) in map.values)
                {
                    write_indent(sb, indent + 1);
                    sb.Append(k);
                    sb.Append(": ");
                    write_meta_value(sb, v, indent + 1);
                }

                break;
        }
    }

    private static void write_blocks(StringBuilder sb, IReadOnlyList<NotedownBlock> blocks, int indent)
    {
        foreach (var block in blocks) write_block(sb, block, indent);
    }

    private static void write_block(StringBuilder sb, NotedownBlock block, int indent)
    {
        switch (block)
        {
            case Header header:
                write_indent(sb, indent);
                sb.Append(inlines_to_text(header.inlines));
                sb.Append(':');
                sb.AppendLine();
                break;
            case Para para:
                write_indent(sb, indent);
                sb.Append("- ");
                sb.AppendLine(inlines_to_text(para.inlines));
                break;
            case Plain plain:
                write_indent(sb, indent);
                sb.AppendLine(inlines_to_text(plain.inlines));
                break;
            case CodeBlock codeBlock:
                write_indent(sb, indent);
                sb.Append(yaml_escape(codeBlock.text));
                sb.AppendLine();
                break;
            case BulletList bulletList:
                write_bullet_list(sb, bulletList, indent);
                break;
            case OrderedList orderedList:
                write_ordered_list(sb, orderedList, indent);
                break;
            case BlockQuote blockQuote:
                write_blocks(sb, blockQuote.children, indent + 1);
                break;
            case HorizontalRule:
                write_indent(sb, indent);
                sb.AppendLine("---");
                break;
            case Table table:
                write_table(sb, table, indent);
                break;
        }
    }

    private static void write_bullet_list(StringBuilder sb, BulletList list, int indent)
    {
        foreach (var item in list.items)
            if (item is [Plain plain])
            {
                write_indent(sb, indent);
                sb.Append("- ");
                sb.AppendLine(inlines_to_text(plain.inlines));
            }
            else
            {
                write_indent(sb, indent);
                sb.AppendLine("-");
                write_blocks(sb, item, indent + 1);
            }
    }

    private static void write_ordered_list(StringBuilder sb, OrderedList list, int indent)
    {
        for (var i = 0; i < list.items.Count; i++)
            if (list.items[i].Count == 1 && list.items[i][0] is Plain plain)
            {
                write_indent(sb, indent);
                sb.Append("- ");
                sb.AppendLine(inlines_to_text(plain.inlines));
            }
            else
            {
                write_indent(sb, indent);
                sb.AppendLine("-");
                write_blocks(sb, list.items[i], indent + 1);
            }
    }

    private static void write_table(StringBuilder sb, Table table, int indent)
    {
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
            write_indent(sb, indent);
            sb.Append("- ");
            sb.AppendLine(string.Join(", ", row.cells.Select(cells_to_text)));
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

    private static void write_indent(StringBuilder sb, int level)
    {
        sb.Append(new string(' ', level * 2));
    }

    private static string yaml_escape(string value)
    {
        if (value.Contains('\n') || value.Contains(':') || value.Contains('#') || value.Contains('"') ||
            value.Contains('\'')) return $"\"{value.Replace("\"", "\\\"")}\"";

        if (string.IsNullOrEmpty(value) || value == "null" || value == "true" || value == "false" ||
            value.StartsWith("---")) return $"\"{value}\"";

        return value;
    }

    #endregion
}