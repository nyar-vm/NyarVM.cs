namespace Std.Template.Site;

/// <summary>
///     传递给插件的页面元数据（只读视图，由 <see cref="PageData" /> 构建）
/// </summary>
public class PageMetadata
{
    /// <summary>
    ///     页面标题
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    ///     页面别名（用于 URL）
    /// </summary>
    public string Slug { get; init; } = "";

    /// <summary>
    ///     发布日期
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    ///     最后更新日期
    /// </summary>
    public DateTime? Updated { get; init; }

    /// <summary>
    ///     标签列表
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    ///     分类列表
    /// </summary>
    public List<string> Categories { get; init; } = [];

    /// <summary>
    ///     页面 URL
    /// </summary>
    public string Url { get; init; } = "";

    /// <summary>
    ///     页面摘要
    /// </summary>
    public string Summary { get; init; } = "";

    /// <summary>
    ///     是否为草稿
    /// </summary>
    public bool Draft { get; init; }

    /// <summary>
    ///     页面所在分区（如 posts、docs/api）
    /// </summary>
    public string Section { get; init; } = "";

    /// <summary>
    ///     页面排版名称
    /// </summary>
    public string Layout { get; init; } = "default";

    /// <summary>
    ///     页面所属语言代码
    /// </summary>
    public string Language { get; init; } = "";

    /// <summary>
    ///     扩展元数据（来自 Front Matter 的额外字段）
    /// </summary>
    public Dictionary<string, object> Extra { get; init; } = new();

    /// <summary>
    ///     从 <see cref="PageData" /> 构建元数据
    /// </summary>
    public static PageMetadata FromPageData(PageData page)
    {
        return new PageMetadata
        {
            Title = page.Title,
            Slug = page.Slug,
            Date = page.Date,
            Updated = page.Updated,
            Tags = [.. page.Tags],
            Categories = [.. page.Categories],
            Url = page.Url,
            Summary = page.Summary,
            Draft = page.Draft,
            Section = page.Section,
            Layout = page.Layout,
            Language = page.Language,
            Extra = new Dictionary<string, object>(page.Extra)
        };
    }
}

/// <summary>
///     Markdown 转换钩子：在 Markdown 解析后、渲染前调用
/// </summary>
public interface IMarkdownTransform
{
    /// <summary>
    ///     插件名称，用于日志和诊断
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     转换 Markdown 内容
    /// </summary>
    /// <param name="markdownContent">原始 Markdown 文本</param>
    /// <param name="metadata">页面元数据</param>
    /// <returns>转换后的 Markdown 文本</returns>
    Task<string> TransformAsync(string markdownContent, PageMetadata metadata);
}

/// <summary>
///     HTML 后处理钩子：在 HTML 渲染完成后调用
/// </summary>
public interface IHtmlPostProcess
{
    /// <summary>
    ///     插件名称，用于日志和诊断
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     后处理 HTML 内容
    /// </summary>
    /// <param name="htmlContent">渲染后的 HTML</param>
    /// <param name="metadata">页面元数据</param>
    /// <returns>处理后的 HTML</returns>
    Task<string> ProcessAsync(string htmlContent, PageMetadata metadata);
}

/// <summary>
///     站点钩子：在站点构建生命周期的关键节点调用
/// </summary>
public interface ISiteHook
{
    /// <summary>
    ///     插件名称，用于日志和诊断
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     在站点构建开始前调用
    /// </summary>
    /// <param name="config">站点配置</param>
    Task OnBeforeBuildAsync(SiteConfig config);

    /// <summary>
    ///     在站点构建完成后调用
    /// </summary>
    /// <param name="outputDir">输出目录路径</param>
    Task OnAfterBuildAsync(string outputDir);
}