namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 块级节点类型，对齐 pandoc Block 类型
/// </summary>
public enum NotedownBlockType
{
    /// <summary>段落（顶级）</summary>
    para,

    /// <summary>纯文本块（非顶级）</summary>
    plain,

    /// <summary>行块（诗歌、地址等）</summary>
    line_block,

    /// <summary>代码块</summary>
    code_block,

    /// <summary>原始格式块</summary>
    raw_block,

    /// <summary>引用块</summary>
    block_quote,

    /// <summary>有序列表</summary>
    ordered_list,

    /// <summary>无序列表</summary>
    bullet_list,

    /// <summary>定义列表</summary>
    definition_list,

    /// <summary>标题</summary>
    header,

    /// <summary>分割线</summary>
    horizontal_rule,

    /// <summary>表格</summary>
    table,

    /// <summary>通用块容器</summary>
    div,

    /// <summary>空块</summary>
    @null
}