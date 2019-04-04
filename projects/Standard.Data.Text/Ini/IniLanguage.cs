using System.Text;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Ini;

/// <summary>
///     INI 语言配置
/// </summary>
public sealed class IniLanguage : Language
{
    private readonly IniParser _parser;

    /// <summary>
    ///     创建 INI 语言实例
    /// </summary>
    public IniLanguage()
    {
        _parser = new IniParser();
    }

    /// <inheritdoc />
    public override string name => "INI";

    /// <summary>
    ///     注释分隔符（默认 ";"）
    /// </summary>
    public string comment_delimiter { get; init; } = ";";

    /// <summary>
    ///     是否允许 # 作为注释
    /// </summary>
    public bool allow_hash_comments { get; init; } = true;

    /// <summary>
    ///     是否允许多行值
    /// </summary>
    public bool allow_multiline_values { get; init; }

    /// <summary>
    ///     是否允许无节键值对
    /// </summary>
    public bool allow_global_keys { get; init; } = true;

    /// <summary>
    ///     解析 INI 文本为 AST
    /// </summary>
    public IniParseResult parse(string source)
    {
        return _parser.parse(source);
    }

    /// <summary>
    ///     将 INI AST 转换为 Notedown IR
    /// </summary>
    public override NotedownDocument to_notedown(object ast)
    {
        var result = (IniParseResult)ast;
        var blocks = new List<NotedownBlock>();

        if (result.global_entries.Count > 0)
        {
            blocks.Add(new Header(2, [new Str("全局")]));

            foreach (var (key, value) in result.global_entries) blocks.Add(format_key_value(key, value));
        }

        foreach (var section in result.sections)
        {
            blocks.Add(new Header(2, [new Str(section.name)]));

            foreach (var (key, value) in section.entries) blocks.Add(format_key_value(key, value));
        }

        if (blocks.Count == 0) return NotedownDocument.empty;

        return new NotedownDocument(blocks);
    }

    /// <summary>
    ///     将 Notedown IR 转换为 INI 文本
    /// </summary>
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();
        var currentSection = string.Empty;

        if (document.meta.values.Count > 0)
        {
            foreach (var (key, value) in document.meta.values)
            {
                sb.Append(key);
                sb.Append('=');
                sb.AppendLine(meta_value_to_text(value));
            }

            if (sb.Length > 0) sb.AppendLine();
        }

        foreach (var block in document.blocks) write_block(sb, block, ref currentSection);

        return sb.ToString();
    }

    #region ToNotedown

    private static NotedownBlock format_key_value(string key, string value)
    {
        var inlines = new List<NotedownInline>
        {
            new Strong([new Str(key)]),
            new Str(": "),
            new Str(value)
        };
        return new Para(inlines);
    }

    #endregion

    #region FromNotedown

    private static void write_block(StringBuilder sb, NotedownBlock block, ref string currentSection)
    {
        switch (block)
        {
            case Header header:
            {
                var sectionName = inlines_to_text(header.inlines);
                if (header.level == 1)
                {
                    if (!string.IsNullOrEmpty(currentSection)) sb.AppendLine();

                    currentSection = sectionName;
                    sb.Append('[');
                    sb.Append(sectionName);
                    sb.AppendLine("]");
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentSection)) sb.AppendLine();

                    currentSection = sectionName;
                    sb.Append('[');
                    sb.Append(sectionName);
                    sb.AppendLine("]");
                }

                break;
            }
            case Para para:
            {
                var text = inlines_to_text(para.inlines);
                var eqIndex = text.IndexOf(": ", StringComparison.Ordinal);
                if (eqIndex > 0)
                {
                    var key = text[..eqIndex];
                    var value = text[(eqIndex + 2)..];
                    sb.Append(key);
                    sb.Append('=');
                    sb.AppendLine(value);
                }
                else
                {
                    sb.Append(';');
                    sb.Append(' ');
                    sb.AppendLine(text);
                }

                break;
            }
            case Plain plain:
            {
                var text = inlines_to_text(plain.inlines);
                sb.Append(';');
                sb.Append(' ');
                sb.AppendLine(text);
                break;
            }
            case CodeBlock codeBlock:
                if (!string.IsNullOrEmpty(codeBlock.text))
                {
                    sb.Append(';');
                    sb.Append(' ');
                    sb.AppendLine(codeBlock.text.Replace("\n", "\n; "));
                }

                break;
            case BulletList bulletList:
                foreach (var item in bulletList.items)
                foreach (var itemBlock in item)
                    if (itemBlock is Plain itemPlain)
                    {
                        sb.Append(';');
                        sb.Append(' ');
                        sb.Append('-');
                        sb.Append(' ');
                        sb.AppendLine(inlines_to_text(itemPlain.inlines));
                    }

                break;
            case HorizontalRule:
                sb.AppendLine("; ---");
                break;
            case Table table:
                write_table_as_ini(sb, table, currentSection);
                break;
        }
    }

    private static void write_table_as_ini(StringBuilder sb, Table table, string currentSection)
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

        if (!string.IsNullOrEmpty(currentSection)) sb.AppendLine();

        var headerRow = allRows[0];
        var headers = headerRow.cells.Select(cells_to_text).ToList();

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
                sb.Append('=');
                sb.AppendLine(cells_to_text(allRows[i].cells[j]));
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

    private static string meta_value_to_text(MetaValue value)
    {
        return value switch
        {
            MetaValue.MetaString str => str.value,
            MetaValue.MetaBool b => b.value ? "true" : "false",
            MetaValue.MetaList list => string.Join(",", list.values.Select(meta_value_to_text)),
            MetaValue.MetaMap map => string.Join(",",
                map.values.Select(kv => $"{kv.Key}={meta_value_to_text(kv.Value)}")),
            _ => string.Empty
        };
    }

    #endregion
}