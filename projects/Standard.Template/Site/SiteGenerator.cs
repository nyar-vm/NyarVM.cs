using System.Text;
using System.Text.RegularExpressions;
using Std.Template.Dejavu;

namespace Std.Template.Site;

#region 数据模型

/// <summary>
///     页面数据
/// </summary>
public class PageData
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string RawContent { get; set; } = "";
    public string Layout { get; set; } = "default";
    public DateTime Date { get; set; } = DateTime.Now;
    public DateTime? Updated { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string Url { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool Draft { get; set; }
    public int Weight { get; set; }
    public string Section { get; set; } = "";
    public string Permalink { get; set; } = "";
    public List<TocItem> TocItems { get; set; } = [];
    public PageData? Previous { get; set; }
    public PageData? Next { get; set; }
    public Dictionary<string, object> Extra { get; set; } = new();
    public string Version { get; set; } = "";
    public List<string> Modules { get; set; } = [];
    public string CurrentModule { get; set; } = "";
    public string Chapter { get; set; } = "";

    /// <summary>
    ///     页面所属语言代码
    /// </summary>
    public string Language { get; set; } = "";
}

/// <summary>
///     站点配置
/// </summary>
public class NavItem
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public List<NavItem> Children { get; set; } = [];
}

public class SiteConfig
{
    public string Title { get; set; } = "My Site";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string Language { get; set; } = "zh-CN";
    public string BaseUrl { get; set; } = "/";
    public string DefaultLayout { get; set; } = "default";
    public string DefaultExtension { get; set; } = ".html";
    public int Paginate { get; set; } = 10;
    public string PermalinkPattern { get; set; } = "/:section/:slug/";
    public bool IncludeDrafts { get; set; }
    public List<NavItem> Navigation { get; set; } = [];
    public List<NavItem> Sidebar { get; set; } = [];
    public Dictionary<string, string> Translations { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Languages { get; set; } = new();
    public Dictionary<string, object> Extra { get; set; } = new();

    public Dictionary<string, string> Taxonomies { get; set; } = new()
    {
        ["tags"] = "tags",
        ["categories"] = "categories"
    };

    /// <summary>
    ///     插件程序集路径列表（支持绝对路径或相对于源目录的路径）
    /// </summary>
    public List<string> Plugins { get; set; } = [];
}

/// <summary>
///     分页数据
/// </summary>
public class PaginatedList
{
    public List<PageData> Items { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public string PreviousUrl => CurrentPage > 1 ? CurrentPage == 2 ? "/" : $"/posts/page/{CurrentPage - 1}/" : "";
    public string NextUrl => HasNext ? $"/posts/page/{CurrentPage + 1}/" : "";
}

#endregion

/// <summary>
///     DejaVu 站点生成器
/// </summary>
public sealed class SiteGenerator
{
    private readonly Dictionary<string, object> _data = new();
    private readonly MarkdownLanguage _markdownLanguage;
    private readonly MarkdownHtmlRenderer _markdownRenderer;
    private readonly bool _optimize;
    private readonly DejaVuRenderer _renderer;
    private readonly Dictionary<string, Dictionary<string, List<PageData>>> _taxonomies = new();
    private readonly TemplateManager _templateManager;
    private List<PageData> _apiDocs = [];
    private List<Dictionary<string, object>> _cachedApiDocsForContext = [];
    private List<Dictionary<string, object>> _cachedPostsForContext = [];
    private Dictionary<string, List<PageData>> _categories = new();
    private SiteConfig _config = new();
    private List<PageData> _pages = [];
    private PluginManager _pluginManager = new();
    private List<PageData> _posts = [];
    private string _sourceDir;
    private Dictionary<string, List<PageData>> _tags = new();

    public SiteGenerator(string sourceDir)
        : this(sourceDir, false)
    {
    }

    /// <summary>
    ///     构造站点生成器
    /// </summary>
    /// <param name="sourceDir">源目录路径</param>
    /// <param name="optimize">是否启用静态资源优化</param>
    public SiteGenerator(string sourceDir, bool optimize)
    {
        _sourceDir = Path.GetFullPath(sourceDir);
        _optimize = optimize;
        var loader = new FileSystemTemplateLoader(_sourceDir);
        _templateManager = new TemplateManager(loader);
        _renderer = new DejaVuRenderer(DejaVuLanguage.dora, _templateManager);
        _markdownLanguage = new MarkdownLanguage();
        _markdownRenderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            generate_heading_ids = true,
            highlight_code = true,
            external_link_new_tab = true,
            generate_toc = true,
            math_mode = MathRenderMode.ka_te_x
        });
    }

