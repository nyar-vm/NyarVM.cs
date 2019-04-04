using Std.Data.Text.Notedown.Syntax;

namespace Std.Data.Text.Notedown.Lexing;

/// <summary>
///     Notedown 词法分析器，逐行识别块级结构
/// </summary>
public sealed class NotedownLexer
{
    private readonly string[] _lines;
    private readonly string _source;
    private readonly List<NotedownToken> _tokens;
    private bool _in_yaml_frontmatter;
    private int _line_index;

    /// <summary>
    ///     创建词法分析器
    /// </summary>
    public NotedownLexer(string source)
    {
        _source = source;
        _lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        _line_index = 0;
        _tokens = [];
        _in_yaml_frontmatter = false;
    }

    /// <summary>
    ///     执行词法分析，返回词法单元序列
    /// </summary>
    public IReadOnlyList<NotedownToken> tokenize()
    {
        _tokens.Clear();
        _line_index = 0;

        while (_line_index < _lines.Length)
        {
            var line = _lines[_line_index];
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (trimmed.Length == 0)
            {
                add_token(NotedownTokenKind.blank_line, line);
                _line_index++;
                continue;
            }

            var token = classify_line(trimmed, indent);
            _tokens.Add(token);
            _line_index++;
        }

        _tokens.Add(NotedownToken.end_of_file);
        return _tokens;
    }

    private void add_token(NotedownTokenKind kind, string text, object? data = null, int indent = 0)
    {
        _tokens.Add(new NotedownToken(kind, text, _line_index + 1, indent, data));
    }

    private NotedownToken classify_line(string trimmed, int indent)
    {
        if (_line_index == 0 && trimmed == "---")
        {
            _in_yaml_frontmatter = true;
            return new NotedownToken(NotedownTokenKind.yaml_frontmatter_delimiter, trimmed, _line_index + 1, indent);
        }

        if (_in_yaml_frontmatter)
        {
            if (trimmed == "---")
            {
                _in_yaml_frontmatter = false;
                return new NotedownToken(NotedownTokenKind.yaml_frontmatter_delimiter, trimmed, _line_index + 1,
                    indent);
            }

            return new NotedownToken(NotedownTokenKind.paragraph_text, trimmed, _line_index + 1, indent);
        }

        if (is_code_fence_start(trimmed))
            return new NotedownToken(NotedownTokenKind.code_fence_start, trimmed, _line_index + 1, indent,
                extract_code_fence_info(trimmed));

        if (is_code_fence_end(trimmed))
            return new NotedownToken(NotedownTokenKind.code_fence_end, trimmed, _line_index + 1, indent);

        if (is_div_fence_start(trimmed))
            return new NotedownToken(NotedownTokenKind.div_fence_start, trimmed, _line_index + 1, indent,
                extract_div_fence_attr(trimmed));

        if (trimmed == ":::")
            return new NotedownToken(NotedownTokenKind.div_fence_end, trimmed, _line_index + 1, indent);

        if (is_horizontal_rule(trimmed))
        {
            if (is_setext_underline_candidate() && indent == 0)
                return new NotedownToken(NotedownTokenKind.setext_underline, trimmed, _line_index + 1, indent);

            return new NotedownToken(NotedownTokenKind.horizontal_rule, trimmed, _line_index + 1, indent);
        }

        if (is_atx_header(trimmed, out var level, out var headerRest))
            return new NotedownToken(NotedownTokenKind.atx_header, trimmed, _line_index + 1, indent,
                new AtxHeaderData(level, headerRest.Trim()));

        if (is_block_quote(trimmed, out var quoteContent))
            return new NotedownToken(NotedownTokenKind.block_quote, trimmed, _line_index + 1, indent, quoteContent);

        if (is_task_list_item(trimmed, out var taskContent, out var isChecked))
            return new NotedownToken(NotedownTokenKind.task_list_item, trimmed, _line_index + 1, indent,
                new TaskListItemData(taskContent, isChecked));

        if (is_unordered_list_item(trimmed, out var listContent))
            return new NotedownToken(NotedownTokenKind.unordered_list_item, trimmed, _line_index + 1, indent,
                listContent);

        if (is_ordered_list_item(trimmed, out var orderedContent, out var startNumber))
            return new NotedownToken(NotedownTokenKind.ordered_list_item, trimmed, _line_index + 1, indent,
                new OrderedListItemData(orderedContent, startNumber));

        if (is_definition_description(trimmed, out var defContent))
            return new NotedownToken(NotedownTokenKind.definition_description, trimmed, _line_index + 1, indent,
                defContent);

        if (is_line_block(trimmed, out var lineContent))
            return new NotedownToken(NotedownTokenKind.line_block_line, trimmed, _line_index + 1, indent, lineContent);

        if (is_table_row(trimmed))
            return new NotedownToken(NotedownTokenKind.table_row, trimmed, _line_index + 1, indent,
                extract_table_cells(trimmed));

        if (is_footnote_definition(trimmed, out var fnId, out var fnContent))
            return new NotedownToken(NotedownTokenKind.footnote_definition, trimmed, _line_index + 1, indent,
                new FootnoteData(fnId, fnContent));

        if (is_html_comment(trimmed))
            return new NotedownToken(NotedownTokenKind.html_comment, trimmed, _line_index + 1, indent);

        if (is_definition_term(trimmed) && is_next_line_definition_description())
            return new NotedownToken(NotedownTokenKind.definition_term, trimmed, _line_index + 1, indent);

        return new NotedownToken(NotedownTokenKind.paragraph_text, trimmed, _line_index + 1, indent);
    }

