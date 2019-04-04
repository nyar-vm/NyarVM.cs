namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 词法单元类型
/// </summary>
public enum NotedownTokenKind
{
    /// <summary>空行</summary>
    blank_line,

    /// <summary>ATX 标题行（# ## ### 等）</summary>
    atx_header,

    /// <summary>Setext 标题下划线（=== 或 ---）</summary>
    setext_underline,

    /// <summary>代码围栏开始（``` 或 ~~~）</summary>
    code_fence_start,

    /// <summary>代码围栏结束（``` 或 ~~~）</summary>
    code_fence_end,

    /// <summary>引用块（&gt; ）</summary>
    block_quote,

    /// <summary>无序列表项（- * + ）</summary>
    unordered_list_item,

    /// <summary>有序列表项（1. 2. 等）</summary>
    ordered_list_item,

    /// <summary>任务列表项（- [ ] 或 - [x]）</summary>
    task_list_item,

    /// <summary>定义术语行</summary>
    definition_term,

    /// <summary>定义描述行（: ）</summary>
    definition_description,

    /// <summary>分割线（--- *** ___）</summary>
    horizontal_rule,

    /// <summary>表格行（| 分隔）</summary>
    table_row,

    /// <summary>Div 围栏开始（:::）</summary>
    div_fence_start,

    /// <summary>Div 围栏结束（:::）</summary>
    div_fence_end,

    /// <summary>YAML 前置元数据分隔（---）</summary>
    yaml_frontmatter_delimiter,

    /// <summary>行块行（| 开头）</summary>
    line_block_line,

    /// <summary>普通段落文本行</summary>
    paragraph_text,

    /// <summary>HTML 注释</summary>
    html_comment,

    /// <summary>脚注定义</summary>
    footnote_definition,

    /// <summary>文件结束</summary>
    end_of_file
}