    /// <summary>
    ///     生成站点
    /// </summary>
    public void Generate(string sourceDir, string outputDir)
    {
        _sourceDir = Path.GetFullPath(sourceDir);
        outputDir = Path.GetFullPath(outputDir);

        Directory.CreateDirectory(outputDir);

        LoadConfig();
        LoadPlugins();
        _pluginManager.RunBeforeBuildAsync(_config).GetAwaiter().GetResult();

        LoadData();
        ScanContent();
        BuildNavigation();
        ProcessContentPages(outputDir);
        GenerateIndexPage(outputDir);
        GeneratePaginatedPages(outputDir);
        GenerateTaxonomyPages(outputDir);
        GenerateRss(outputDir);
        GenerateSitemap(outputDir);
        GenerateSearchIndex(outputDir);
        GenerateSearchPage(outputDir);
        CopyStaticFiles(outputDir);

        _pluginManager.RunAfterBuildAsync(outputDir).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     加载插件：从配置中的路径和约定的 plugins 目录加载
    /// </summary>
    private void LoadPlugins()
    {
        _pluginManager = new PluginManager();

        if (_config.Plugins.Count > 0) _pluginManager.LoadFromConfig(_config.Plugins, _sourceDir);

        var conventionDir = Path.Combine(_sourceDir, "plugins");
        _pluginManager.LoadFromDirectory(conventionDir);
    }

    #region 导航构建

    private void BuildNavigation()
    {
        var sectionGroups = _pages.GroupBy(p => p.Section);

        foreach (var group in sectionGroups)
        {
            var sorted = group.OrderBy(p => p.Date).ThenBy(p => p.Weight).ToList();

            for (var i = 0; i < sorted.Count; i++)
            {
                sorted[i].Previous = i > 0 ? sorted[i - 1] : null;
                sorted[i].Next = i < sorted.Count - 1 ? sorted[i + 1] : null;
            }
        }
    }

    #endregion

    #region 首页

    private void GenerateIndexPage(string outputDir)
    {
        var indexPage = new PageData
        {
            Title = _config.Title,
            Layout = "index",
            Url = "/"
        };

        var context = BuildContext(indexPage);
        context["paginated"] = CreatePaginatedList(_pages, 1);

        var rendered = RenderLayoutWithFallback("index", context);

        File.WriteAllText(Path.Combine(outputDir, "index.html"), rendered);
    }

    #endregion

    #region RSS

    private void GenerateRss(string outputDir)
    {
        var baseUrl = _config.BaseUrl.TrimEnd('/');
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
        sb.AppendLine("  <channel>");
        sb.AppendLine($"    <title>{HtmlUtility.EscapeXml(_config.Title)}</title>");
        sb.AppendLine($"    <description>{HtmlUtility.EscapeXml(_config.Description)}</description>");
        sb.AppendLine($"    <link>{baseUrl}</link>");
        sb.AppendLine($"    <atom:link href=\"{baseUrl}/rss.xml\" rel=\"self\" type=\"application/rss+xml\" />");
        sb.AppendLine($"    <language>{_config.Language}</language>");
        sb.AppendLine($"    <lastBuildDate>{DateTime.UtcNow:R}</lastBuildDate>");

        foreach (var page in _pages.Take(20))
        {
            sb.AppendLine("    <item>");
            sb.AppendLine($"      <title>{HtmlUtility.EscapeXml(page.Title)}</title>");
            sb.AppendLine($"      <link>{baseUrl}{page.Url}</link>");
            sb.AppendLine($"      <guid isPermaLink=\"true\">{baseUrl}{page.Url}</guid>");
            sb.AppendLine($"      <pubDate>{page.Date:R}</pubDate>");
            sb.AppendLine($"      <description>{HtmlUtility.EscapeXml(page.Summary)}</description>");

            foreach (var tag in page.Tags) sb.AppendLine($"      <category>{HtmlUtility.EscapeXml(tag)}</category>");

            sb.AppendLine("    </item>");
        }

        sb.AppendLine("  </channel>");
        sb.AppendLine("</rss>");

        File.WriteAllText(Path.Combine(outputDir, "rss.xml"), sb.ToString());
    }

    #endregion

    #region Sitemap

    private void GenerateSitemap(string outputDir)
    {
        var baseUrl = _config.BaseUrl.TrimEnd('/');
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{baseUrl}/</loc>");
        sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
        sb.AppendLine("    <changefreq>daily</changefreq>");
        sb.AppendLine("    <priority>1.0</priority>");
        sb.AppendLine("  </url>");

        foreach (var page in _pages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{page.Url}</loc>");
            sb.AppendLine($"    <lastmod>{page.Updated ?? page.Date:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.8</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        File.WriteAllText(Path.Combine(outputDir, "sitemap.xml"), sb.ToString());
    }

    #endregion

    #region 配置与数据

    private void LoadConfig()
    {
        var configPath = Path.Combine(_sourceDir, "config.von");
        if (File.Exists(configPath))
        {
            var content = File.ReadAllText(configPath);
            _config = ParseVonConfig(content);
        }
    }

    private SiteConfig ParseVonConfig(string content)
    {
        var parser = new VonParser(new DiagnosticSink());
        var serdeValue = parser.Deserialize(content);
        var config = new SiteConfig();

        if (serdeValue.type != SerdeValueType.@object || serdeValue.fields == null)
            return config;

        foreach (var (key, value) in serdeValue.fields)
            switch (key)
            {
                case "title":
                    config.Title = value.get_string() ?? config.Title;
                    break;
                case "description":
                    config.Description = value.get_string() ?? config.Description;
                    break;
                case "author":
                    config.Author = value.get_string() ?? config.Author;
                    break;
                case "language":
                    config.Language = value.get_string() ?? config.Language;
                    break;
                case "baseUrl":
                    config.BaseUrl = (value.get_string() ?? config.BaseUrl).TrimEnd('/') + "/";
                    break;
                case "defaultLayout":
                    config.DefaultLayout = value.get_string() ?? config.DefaultLayout;
                    break;
                case "paginate":
                    if (value.type == SerdeValueType.integer && value.get_integer_string() != null)
                        config.Paginate = int.Parse(value.get_integer_string()!);
                    break;
                case "permalink":
                    config.PermalinkPattern = value.get_string() ?? config.PermalinkPattern;
                    break;
                case "includeDrafts":
                    if (value.type == SerdeValueType.boolean)
                        config.IncludeDrafts = value.get_boolean();
                    break;
                case "navigation":
                case "nav":
                    ParseNavItems(value, config.Navigation);
                    break;
                case "sidebar":
                    ParseNavItems(value, config.Sidebar);
                    break;
                case "languages":
                    ParseLanguages(value, config);
                    break;
                case "taxonomies":
                    ParseTaxonomies(value, config);
                    break;
                case "plugins":
                    if (value.type == SerdeValueType.array && value.elements != null)
                        config.Plugins = value.elements
                            .Select(e => e.get_string() ?? "")
                            .Where(p => !string.IsNullOrEmpty(p))
                            .ToList();
                    break;
                default:
                    config.Extra[key] = ConvertVonValueToObject(value);
                    break;
            }

        return config;
    }

    private static object ConvertVonValueToObject(SerdeValue value)
    {
        return value.type switch
        {
            SerdeValueType.@null => "",
            SerdeValueType.boolean => value.get_boolean(),
            SerdeValueType.integer => value.get_integer_string() ?? "0",
            SerdeValueType.@decimal => value.get_decimal_string() ?? "0",
            SerdeValueType.@string => value.get_string() ?? "",
            SerdeValueType.array => value.elements?.Select(ConvertVonValueToObject).ToList() ?? new List<object>(),
            SerdeValueType.@object => value.fields?.ToDictionary(kvp => kvp.Key,
                kvp => ConvertVonValueToObject(kvp.Value)) ?? new Dictionary<string, object>(),
            _ => value.get_string() ?? ""
        };
    }

    private void ParseTaxonomies(SerdeValue value, SiteConfig config)
    {
        if (value.type != SerdeValueType.@object || value.fields == null) return;

        config.Taxonomies.Clear();
        foreach (var (key, labelValue) in value.fields) config.Taxonomies[key] = labelValue.get_string() ?? key;
    }

    private void ParseNavItems(SerdeValue value, List<NavItem> target)
    {
        if (value.type != SerdeValueType.array || value.elements == null) return;

        foreach (var item in value.elements)
            if (item.type == SerdeValueType.@object && item.fields != null)
                foreach (var (itemKey, itemValue) in item.fields)
                {
                    target.Add(new NavItem { Label = itemKey, Url = itemValue.get_string() ?? "" });
                    break;
                }
    }

    private void ParseLanguages(SerdeValue value, SiteConfig config)
    {
        if (value.type != SerdeValueType.@object || value.fields == null) return;

        foreach (var (langCode, langValue) in value.fields)
            if (langValue.type == SerdeValueType.@object && langValue.fields != null)
            {
                var translations = new Dictionary<string, string>();
                foreach (var (transKey, transValue) in langValue.fields)
                    translations[transKey] = transValue.get_string() ?? "";
                config.Languages[langCode] = translations;
            }
    }

    private void LoadData()
    {
        var dataDir = Path.Combine(_sourceDir, "data");
        if (!Directory.Exists(dataDir)) return;

        foreach (var file in Directory.GetFiles(dataDir, "*.*", SearchOption.AllDirectories))
        {
            var key = Path.GetRelativePath(dataDir, file);
            key = Path.ChangeExtension(key, null).Replace("\\", "/");
            var content = File.ReadAllText(file);

            if (file.EndsWith(".yaml") || file.EndsWith(".yml"))
                _data[key] = ParseYamlData(content);
            else if (file.EndsWith(".json")) _data[key] = content;
        }
    }

    private Dictionary<string, object> ParseYamlData(string content)
    {
        var data = DataConvert.YamlToDictionary(content);
        return data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "");
    }

    #endregion

    #region 内容扫描

    private void ScanContent()
    {
        _pages.Clear();
        _tags.Clear();
        _categories.Clear();
        _taxonomies.Clear();
        _posts.Clear();
        _apiDocs.Clear();
        _cachedPostsForContext.Clear();
        _cachedApiDocsForContext.Clear();

        var contentDir = Path.Combine(_sourceDir, "content");
        if (!Directory.Exists(contentDir)) return;

        var files = Directory.GetFiles(contentDir, "*.md", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(contentDir, "*.dora", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(contentDir, file);
            var extension = Path.GetExtension(file).ToLowerInvariant();
            var page = ParseContentFile(file, relativePath, extension);

            if (page.Draft && !_config.IncludeDrafts) continue;

            _pages.Add(page);
        }

        _pages = [.. _pages.OrderByDescending(p => p.Date).ThenBy(p => p.Weight)];

        foreach (var (taxoKey, taxoLabel) in _config.Taxonomies)
        {
            var termDict = new Dictionary<string, List<PageData>>();
            foreach (var page in _pages)
            {
                var terms = GetTaxonomyTerms(page, taxoKey);
                foreach (var term in terms)
                {
                    if (!termDict.ContainsKey(term)) termDict[term] = [];

                    termDict[term].Add(page);
                }
            }

            _taxonomies[taxoKey] = termDict;

            if (taxoKey == "tags")
                _tags = termDict;
            else if (taxoKey == "categories") _categories = termDict;
        }

        if (_config.Languages.Count > 1)
            BuildPageLanguageVariants();
        else
            foreach (var page in _pages)
                page.Language = _config.Language;

        _posts = [.. _pages.Where(p => p.Section == "posts").OrderByDescending(p => p.Date)];
        _apiDocs = [.. _pages.Where(p => p.Section == "docs/api").OrderBy(p => p.Weight).ThenBy(p => p.Title)];

        _cachedPostsForContext =
        [
            .. _pages.Select(p => new Dictionary<string, object>
            {
                ["title"] = p.Title,
                ["summary"] = p.Summary,
                ["date"] = p.Date.ToString("yyyy-MM-dd"),
                ["url"] = p.Url,
                ["tags"] = p.Tags,
                ["categories"] = p.Categories,
                ["section"] = p.Section
            })
        ];

        _cachedApiDocsForContext =
        [
            .. _apiDocs.Select(p => new Dictionary<string, object>
            {
                ["title"] = p.Title,
                ["url"] = p.Url,
                ["module"] = p.CurrentModule
            })
        ];
    }

    /// <summary>
    ///     为多语言站点生成各语言版本的页面变体
    /// </summary>
    private void BuildPageLanguageVariants()
    {
        var defaultLang = _config.Language;
        var originalPages = _pages.ToList();

        foreach (var page in originalPages) page.Language = defaultLang;

        foreach (var (langCode, _) in _config.Languages)
        {
            if (langCode == defaultLang) continue;

            foreach (var page in originalPages)
            {
                var variant = new PageData
                {
                    Title = page.Title,
                    Content = page.Content,
                    RawContent = page.RawContent,
                    Layout = page.Layout,
                    Date = page.Date,
                    Updated = page.Updated,
                    Tags = [.. page.Tags],
                    Categories = [.. page.Categories],
                    Url = $"/{langCode}{page.Url}",
                    Summary = page.Summary,
                    Slug = page.Slug,
                    Draft = page.Draft,
                    Weight = page.Weight,
                    Section = page.Section,
                    Permalink = page.Permalink,
                    TocItems = [.. page.TocItems],
                    Extra = new Dictionary<string, object>(page.Extra),
                    Version = page.Version,
                    Modules = [.. page.Modules],
                    CurrentModule = page.CurrentModule,
                    Chapter = page.Chapter,
                    Language = langCode
                };
                _pages.Add(variant);
            }
        }

        _pages = [.. _pages.OrderByDescending(p => p.Date).ThenBy(p => p.Weight)];
    }

    private static List<string> GetTaxonomyTerms(PageData page, string taxoKey)
    {
        if (taxoKey == "tags") return page.Tags;

        if (taxoKey == "categories") return page.Categories;

        if (page.Extra.TryGetValue(taxoKey, out var value) && value is List<object> list)
            return [.. list.OfType<string>()];

        if (page.Extra.TryGetValue(taxoKey, out var strValue) && strValue is string s)
            return [.. s.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t))];

        return [];
    }

    private PageData ParseContentFile(string filePath, string relativePath, string extension)
    {
        var content = File.ReadAllText(filePath);
        var page = new PageData();

        var (frontMatter, body) = FrontMatterParser.Extract(content);
        if (frontMatter != null)
        {
            var mapping = FrontMatterParser.Parse(frontMatter);
            if (mapping != null) ParseFrontMatterFromMapping(mapping, page);
        }
        else
        {
            body = content;
        }

        page.RawContent = body;
        page.Section = GetSection(relativePath);

        if (extension == ".dora")
        {
            page.Content = body;
        }
        else
        {
            body = _pluginManager.RunMarkdownTransformsAsync(body, PageMetadata.FromPageData(page)).GetAwaiter()
                .GetResult();
            page.RawContent = body;
            var markdownDocument = _markdownLanguage.parse(body);
            var mdResult = _markdownRenderer.render(markdownDocument);
            page.Content = mdResult.html;
            page.TocItems = mdResult.toc_items.ToList();
        }

        page.Slug = GenerateSlug(relativePath);
        page.Url = GeneratePermalink(page);

        if (string.IsNullOrEmpty(page.Summary))
            page.Summary = GenerateSummary(extension == ".dora" ? body : page.Content);

        if (extension != ".dora" && _pluginManager.HtmlPostProcessors.Count > 0)
            page.Content = _pluginManager.RunHtmlPostProcessAsync(page.Content, PageMetadata.FromPageData(page))
                .GetAwaiter().GetResult();

        return page;
    }

    private void ParseFrontMatterFromMapping(YamlMapping mapping, PageData page)
    {
        page.Title = DataConvert.GetYamlString(mapping, "title", page.Title);
        page.Layout = DataConvert.GetYamlString(mapping, "layout", page.Layout);
        page.Summary = DataConvert.GetYamlString(mapping, "summary", page.Summary);
        page.Slug = DataConvert.GetYamlString(mapping, "slug", page.Slug);
        page.Draft = DataConvert.GetYamlBool(mapping, "draft");
        page.Weight = DataConvert.GetYamlInt(mapping, "weight");
        page.Version = DataConvert.GetYamlString(mapping, "version").Trim('"', '\'');
        page.CurrentModule = DataConvert.GetYamlString(mapping, "currentModule").Trim('"', '\'');
        page.Chapter = DataConvert.GetYamlString(mapping, "chapter").Trim('"', '\'');

        var dateStr = DataConvert.GetYamlString(mapping, "date");
        if (DateTime.TryParse(dateStr, out var date)) page.Date = date;

        var updatedStr = DataConvert.GetYamlString(mapping, "updated");
        if (DateTime.TryParse(updatedStr, out var updated)) page.Updated = updated;

        page.Tags = DataConvert.GetYamlStringList(mapping, "tags");
        page.Categories = DataConvert.GetYamlStringList(mapping, "categories");
        page.Modules = DataConvert.GetYamlStringList(mapping, "modules");

        foreach (var (key, value) in mapping.properties)
            if (key is not ("title" or "layout" or "date" or "updated" or "summary" or "slug"
                or "draft" or "weight" or "tags" or "categories" or "version" or "modules"
                or "currentModule" or "chapter"))
                page.Extra[key] = DataConvert.YamlValueToObject(value) ?? "";
    }

    private static string GetSection(string relativePath)
    {
        var parts = relativePath.Replace("\\", "/").Split('/');
        if (parts.Length <= 1) return "";
        if (parts.Length > 2) return string.Join("/", parts[..^1]);
        return parts[0];
    }

    private string GenerateSlug(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        return name.ToLowerInvariant().Replace(" ", "-");
    }

    private string GeneratePermalink(PageData page)
    {
        var pattern = _config.PermalinkPattern;
        pattern = pattern.Replace(":section", page.Section);
        pattern = pattern.Replace(":slug", page.Slug);
        pattern = pattern.Replace(":year", page.Date.ToString("yyyy"));
        pattern = pattern.Replace(":month", page.Date.ToString("MM"));
        pattern = pattern.Replace(":day", page.Date.ToString("dd"));
        pattern = pattern.Replace(":title", page.Slug);

        if (!pattern.StartsWith("/")) pattern = "/" + pattern;
        if (!pattern.EndsWith("/")) pattern += "/";

        return pattern;
    }

    private string GenerateSummary(string htmlContent)
    {
        var plainText = Regex.Replace(htmlContent, @"<[^>]+>", "");
        plainText = Regex.Replace(plainText, @"\s+", " ").Trim();
        return plainText.Length > 200 ? plainText[..200] + "..." : plainText;
    }

    #endregion

    #region 页面渲染

    private void ProcessContentPages(string outputDir)
    {
        foreach (var page in _pages) RenderPage(page, outputDir);
    }

    private void RenderPage(PageData page, string outputDir)
    {
        var context = BuildContext(page);

        var layoutName = string.IsNullOrEmpty(page.Layout) ? _config.DefaultLayout : page.Layout;
        var layoutPath = Path.Combine(_sourceDir, $"layouts/{layoutName}.dora");
        if (!File.Exists(layoutPath))
        {
            layoutName = "default";
            layoutPath = Path.Combine(_sourceDir, "layouts/default.dora");
        }

        var blockName = layoutName is "api-doc" or "guide" ? "doc-content" : "content";
        var pageTemplate = $"<% extends 'layouts/{layoutName}.dora' %>\n" +
                           $"<% block {blockName} %>\n" +
                           $"{page.Content}\n" +
                           "<% end block %>";

        var rendered = _renderer.Render(pageTemplate, context);

        if (_pluginManager.HtmlPostProcessors.Count > 0)
            rendered = _pluginManager.RunHtmlPostProcessAsync(rendered, PageMetadata.FromPageData(page)).GetAwaiter()
                .GetResult();

        var outputPath = Path.Combine(outputDir, page.Url.TrimStart('/'), "index.html");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, rendered);
    }

