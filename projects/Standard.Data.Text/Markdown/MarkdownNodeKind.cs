using Std.Data.Text.Syntax;

namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 词法节点类型
/// </summary>
public static class MarkdownNodeKind
{
    public static readonly NodeKind unknown = 0;
    public static readonly NodeKind text = 1;
    public static readonly NodeKind new_line = 2;
    public static readonly NodeKind whitespace = 3;
    public static readonly NodeKind heading_marker = 4;
    public static readonly NodeKind strong_marker = 5;
    public static readonly NodeKind emphasis_marker = 6;
    public static readonly NodeKind strikethrough_marker = 7;
    public static readonly NodeKind highlight_marker = 8;
    public static readonly NodeKind inline_code_marker = 9;
    public static readonly NodeKind code_block_marker = 10;
    public static readonly NodeKind code_language = 11;
    public static readonly NodeKind code_content = 12;
    public static readonly NodeKind blockquote_marker = 13;
    public static readonly NodeKind unordered_list_marker = 14;
    public static readonly NodeKind ordered_list_marker = 15;
    public static readonly NodeKind task_list_marker = 16;
    public static readonly NodeKind link_open = 17;
    public static readonly NodeKind link_close = 18;
    public static readonly NodeKind url_open = 19;
    public static readonly NodeKind url_close = 20;
    public static readonly NodeKind image_marker = 21;
    public static readonly NodeKind horizontal_rule = 22;
    public static readonly NodeKind table_delimiter = 23;
    public static readonly NodeKind table_align = 24;
    public static readonly NodeKind escape = 25;
    public static readonly NodeKind auto_link = 26;
    public static readonly NodeKind html_tag = 27;
    public static readonly NodeKind footnote_marker = 28;
    public static readonly NodeKind math_marker = 29;
    public static readonly NodeKind setext_heading_marker = 30;
    public static readonly NodeKind indented_code_marker = 31;
    public static readonly NodeKind colon = 32;
    public static readonly NodeKind eof = 33;
}