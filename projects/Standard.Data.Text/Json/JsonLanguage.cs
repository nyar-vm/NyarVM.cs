using System.Text;
using System.Text.Json;
using Std.Data.Text.Notedown.Syntax;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Json;

/// <summary>
///     JSON 语言前端，封装解析以及与 Notedown IR 的双向转换
/// </summary>
public sealed class JsonLanguage : Language
{
    /// <inheritdoc />
    public override string name => "JSON";

    /// <summary>
    ///     将 JSON 文本解析为 Notedown IR
    /// </summary>
    public override NotedownDocument to_notedown(object ast)
    {
        var source = (string)ast;
        try
        {
            using var doc = JsonDocument.Parse(source);
            var blocks = convert_element(doc.RootElement, 1);
            return new NotedownDocument(blocks);
        }
        catch (JsonException)
        {
            return NotedownDocument.empty;
        }
    }

    /// <summary>
    ///     将 Notedown IR 转换为 JSON 文本
    /// </summary>
    public override object from_notedown(NotedownDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        var first = true;
        if (document.meta.values.Count > 0)
            foreach (var (key, value) in document.meta.values)
            {
                if (!first) sb.AppendLine(",");

                first = false;
                sb.Append("  \"");
                sb.Append(key);
                sb.Append("\": ");
                write_meta_value(sb, value);
            }

        foreach (var block in document.blocks)
        {
            if (!first) sb.AppendLine(",");

            first = false;
            write_block_as_json(sb, block, 1);
        }

        if (!first) sb.AppendLine();

        sb.AppendLine("}");
        return sb.ToString();
    }

    #region 解析

    private static List<NotedownBlock> convert_element(JsonElement element, int level)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => convert_object(element, level),
            JsonValueKind.Array => convert_array(element, level),
            _ => [new Para([new Str(element_to_text(element))])]
        };
    }

    private static List<NotedownBlock> convert_object(JsonElement element, int level)
    {
        var blocks = new List<NotedownBlock>();
        foreach (var property in element.EnumerateObject())
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                blocks.Add(new Header(level, [new Str(property.Name)]));
                blocks.AddRange(convert_object(property.Value, System.Math.Min(level + 1, 6)));
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                blocks.Add(new Header(level, [new Str(property.Name)]));
                blocks.AddRange(convert_array(property.Value, level + 1));
            }
            else
            {
                var inlines = new List<NotedownInline>
                {
                    new Strong([new Str(property.Name)]),
                    new Str(": "),
                    new Str(element_to_text(property.Value))
                };
                blocks.Add(new Para(inlines));
            }

        return blocks;
    }

    private static List<NotedownBlock> convert_array(JsonElement element, int level)
    {
        var items = new List<IReadOnlyList<NotedownBlock>>();
        foreach (var item in element.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object)
                items.Add(convert_object(item, level));
            else
                items.Add([new Plain([new Str(element_to_text(item))])]);

        return [new BulletList(items)];
    }

    private static string element_to_text(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => "[...]",
            JsonValueKind.Object => "{...}",
            _ => string.Empty
        };
    }

    #endregion

    #region 格式化

    private static void write_block_as_json(StringBuilder sb, NotedownBlock block, int indent)
    {
        switch (block)
        {
            case Header header:
                var key = inlines_to_text(header.inlines);
                sb.Append(new string(' ', indent * 2));
                sb.Append('"');
                sb.Append(key);
                sb.Append("\": {}");
                break;
            case Para para:
                var text = inlines_to_text(para.inlines);
                var colonIdx = text.IndexOf(": ", StringComparison.Ordinal);
                if (colonIdx > 0)
                {
                    var pKey = text[..colonIdx];
                    var pValue = text[(colonIdx + 2)..];
                    sb.Append(new string(' ', indent * 2));
                    sb.Append('"');
                    sb.Append(pKey);
                    sb.Append("\": ");
                    sb.Append(json_escape(pValue));
                }

                break;
            case BulletList bulletList:
            {
                var listKey = "list";
                sb.Append(new string(' ', indent * 2));
                sb.Append('"');
                sb.Append(listKey);
                sb.Append("\": [");
                if (bulletList.items.Count > 0)
                {
                    sb.AppendLine();
                    for (var i = 0; i < bulletList.items.Count; i++)
                    {
                        var item = bulletList.items[i];
                        sb.Append(new string(' ', (indent + 1) * 2));
                        if (item is [Plain p])
                            sb.Append(json_escape(inlines_to_text(p.inlines)));
                        else
                            sb.Append("{}");

                        if (i < bulletList.items.Count - 1) sb.Append(',');

                        sb.AppendLine();
                    }

                    sb.Append(new string(' ', indent * 2));
                }

                sb.Append(']');
                break;
            }
            case CodeBlock codeBlock:
            {
                sb.Append(new string(' ', indent * 2));
                sb.Append('"');
                sb.Append(codeBlock.language.Length > 0 ? codeBlock.language : "code");
                sb.Append("\": ");
                sb.Append(json_escape(codeBlock.text));
                break;
            }
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

    private static void write_meta_value(StringBuilder sb, MetaValue value)
    {
        switch (value)
        {
            case MetaValue.MetaString str:
                sb.Append(json_escape(str.value));
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
                    sb.Append('"');
                    sb.Append(k);
                    sb.Append("\": ");
                    write_meta_value(sb, v);
                }

                sb.Append('}');
                break;
        }
    }

    private static string json_escape(string value)
    {
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var ch in value)
            switch (ch)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }

        sb.Append('"');
        return sb.ToString();
    }

    #endregion
}