    #endregion

    #region 分页

    private void GeneratePaginatedPages(string outputDir)
    {
        if (_config.Paginate <= 0) return;

        var posts = _pages.Where(p => p.Section == "posts").ToList();
        var totalPages = (int)Math.Ceiling((double)posts.Count / _config.Paginate);

        for (var page = 1; page <= totalPages; page++)
        {
            var paginated = CreatePaginatedList(posts, page);
            var context = BuildContext(new PageData { Title = _config.Title, Layout = "index", Url = "/" });
            context["paginated"] = paginated;

            var rendered = RenderLayoutWithFallback("index", context);

            if (page == 1) File.WriteAllText(Path.Combine(outputDir, "index.html"), rendered);

            var pageDir = Path.Combine(outputDir, "posts", "page", page.ToString());
            Directory.CreateDirectory(pageDir);
            File.WriteAllText(Path.Combine(pageDir, "index.html"), rendered);
        }
    }

    private PaginatedList CreatePaginatedList(List<PageData> items, int page)
    {
        var totalItems = items.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / _config.Paginate);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pageItems = items
            .Skip((page - 1) * _config.Paginate)
            .Take(_config.Paginate)
            .ToList();

        return new PaginatedList
        {
            Items = pageItems,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems
        };
    }

    #endregion

    #region 分类法（标签/分类）

    private void GenerateTaxonomyPages(string outputDir)
    {
        foreach (var (taxoKey, _) in _config.Taxonomies)
        {
            if (!_taxonomies.TryGetValue(taxoKey, out var termDict)) continue;

            GenerateTaxonomyListPage(taxoKey, termDict, outputDir);
            GenerateTaxonomyTermPages(taxoKey, termDict, outputDir);
        }
    }

    private void GenerateTaxonomyListPage(string taxoKey, Dictionary<string, List<PageData>> termDict, string outputDir)
    {
        var taxoDir = Path.Combine(outputDir, taxoKey);
        Directory.CreateDirectory(taxoDir);

        var label = _config.Taxonomies.TryGetValue(taxoKey, out var configuredLabel) ? configuredLabel : taxoKey;
        var context = BuildContext(new PageData { Title = label, Layout = taxoKey, Url = $"/{taxoKey}/" });
        context["terms"] = termDict.Keys.Select(k => new Dictionary<string, object>
        {
            ["name"] = k,
            ["url"] = $"/{taxoKey}/{k.ToLowerInvariant().Replace(" ", "-")}/",
            ["count"] = termDict[k].Count
        }).ToList();
        context["currentTaxonomy"] = taxoKey;
        context["currentTaxonomyLabel"] = label;

        string rendered;
        var layoutPath = Path.Combine(_sourceDir, $"{taxoKey}.dora");

        if (File.Exists(layoutPath))
            rendered = RenderLayoutWithFallback(taxoKey, context);
        else
            rendered = GenerateDefaultTaxonomyListHtml(taxoKey, label, termDict, context);

        File.WriteAllText(Path.Combine(taxoDir, "index.html"), rendered);
    }

    private void GenerateTaxonomyTermPages(string taxoKey, Dictionary<string, List<PageData>> termDict,
        string outputDir)
    {
        var taxoDir = Path.Combine(outputDir, taxoKey);

        foreach (var (term, pages) in termDict)
        {
            var termSlug = term.ToLowerInvariant().Replace(" ", "-");
            var termDir = Path.Combine(taxoDir, termSlug);
            Directory.CreateDirectory(termDir);

            var label = _config.Taxonomies.TryGetValue(taxoKey, out var configuredLabel) ? configuredLabel : taxoKey;
            var context = BuildContext(new PageData
                { Title = $"{label}: {term}", Layout = "term", Url = $"/{taxoKey}/{termSlug}/" });
            context["currentTaxonomy"] = taxoKey;
            context["currentTaxonomyLabel"] = label;
            context["currentTerm"] = term;
            context["termPages"] = pages.Select(p => new Dictionary<string, object>
            {
                ["title"] = p.Title,
                ["url"] = p.Url,
                ["date"] = p.Date.ToString("yyyy-MM-dd"),
                ["summary"] = p.Summary
            }).ToList();

            string rendered;
            var termLayoutPath = Path.Combine(_sourceDir, "layouts/term.dora");

            if (File.Exists(termLayoutPath))
                rendered = RenderLayoutWithFallback("term", context);
            else
                rendered = GenerateDefaultTaxonomyTermHtml(label, term, pages, context);

            File.WriteAllText(Path.Combine(termDir, "index.html"), rendered);
        }
    }

    private string GenerateDefaultTaxonomyListHtml(string taxoKey, string label,
        Dictionary<string, List<PageData>> termDict, Dictionary<string, object> context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h1>{HtmlUtility.EscapeHtml(label)}</h1>");
        sb.AppendLine("<ul class=\"taxonomy-list\">");

        foreach (var (term, pages) in termDict)
        {
            var termUrl = $"/{taxoKey}/{term.ToLowerInvariant().Replace(" ", "-")}/";
            sb.AppendLine($"<li><a href=\"{termUrl}\">{HtmlUtility.EscapeHtml(term)}</a> ({pages.Count})</li>");
        }

        sb.AppendLine("</ul>");

        var layoutPath = Path.Combine(_sourceDir, "layouts/default.dora");
        if (!File.Exists(layoutPath)) return WrapInDefaultHtml(sb.ToString());

        var pageTemplate = $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{sb}\n<% end block %>";
        return _renderer.Render(pageTemplate, context);
    }

    private string GenerateDefaultTaxonomyTermHtml(string label, string term, List<PageData> pages,
        Dictionary<string, object> context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h1>{HtmlUtility.EscapeHtml(label)}: {HtmlUtility.EscapeHtml(term)}</h1>");

        foreach (var page in pages)
        {
            sb.AppendLine("<article>");
            sb.AppendLine($"<h2><a href=\"{page.Url}\">{HtmlUtility.EscapeHtml(page.Title)}</a></h2>");
            sb.AppendLine($"<time>{page.Date:yyyy-MM-dd}</time>");
            sb.AppendLine($"<p>{HtmlUtility.EscapeHtml(page.Summary)}</p>");
            sb.AppendLine("</article>");
        }

        var layoutPath = Path.Combine(_sourceDir, "layouts/default.dora");
        if (!File.Exists(layoutPath)) return WrapInDefaultHtml(sb.ToString());

        var pageTemplate = $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{sb}\n<% end block %>";
        return _renderer.Render(pageTemplate, context);
    }

    private string WrapInDefaultHtml(string content)
    {
        return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head><meta charset=""UTF-8""><title>{HtmlUtility.EscapeHtml(_config.Title)}</title></head>
<body>{content}</body>
</html>";
    }

    #endregion

    #region 搜索索引

    private void GenerateSearchIndex(string outputDir)
    {
        var items = new List<Dictionary<string, object>>();
        foreach (var page in _pages)
        {
            var plainContent = Regex.Replace(page.Content, @"<[^>]+>", "");
            plainContent = Regex.Replace(plainContent, @"\s+", " ").Trim();

            plainContent = SafeTruncate(plainContent, 500);

            items.Add(new Dictionary<string, object>
            {
                ["title"] = page.Title,
                ["url"] = page.Url,
                ["content"] = plainContent,
                ["date"] = page.Date.ToString("yyyy-MM-dd")
            });
        }

        var json = DataConvert.SerializeJson(items);
        File.WriteAllText(Path.Combine(outputDir, "search-index.json"), json);
    }

    /// <summary>
    ///     生成搜索页面 /search/index.html
    /// </summary>
    private void GenerateSearchPage(string outputDir)
    {
        var searchPage = new PageData
        {
            Title = "搜索",
            Layout = "search",
            Url = "/search/"
        };

        var context = BuildContext(searchPage);
        var rendered = RenderLayoutWithFallback("search", context);

        var outputDirFull = Path.Combine(outputDir, "search");
        Directory.CreateDirectory(outputDirFull);
        File.WriteAllText(Path.Combine(outputDirFull, "index.html"), rendered);
    }

    private static string SafeTruncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;

        for (var i = maxLength; i > maxLength - 4 && i > 0; i--)
            if (!char.IsHighSurrogate(text[i]))
                return text[..i] + "...";

        return text[..maxLength] + "...";
    }

    #endregion

    #region 静态文件

    private void CopyStaticFiles(string outputDir)
    {
        var staticDir = Path.Combine(_sourceDir, "static");
        if (!Directory.Exists(staticDir)) return;

        foreach (var file in Directory.GetFiles(staticDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(staticDir, file);
            var outputPath = Path.Combine(outputDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            if (_optimize)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".css")
                {
                    var content = File.ReadAllText(file);
                    var minified = MinifyCss(content);
                    File.WriteAllText(outputPath, minified);
                    continue;
                }

                if (ext is ".js")
                {
                    var content = File.ReadAllText(file);
                    var minified = MinifyJs(content);
                    File.WriteAllText(outputPath, minified);
                    continue;
                }

                if (ext is ".png" or ".jpg" or ".jpeg")
                {
                    var webpPath = Path.ChangeExtension(outputPath, ".webp");
                    ConvertToWebP(file, webpPath);
                    continue;
                }
            }

            File.Copy(file, outputPath, true);
        }

        if (_optimize) UpdateHtmlImageReferences(outputDir);
    }

    #region 资源压缩

    /// <summary>
    ///     简单压缩 CSS：去除块注释、压缩空白、合并为单行
    /// </summary>
    private static string MinifyCss(string css)
    {
        var noComments = RemoveBlockComments(css);
        var lines = noComments.Split('\n');
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            trimmed = Regex.Replace(trimmed, @"\s+", " ");
            sb.Append(trimmed);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     简单压缩 JS：去除块注释和行注释、压缩空白、合并为单行
    ///     注意处理字符串内的注释字符以避免误删
    /// </summary>
    private static string MinifyJs(string js)
    {
        var noBlockComments = RemoveBlockComments(js);
        var noLineComments = RemoveLineComments(noBlockComments);
        var lines = noLineComments.Split('\n');
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            trimmed = Regex.Replace(trimmed, @"\s+", " ");
            sb.Append(trimmed);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     去除 /* */ 块注释，保留字符串内的内容
    /// </summary>
    private static string RemoveBlockComments(string input)
    {
        var sb = new StringBuilder();
        var i = 0;
        var inDoubleString = false;
        var inSingleString = false;
        var inTemplateString = false;

        while (i < input.Length)
        {
            var c = input[i];

            if (!inDoubleString && !inTemplateString && c == '\'' && !IsEscaped(input, i))
            {
                inSingleString = !inSingleString;
                sb.Append(c);
                i++;
                continue;
            }

            if (!inSingleString && !inTemplateString && c == '"' && !IsEscaped(input, i))
            {
                inDoubleString = !inDoubleString;
                sb.Append(c);
                i++;
                continue;
            }

            if (!inSingleString && !inDoubleString && c == '`' && !IsEscaped(input, i))
            {
                inTemplateString = !inTemplateString;
                sb.Append(c);
                i++;
                continue;
            }

            if (!inSingleString && !inDoubleString && !inTemplateString &&
                c == '/' && i + 1 < input.Length && input[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < input.Length)
                {
                    if (input[i] == '*' && input[i + 1] == '/')
                    {
                        i += 2;
                        break;
                    }

                    i++;
                }

                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    ///     去除 // 行注释，保留字符串内的内容
    /// </summary>
    private static string RemoveLineComments(string input)
    {
        var lines = input.Split('\n');
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            var inDoubleString = false;
            var inSingleString = false;
            var inTemplateString = false;

            var j = 0;
            while (j < line.Length)
            {
                var c = line[j];

                if (!inDoubleString && !inTemplateString && c == '\'' && !IsEscaped(line, j))
                {
                    inSingleString = !inSingleString;
                    sb.Append(c);
                    j++;
                    continue;
                }

                if (!inSingleString && !inTemplateString && c == '"' && !IsEscaped(line, j))
                {
                    inDoubleString = !inDoubleString;
                    sb.Append(c);
                    j++;
                    continue;
                }

                if (!inSingleString && !inDoubleString && c == '`' && !IsEscaped(line, j))
                {
                    inTemplateString = !inTemplateString;
                    sb.Append(c);
                    j++;
                    continue;
                }

                if (!inSingleString && !inDoubleString && !inTemplateString &&
                    c == '/' && j + 1 < line.Length && line[j + 1] == '/')
                    break;

                sb.Append(c);
                j++;
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    ///     判断指定位置的字符是否被反斜杠转义
    /// </summary>
    private static bool IsEscaped(string input, int index)
    {
        if (index == 0) return false;

        var backslashCount = 0;
        var pos = index - 1;
        while (pos >= 0 && input[pos] == '\\')
        {
            backslashCount++;
            pos--;
        }

        return backslashCount % 2 == 1;
    }

    #endregion

    #region 图像优化

    /// <summary>
    ///     将图像转换为 WebP 格式（80% 质量）
    /// </summary>
    private static void ConvertToWebP(string inputPath, string outputPath)
    {
        using var inputStream = File.OpenRead(inputPath);
        using var bitmap = SkiaSharp.SKBitmap.Decode(inputStream);
        if (bitmap == null)
        {
            File.Copy(inputPath, outputPath, true);
            return;
        }

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Webp, 80);
        using var outputStream = File.Create(outputPath);
        data.SaveTo(outputStream);
    }

    #endregion

    #region HTML 后处理

    /// <summary>
    ///     将输出目录中 HTML 文件的图像引用从 .png/.jpg/.jpeg 替换为 .webp
    /// </summary>
    private static void UpdateHtmlImageReferences(string outputDir)
    {
        foreach (var htmlFile in Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(htmlFile);
            var updated = content
                .Replace(".png\"", ".webp\"")
                .Replace(".png'", ".webp'")
                .Replace(".jpg\"", ".webp\"")
                .Replace(".jpg'", ".webp'")
                .Replace(".jpeg\"", ".webp\"")
                .Replace(".jpeg'", ".webp'");
            File.WriteAllText(htmlFile, updated);
        }
    }

    #endregion

    #endregion

    #region 上下文构建

    private Dictionary<string, object> BuildContext(PageData page)
    {
        var context = new Dictionary<string, object>
        {
            ["site"] = new Dictionary<string, object>
            {
                ["title"] = _config.Title,
                ["description"] = _config.Description,
                ["author"] = _config.Author,
                ["language"] = _config.Language,
                ["baseUrl"] = _config.BaseUrl,
                ["navigation"] = _config.Navigation.Select(n => new Dictionary<string, object>
                {
                    ["label"] = n.Label,
                    ["url"] = n.Url
                }).ToList(),
                ["sidebar"] = _config.Sidebar.Select(n => new Dictionary<string, object>
                {
                    ["label"] = n.Label,
                    ["url"] = n.Url
                }).ToList(),
                ["languages"] = _config.Languages.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)new Dictionary<string, string>(kvp.Value)
                ),
                ["currentLang"] = _config.Language
            },
            ["page"] = new Dictionary<string, object>
            {
                ["title"] = page.Title,
                ["content"] = page.Content,
                ["rawContent"] = page.RawContent,
                ["date"] = page.Date.ToString("yyyy-MM-dd"),
                ["url"] = page.Url,
                ["summary"] = page.Summary,
                ["tags"] = page.Tags,
                ["categories"] = page.Categories,
                ["draft"] = page.Draft,
                ["section"] = page.Section,
                ["slug"] = page.Slug,
                ["version"] = page.Version,
                ["modules"] = page.Modules.Select(m => new Dictionary<string, object>
                {
                    ["name"] = m,
                    ["url"] = $"/docs/api/{m.ToLowerInvariant()}/"
                }).ToList(),
                ["currentModule"] = page.CurrentModule,
                ["chapter"] = page.Chapter,
                ["chapters"] = BuildGuideChapters(),
                ["breadcrumbs"] = BuildBreadcrumbs(page),
                ["toc"] = page.TocItems.Select(t => new Dictionary<string, object>
                {
                    ["level"] = t.level,
                    ["text"] = t.text,
                    ["id"] = t.id
                }).ToList()
            },
            ["posts"] = _cachedPostsForContext,
            ["data"] = _data,
            ["tags"] = _tags.Keys.ToList(),
            ["categories"] = _categories.Keys.ToList(),
            ["apiDocs"] = _cachedApiDocsForContext,
            ["guideDocs"] = BuildGuideDocList(),
            ["guideChapters"] = BuildGuideChapters()
        };

        if (page.Previous != null)
            ((Dictionary<string, object>)context["page"])["previous"] = new Dictionary<string, object>
            {
                ["title"] = page.Previous.Title,
                ["url"] = page.Previous.Url
            };

        if (page.Next != null)
            ((Dictionary<string, object>)context["page"])["next"] = new Dictionary<string, object>
            {
                ["title"] = page.Next.Title,
                ["url"] = page.Next.Url
            };

        if (_config.Languages.Count > 1)
        {
            var pageBaseUrl = GetBaseUrlWithoutLanguage(page.Url);
            var hreflangs = new List<Dictionary<string, object>>();
            var languageLinks = new List<Dictionary<string, object>>();

            foreach (var (langCode, _) in _config.Languages)
            {
                var langUrl = BuildLanguageUrl(pageBaseUrl, langCode);
                hreflangs.Add(new Dictionary<string, object>
                {
                    ["lang"] = langCode,
                    ["url"] = langUrl
                });
                languageLinks.Add(new Dictionary<string, object>
                {
                    ["code"] = langCode,
                    ["label"] = GetLanguageDisplayName(langCode),
                    ["url"] = langUrl,
                    ["active"] = langCode == page.Language
                });
            }

            hreflangs.Add(new Dictionary<string, object>
            {
                ["lang"] = "x-default",
                ["url"] = pageBaseUrl
            });

            ((Dictionary<string, object>)context["page"])["hreflangs"] = hreflangs;
            ((Dictionary<string, object>)context["page"])["languageLinks"] = languageLinks;
            ((Dictionary<string, object>)context["page"])["currentLanguage"] = page.Language;
        }

        return context;
    }

    private static List<Dictionary<string, object>> BuildBreadcrumbs(PageData page)
    {
        var breadcrumbs = new List<Dictionary<string, object>>
        {
            new()
            {
                ["label"] = "首页",
                ["url"] = "/"
            }
        };

        if (string.IsNullOrEmpty(page.Section)) return breadcrumbs;

        var parts = page.Section.Split('/');
        var accumulatedUrl = "/";

        foreach (var part in parts)
        {
            accumulatedUrl += $"{part}/";
            breadcrumbs.Add(new Dictionary<string, object>
            {
                ["label"] = part,
                ["url"] = accumulatedUrl
            });
        }

        if (!string.IsNullOrEmpty(page.Title) && breadcrumbs.LastOrDefault() is { } last)
            if ((string)last["label"] != page.Title)
                breadcrumbs.Add(new Dictionary<string, object>
                {
                    ["label"] = page.Title,
                    ["url"] = page.Url
                });

        return breadcrumbs;
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     语言代码到显示名称的映射
    /// </summary>
    private static readonly Dictionary<string, string> LanguageDisplayNames = new()
    {
        ["zh-CN"] = "中文",
        ["zh-TW"] = "繁體中文",
        ["en"] = "English",
        ["ja"] = "日本語",
        ["ko"] = "한국어",
        ["fr"] = "Français",
        ["de"] = "Deutsch",
        ["es"] = "Español",
        ["pt"] = "Português",
        ["ru"] = "Русский",
        ["ar"] = "العربية"
    };

    /// <summary>
    ///     获取语言代码的显示名称
    /// </summary>
    private static string GetLanguageDisplayName(string langCode)
    {
        return LanguageDisplayNames.TryGetValue(langCode, out var name) ? name : langCode;
    }

    /// <summary>
    ///     去除 URL 中的语言前缀，得到基础 URL
    /// </summary>
    private string GetBaseUrlWithoutLanguage(string url)
    {
        foreach (var lang in _config.Languages.Keys)
        {
            var prefix = $"/{lang}/";
            if (url.StartsWith(prefix)) return "/" + url.Substring(prefix.Length);
        }

        return url;
    }

    /// <summary>
    ///     构建指定语言代码的完整 URL
    /// </summary>
    private string BuildLanguageUrl(string baseUrl, string langCode)
    {
        if (langCode == _config.Language) return baseUrl;

        return $"/{langCode}{baseUrl}";
    }

    private string RenderLayoutWithFallback(string layoutName, Dictionary<string, object> context)
    {
        var layoutPath = Path.Combine(_sourceDir, $"layouts/{layoutName}.dora");
        if (!File.Exists(layoutPath)) layoutPath = Path.Combine(_sourceDir, "layouts/default.dora");

        var layoutContent = File.ReadAllText(layoutPath);

        string rendered;
        if (layoutContent.Contains("<% extends "))
        {
            rendered = _renderer.Render(layoutContent, context);
        }
        else
        {
            var pageTemplate = "<% extends 'layouts/default.dora' %>\n" +
                               "<% block content %>\n" +
                               $"{layoutContent}\n" +
                               "<% end block %>";
            rendered = _renderer.Render(pageTemplate, context);
        }

        if (_pluginManager.HtmlPostProcessors.Count > 0)
        {
            var metadata = ExtractPageMetadata(context);
            rendered = _pluginManager.RunHtmlPostProcessAsync(rendered, metadata).GetAwaiter().GetResult();
        }

        return rendered;
    }

    /// <summary>
    ///     从渲染上下文中提取页面元数据
    /// </summary>
    private static PageMetadata ExtractPageMetadata(Dictionary<string, object> context)
    {
        if (context.TryGetValue("page", out var pageObj) && pageObj is Dictionary<string, object> pageDict)
            return new PageMetadata
            {
                Title = pageDict.TryGetValue("title", out var t) ? t?.ToString() ?? "" : "",
                Slug = pageDict.TryGetValue("slug", out var s) ? s?.ToString() ?? "" : "",
                Url = pageDict.TryGetValue("url", out var u) ? u?.ToString() ?? "" : "",
                Summary = pageDict.TryGetValue("summary", out var sm) ? sm?.ToString() ?? "" : "",
                Section = pageDict.TryGetValue("section", out var se) ? se?.ToString() ?? "" : "",
                Draft = pageDict.TryGetValue("draft", out var d) && d is bool b && b,
                Tags = pageDict.TryGetValue("tags", out var tags) && tags is List<string> tagList
                    ? tagList
                    : [],
                Categories = pageDict.TryGetValue("categories", out var cats) && cats is List<string> catList
                    ? catList
                    : []
            };

        return new PageMetadata();
    }

    private Dictionary<string, List<PageData>> GetChapterGroups()
    {
        var guidePages = _pages.Where(p => p.Section == "docs/guide").OrderBy(p => p.Weight).ToList();
        var chapterGroups = new Dictionary<string, List<PageData>>();

        foreach (var page in guidePages)
        {
            var chapterName = string.IsNullOrEmpty(page.Chapter) ? page.Title : page.Chapter;
            if (!chapterGroups.ContainsKey(chapterName)) chapterGroups[chapterName] = [];

            chapterGroups[chapterName].Add(page);
        }

        return chapterGroups;
    }

    private List<Dictionary<string, object>> BuildGuideChapters()
    {
        var chapters = new List<Dictionary<string, object>>();

        foreach (var (chapterName, pages) in GetChapterGroups())
        {
            var sections = pages.Select(p => new Dictionary<string, object>
            {
                ["title"] = p.Title,
                ["url"] = p.Url
            }).ToList();

            chapters.Add(new Dictionary<string, object>
            {
                ["title"] = chapterName,
                ["sections"] = sections
            });
        }

        return chapters;
    }

    private List<Dictionary<string, object>> BuildGuideDocList()
    {
        var flatList = new List<Dictionary<string, object>>();

        foreach (var (chapterName, pages) in GetChapterGroups())
        {
            flatList.Add(new Dictionary<string, object>
            {
                ["type"] = "chapter",
                ["title"] = chapterName
            });

            foreach (var page in pages)
                flatList.Add(new Dictionary<string, object>
                {
                    ["type"] = "section",
                    ["title"] = page.Title,
                    ["url"] = page.Url
                });
        }

        return flatList;
    }

    #endregion
}