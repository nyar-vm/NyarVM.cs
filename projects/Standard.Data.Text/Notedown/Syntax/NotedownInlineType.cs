namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 行内节点类型，对齐 pandoc Inline 类型
/// </summary>
public enum NotedownInlineType
{
    /// <summary>纯文本</summary>
    str,

    /// <summary>斜体</summary>
    emph,

    /// <summary>加粗</summary>
    strong,

    /// <summary>删除线</summary>
    strikeout,

    /// <summary>上标</summary>
    superscript,

    /// <summary>下标</summary>
    subscript,

    /// <summary>小大写</summary>
    small_caps,

    /// <summary>引用（单/双引号）</summary>
    quoted,

    /// <summary>文献引用</summary>
    cite,

    /// <summary>行内代码</summary>
    code,

    /// <summary>空格</summary>
    space,

    /// <summary>软换行</summary>
    soft_break,

    /// <summary>硬换行</summary>
    line_break,

    /// <summary>数学公式</summary>
    math,

    /// <summary>原始行内格式</summary>
    raw_inline,

    /// <summary>链接</summary>
    link,

    /// <summary>图片</summary>
    image,

    /// <summary>脚注</summary>
    note,

    /// <summary>通用行内容器</summary>
    span
}