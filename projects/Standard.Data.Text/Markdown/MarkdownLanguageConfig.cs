namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown 语言配置
/// </summary>
public sealed class MarkdownLanguageConfig
{
    /// <summary>
    ///     是否启用表格扩展
    /// </summary>
    public bool enable_tables { get; init; } = true;

    /// <summary>
    ///     是否启用任务列表扩展
    /// </summary>
    public bool enable_task_lists { get; init; } = true;

    /// <summary>
    ///     是否启用删除线扩展
    /// </summary>
    public bool enable_strikethrough { get; init; } = true;

    /// <summary>
    ///     是否启用自动链接扩展
    /// </summary>
    public bool enable_auto_links { get; init; } = true;

    /// <summary>
    ///     是否启用高亮/标记扩展 ==text==
    /// </summary>
    public bool enable_highlight { get; init; } = true;

    /// <summary>
    ///     是否启用脚注扩展 [^1] / [^1]: text
    /// </summary>
    public bool enable_footnotes { get; init; } = true;

    /// <summary>
    ///     是否启用数学公式扩展 $...$ / $$...$$
    /// </summary>
    public bool enable_math { get; init; } = true;

    /// <summary>
    ///     是否启用 HTML 内联标签
    /// </summary>
    public bool enable_html_inline { get; init; } = true;

    /// <summary>
    ///     是否启用 HTML 块标签
    /// </summary>
    public bool enable_html_blocks { get; init; } = true;

    /// <summary>
    ///     是否启用 Setext 风格标题（下划线 === / ---）
    /// </summary>
    public bool enable_setext_headings { get; init; } = true;

    /// <summary>
    ///     是否启用缩进代码块（4 空格缩进）
    /// </summary>
    public bool enable_indented_code_blocks { get; init; } = true;

    /// <summary>
    ///     是否启用引用式链接 [text][id] / [id]: url
    /// </summary>
    public bool enable_reference_links { get; init; } = true;

    /// <summary>
    ///     是否将段落内单个换行符渲染为软换行（&lt;br&gt;）
    /// </summary>
    public bool soft_break_as_line_break { get; init; }

    /// <summary>
    ///     默认配置实例
    /// </summary>
    public static MarkdownLanguageConfig @default { get; } = new();

    /// <summary>
    ///     严格 CommonMark 配置（不启用任何扩展）
    /// </summary>
    public static MarkdownLanguageConfig common_mark { get; } = new()
    {
        enable_tables = false,
        enable_task_lists = false,
        enable_strikethrough = false,
        enable_auto_links = false,
        enable_highlight = false,
        enable_footnotes = false,
        enable_math = false,
        enable_html_inline = false,
        enable_html_blocks = false,
        enable_setext_headings = true,
        enable_indented_code_blocks = true,
        enable_reference_links = true,
        soft_break_as_line_break = false
    };

    /// <summary>
    ///     GFM（GitHub Flavored Markdown）配置
    /// </summary>
    public static MarkdownLanguageConfig gfm { get; } = new()
    {
        enable_tables = true,
        enable_task_lists = true,
        enable_strikethrough = true,
        enable_auto_links = true,
        enable_highlight = false,
        enable_footnotes = false,
        enable_math = false,
        enable_html_inline = true,
        enable_html_blocks = true,
        enable_setext_headings = true,
        enable_indented_code_blocks = true,
        enable_reference_links = true,
        soft_break_as_line_break = false
    };
}