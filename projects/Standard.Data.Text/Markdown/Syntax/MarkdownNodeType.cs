namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     Markdown 节点类型
/// </summary>
public enum MarkdownNodeType
{
    document,
    heading,
    paragraph,
    code_block,
    inline_code,
    blockquote,
    list,
    list_item,
    task_list_item,
    horizontal_rule,
    link,
    image,
    emphasis,
    strong,
    strikethrough,
    highlight,
    line_break,
    soft_break,
    text,
    table,
    table_row,
    table_cell,
    html_block,
    html_inline,
    footnote,
    footnote_definition,
    math_inline,
    math_block,
    reference_link_definition,
    indented_code_block
}