    #region 行分类检测

    private static bool is_atx_header(string line, out int level, out string rest)
    {
        level = 0;
        rest = string.Empty;

        var i = 0;
        while (i < line.Length && line[i] == '#') i++;

        if (i == 0 || i > 6) return false;

        if (i < line.Length && line[i] != ' ') return false;

        level = i;
        rest = line[i..];
        return true;
    }

    private bool is_setext_underline_candidate()
    {
        if (_line_index == 0) return false;

        var prevLine = _lines[_line_index - 1].Trim();
        return prevLine.Length > 0 && !is_horizontal_rule(prevLine) && !is_code_fence_start(prevLine);
    }

    private static bool is_horizontal_rule(string line)
    {
        var ch = line[0];
        if (ch is not ('-' and not '_') && ch is not ('*' and not ' '))
            if (ch != '-' && ch != '*' && ch != '_')
                return false;

        var count = 0;
        foreach (var c in line)
            if (c == ch)
                count++;
            else if (c != ' ') return false;

        return count >= 3;
    }

    private static bool is_block_quote(string line, out string content)
    {
        content = string.Empty;
        if (line.StartsWith('>'))
        {
            content = line[1..].TrimStart();
            return true;
        }

        return false;
    }

    private static bool is_unordered_list_item(string line, out string content)
    {
        content = string.Empty;
        if (line.Length < 2) return false;

        if (line[0] is '-' or '*' or '+' && line[1] == ' ')
        {
            content = line[2..];
            return true;
        }

        return false;
    }

    private static bool is_ordered_list_item(string line, out string content, out int startNumber)
    {
        content = string.Empty;
        startNumber = 0;

        var i = 0;
        while (i < line.Length && char.IsDigit(line[i])) i++;

        if (i == 0 || i >= line.Length - 1) return false;

        if (line[i] == '.' && (i + 1 >= line.Length || line[i + 1] == ' '))
        {
            startNumber = int.Parse(line[..i]);
            content = i + 1 < line.Length ? line[(i + 1)..].TrimStart() : string.Empty;
            return true;
        }

        return false;
    }

    private static bool is_task_list_item(string line, out string content, out bool isChecked)
    {
        content = string.Empty;
        isChecked = false;

        if (!is_unordered_list_item(line, out var rawContent)) return false;

        var trimmed = rawContent.TrimStart();
        if (trimmed.Length < 4) return false;

        if (trimmed.StartsWith("[ ] "))
        {
            content = trimmed[4..];
            isChecked = false;
            return true;
        }

        if (trimmed.StartsWith("[x] ") || trimmed.StartsWith("[X] "))
        {
            content = trimmed[4..];
            isChecked = true;
            return true;
        }

        return false;
    }

