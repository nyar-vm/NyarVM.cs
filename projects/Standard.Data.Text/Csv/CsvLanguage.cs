using System.Text;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Csv;

/// <summary>
///     CSV 语言前端，封装解析以及与 Notedown IR 的双向转换
/// </summary>
public sealed class CsvLanguage : Language
{
    /// <summary>
    ///     创建 CSV 语言实例（使用默认配置）
    /// </summary>
    public CsvLanguage()
        : this(CsvLanguageConfig.@default)
    {
    }

    /// <summary>
    ///     创建 CSV 语言实例
    /// </summary>
    public CsvLanguage(CsvLanguageConfig config)
    {
        this.config = config;
    }

    /// <inheritdoc />
    public override string name => "CSV";

    /// <summary>
    ///     语言配置
    /// </summary>
    public CsvLanguageConfig config { get; }

    /// <inheritdoc />
    public override NotedownDocument to_notedown(object ast)
    {
        var source = (string)ast;
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var rows = new List<List<string>>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var cells = parse_csv_line(trimmed);
            if (cells.Count > 0) rows.Add(cells);
        }

        if (rows.Count == 0) return NotedownDocument.empty;

        var tableRows = new List<TableRow>();
        for (var i = 0; i < rows.Count; i++)
        {
            var cells = rows[i].Select(c => (IReadOnlyList<NotedownInline>)new List<NotedownInline> { new Str(c) })
                .ToList();
            tableRows.Add(new TableRow(cells));
        }

        TableHead head;
        List<TableRow> bodyRows;
        if (config.has_header && tableRows.Count > 1)
        {
            head = new TableHead(Attr.empty, [tableRows[0]]);
            bodyRows = [.. tableRows.Skip(1)];
        }
        else
        {
            head = new TableHead(Attr.empty, [tableRows[0]]);
            bodyRows = [.. tableRows.Skip(1)];
        }

        var table = new Table(
            Attr.empty,
            null,
            [],
            head,
            [new TableBody(Attr.empty, new RowHeadColumns(0), [], bodyRows)],
            new TableFoot(Attr.empty, []));

        return new NotedownDocument([table]);
    }

    /// <inheritdoc />
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();

        foreach (var block in document.blocks)
            if (block is Table table)
            {
                write_table_as_csv(sb, table);
            }
            else
            {
                var text = inlines_to_text(block);
                if (!string.IsNullOrEmpty(text)) sb.AppendLine(csv_escape(text));
            }

        return sb.ToString();
    }

    #region 解析辅助

    private static List<string> parse_csv_line(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            else
            {
                if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    cells.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }

    #endregion

    #region 格式化辅助

    private void write_table_as_csv(StringBuilder sb, Table table)
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
            for (var i = 0; i < row.cells.Count; i++)
            {
                if (i > 0) sb.Append(',');

                sb.Append(csv_escape(cells_to_text(row.cells[i])));
            }

            sb.AppendLine();
        }
    }

    private static string cells_to_text(IReadOnlyList<NotedownInline> cells)
    {
        var sb = new StringBuilder();
        foreach (var inline in cells) append_inline_text(sb, inline);

        return sb.ToString();
    }

    private static string inlines_to_text(NotedownBlock block)
    {
        return block switch
        {
            Para para => inlines_to_plain_text(para.inlines),
            Plain plain => inlines_to_plain_text(plain.inlines),
            Header header => inlines_to_plain_text(header.inlines),
            CodeBlock codeBlock => codeBlock.text,
            _ => string.Empty
        };
    }

    private static string inlines_to_plain_text(IReadOnlyList<NotedownInline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines) append_inline_text(sb, inline);

        return sb.ToString();
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

    private static string csv_escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    #endregion
}