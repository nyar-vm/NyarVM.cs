namespace Std.Template;

/// <summary>
///     Markdown → HTML 渲染器，内部委托给 Oak.Markdown.MarkdownHtmlRenderer
/// </summary>
public sealed class MarkdownRenderer
{
    private readonly MarkdownRenderOptions _options;

    /// <summary>
    ///     创建 Markdown 渲染器
    /// </summary>
    public MarkdownRenderer(MarkdownRenderOptions? options = null)
    {
        _options = options ?? MarkdownRenderOptions.Default;
    }

    /// <summary>
    ///     获取或设置 KaTeX 服务端渲染服务
    /// </summary>
    public KaTeXService? KaTeXService { get; set; }

    /// <summary>
    ///     渲染 Markdown 文本为 HTML
    /// </summary>
    public MarkdownRenderResult Render(string markdown)
    {
        return RenderCore(markdown);
    }

    /// <summary>
    ///     渲染 Markdown 文本为 HTML（保留 startingHeadingLevel 兼容性，委托 Oak 处理）
    /// </summary>
    public MarkdownRenderResult Render(string markdown, int startingHeadingLevel)
    {
        return RenderCore(markdown);
    }

    /// <summary>
    ///     渲染已解析的 Markdown 文档
    /// </summary>
    public MarkdownRenderResult Render(MarkdownDocument doc)
    {
        return RenderCore(doc);
    }

    /// <summary>
    ///     渲染已解析的 Markdown 文档（保留 startingHeadingLevel 兼容性）
    /// </summary>
    public MarkdownRenderResult Render(MarkdownDocument doc, int startingHeadingLevel)
    {
        return RenderCore(doc);
    }

    /// <summary>
    ///     渲染已解析的 Markdown 文档（保留 startingHeadingLevel 兼容性）
    /// </summary>
    public MarkdownRenderResult RenderDocument(MarkdownDocument doc, int startingHeadingLevel)
    {
        return RenderCore(doc);
    }

    private MarkdownHtmlOptions BuildHtmlOptions()
    {
        return new MarkdownHtmlOptions
        {
            GenerateHeadingIds = _options.GenerateHeadingIds,
            HighlightCode = _options.HighlightCode,
            CodeBlockClassPrefix = _options.CodeBlockClassPrefix,
            ExternalLinkNewTab = _options.ExternalLinkNewTab,
            GenerateToc = _options.GenerateToc,
            MathMode = _options.MathMode,
            SoftBreakAsLineBreak = _options.SoftBreakAsLineBreak
        };
    }

    private MarkdownRenderResult RenderCore(string markdown)
    {
        var htmlRenderer = new MarkdownHtmlRenderer(BuildHtmlOptions());
        if (KaTeXService != null) htmlRenderer.KaTeXService = KaTeXService;

        var result = htmlRenderer.Render(markdown);
        return ConvertResult(result);
    }

    private MarkdownRenderResult RenderCore(MarkdownDocument doc)
    {
        var htmlRenderer = new MarkdownHtmlRenderer(BuildHtmlOptions());
        if (KaTeXService != null) htmlRenderer.KaTeXService = KaTeXService;

        var result = htmlRenderer.Render(doc);
        return ConvertResult(result);
    }

    private static MarkdownRenderResult ConvertResult(MarkdownHtmlResult result)
    {
        return new MarkdownRenderResult
        {
            Html = result.Html,
            TocItems = result.TocItems.ToList(),
            FootnoteItems = result.Footnotes.Select(f => new FootnoteItem
            {
                Label = f.Label,
                Content = string.Empty
            }).ToList()
        };
    }
}

/// <summary>
///     Markdown 渲染选项
/// </summary>
public sealed class MarkdownRenderOptions
{
    /// <summary>
    ///     是否为标题生成 ID 锚点
    /// </summary>
    public bool GenerateHeadingIds { get; set; }

    /// <summary>
    ///     是否为代码块生成语法高亮 class
    /// </summary>
    public bool HighlightCode { get; set; }

    /// <summary>
    ///     代码块 class 前缀
    /// </summary>
    public string CodeBlockClassPrefix { get; set; } = "language-";

    /// <summary>
    ///     是否在新标签页打开外部链接
    /// </summary>
    public bool ExternalLinkNewTab { get; set; }

    /// <summary>
    ///     是否生成 TOC 数据
    /// </summary>
    public bool GenerateToc { get; set; }

    /// <summary>
    ///     数学公式渲染方式
    /// </summary>
    public MathRenderMode MathMode { get; set; } = MathRenderMode.KaTeX;

    /// <summary>
    ///     是否将软换行渲染为 br
    /// </summary>
    public bool SoftBreakAsLineBreak { get; set; }

    /// <summary>
    ///     是否启用缩进代码块
    /// </summary>
    public bool EnableIndentedCodeBlocks { get; set; } = true;

    /// <summary>
    ///     默认选项
    /// </summary>
    public static MarkdownRenderOptions Default => new();
}

/// <summary>
///     脚注项
/// </summary>
public sealed class FootnoteItem
{
    /// <summary>
    ///     脚注标识
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    ///     脚注 HTML 内容（渲染后的 HTML 已包含在 Html 输出中）
    /// </summary>
    public string Content { get; set; } = "";
}

/// <summary>
///     Markdown 渲染结果
/// </summary>
public sealed class MarkdownRenderResult
{
    /// <summary>
    ///     渲染后的 HTML
    /// </summary>
    public string Html { get; set; } = "";

    /// <summary>
    ///     目录项列表
    /// </summary>
    public List<TocItem> TocItems { get; set; } = new();

    /// <summary>
    ///     脚注项列表
    /// </summary>
    public List<FootnoteItem> FootnoteItems { get; set; } = new();
}