    private static bool is_code_fence_start(string line)
    {
        var trimmed = line;
        if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~")) return true;

        return false;
    }

    private static bool is_code_fence_end(string line)
    {
        var trimmed = line.Trim();
        return trimmed is "```" or "~~~";
    }

    private static bool is_definition_description(string line, out string content)
    {
        content = string.Empty;
        if (line.StartsWith(": ") || line == ":")
        {
            content = line.Length > 1 ? line[2..] : string.Empty;
            return true;
        }

        return false;
    }

    private bool is_definition_term(string line)
    {
        return line.Length > 0 && !line.StartsWith('#') && !line.StartsWith('>')
               && !line.StartsWith('-') && !line.StartsWith('*') && !line.StartsWith('+')
               && !line.StartsWith('|') && !line.StartsWith(':') && !line.StartsWith("```")
               && !line.StartsWith("~~~") && !line.StartsWith(":::");
    }

    private bool is_next_line_definition_description()
    {
        if (_line_index + 1 >= _lines.Length) return false;

        return is_definition_description(_lines[_line_index + 1].TrimStart(), out _);
    }

    private static bool is_line_block(string line, out string content)
    {
        content = string.Empty;
        if (line.StartsWith("| "))
        {
            content = line[2..];
            return true;
        }

        return false;
    }

    private static bool is_table_row(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|') && trimmed.EndsWith('|')) return true;

        return false;
    }

    private static bool is_div_fence_start(string line)
    {
        return line.StartsWith(":::") && line.Length > 3;
    }

    private static bool is_footnote_definition(string line, out string fnId, out string content)
    {
        fnId = string.Empty;
        content = string.Empty;

        if (!line.StartsWith("[^")) return false;

        var closeBracket = line.IndexOf(']');
        if (closeBracket <= 2) return false;

        fnId = line[2..closeBracket];
        if (closeBracket + 1 < line.Length && line[closeBracket + 1] == ':')
        {
            content = closeBracket + 2 < line.Length ? line[(closeBracket + 2)..].TrimStart() : string.Empty;
            return true;
        }

        return false;
    }

    private static bool is_html_comment(string line)
    {
        return line.TrimStart().StartsWith("<!--");
    }

    #endregion

    #region 数据提取

    private static string extract_code_fence_info(string line)
    {
        var fenceChar = line[0];
        var i = 0;
        while (i < line.Length && line[i] == fenceChar) i++;

        return i < line.Length ? line[i..].Trim() : string.Empty;
    }

    private static Attr extract_div_fence_attr(string line)
    {
        var attrStart = line.IndexOf('{');
        if (attrStart < 0) return Attr.empty;

        var attrEnd = line.IndexOf('}', attrStart);
        if (attrEnd < 0) return Attr.empty;

        return parse_attr(line[(attrStart + 1)..attrEnd]);
    }

    private static IReadOnlyList<string> extract_table_cells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];

        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];

        return [.. trimmed.Split('|').Select(c => c.Trim())];
    }

    /// <summary>
    ///     解析 Notedown 属性字符串，如 "#id .class key=value"
    /// </summary>
    public static Attr parse_attr(string attrStr)
    {
        var id = string.Empty;
        var classes = new List<string>();
        var keyValues = new List<KeyValuePair<string, string>>();

        var parts = attrStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
            if (part.StartsWith('#') && part.Length > 1)
            {
                id = part[1..];
            }
            else if (part.StartsWith('.') && part.Length > 1)
            {
                classes.Add(part[1..]);
            }
            else
            {
                var eqIndex = part.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = part[..eqIndex];
                    var value = part[(eqIndex + 1)..].Trim('"');
                    keyValues.Add(new KeyValuePair<string, string>(key, value));
                }
            }

        return new Attr(id, classes, keyValues);
    }

    #endregion
}