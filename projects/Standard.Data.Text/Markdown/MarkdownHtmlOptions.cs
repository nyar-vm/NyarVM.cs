namespace Std.Data.Text.Markdown;

/// <summary>
///     Markdown HTML 渲染选项
/// </summary>
public sealed class MarkdownHtmlOptions
{
    /// <summary>
    ///     是否为标题生成 ID 锚点
    /// </summary>
    public bool generate_heading_ids { get; init; } = true;


    /// <summary>
    ///     是否为代码块生成语法高亮 class
    /// </summary>
    public bool highlight_code { get; init; } = true;


    /// <summary>
    ///     代码块 class 前缀
    /// </summary>
    public string code_block_class_prefix { get; init; } = "language-";


    /// <summary>
    ///     是否在新标签页打开外部链接
    /// </summary>
    public bool external_link_new_tab { get; init; } = true;


    /// <summary>
    ///     外部链接判断前缀列表
    /// </summary>
    public IReadOnlyList<string> external_link_prefixes { get; init; } = new List<string> { "http://", "https://" };


    /// <summary>
    ///     是否生成 TOC 数据
    /// </summary>
    public bool generate_toc { get; init; } = true;


    /// <summary>
    ///     数学公式渲染方式（默认 KaTeX）
    /// </summary>
    public MathRenderMode math_mode { get; init; } = MathRenderMode.ka_te_x;


    /// <summary>
    ///     是否将软换行渲染为 &lt;br&gt;
    /// </summary>
    public bool soft_break_as_line_break { get; init; } = false;
}