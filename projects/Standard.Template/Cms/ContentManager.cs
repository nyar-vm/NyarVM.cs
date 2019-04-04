using System.Text.RegularExpressions;

namespace Std.Template.Cms;

#region 数据模型

/// <summary>
///     CMS 内容条目
/// </summary>
public class ContentEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ContentType { get; set; } = "page";
    public string Content { get; set; } = "";
    public string RawContent { get; set; } = "";
    public string Layout { get; set; } = "default";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string Author { get; set; } = "";
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public List<string> Tags { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string Summary { get; set; } = "";
    public int Weight { get; set; }
    public Dictionary<string, object> Fields { get; set; } = new();
    public List<TocItem> TocItems { get; set; } = [];
}

/// <summary>
///     内容状态
/// </summary>
public enum ContentStatus
{
    Draft,
    Published,
    Archived,
    Scheduled
}

/// <summary>
///     内容类型定义
/// </summary>
public class ContentTypeDefinition
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Layout { get; set; } = "default";
    public List<FieldDefinition> Fields { get; set; } = [];
}

/// <summary>
///     字段定义
/// </summary>
public class FieldDefinition
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "text";
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
///     CMS 查询条件
/// </summary>
public class ContentQuery
{
    public string? ContentType { get; set; }
    public string? Tag { get; set; }
    public string? Category { get; set; }
    public string? Author { get; set; }
    public ContentStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string OrderBy { get; set; } = "date";
    public bool Descending { get; set; } = true;
}

/// <summary>
///     查询结果
/// </summary>
public class ContentQueryResult
{
    public List<ContentEntry> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

#endregion

/// <summary>
///     内容管理系统
/// </summary>
public sealed class ContentManager
{
    private readonly Dictionary<string, ContentTypeDefinition> _contentTypes = new();
    private readonly List<ContentEntry> _entries = [];
    private readonly MarkdownLanguage _markdownLanguage;
    private readonly MarkdownRenderer _markdownRenderer;
    private readonly DejaVuRenderer _renderer;
    private readonly string _sourceDir = "";
    private readonly TemplateManager _templateManager;

    public ContentManager(string sourceDir)
    {
        _sourceDir = Path.GetFullPath(sourceDir);
        var loader = new FileSystemTemplateLoader(_sourceDir);
        _templateManager = new TemplateManager(loader);
        _renderer = new DejaVuRenderer(DejaVuLanguage.dora, _templateManager);
        _markdownLanguage = new MarkdownLanguage();
        _markdownRenderer = new MarkdownRenderer(new MarkdownRenderOptions
        {
            GenerateHeadingIds = true,
            HighlightCode = true,
            GenerateToc = true
        });
    }

    /// <summary>
    ///     加载内容仓库
    /// </summary>
    public void Load()
    {
        LoadContentTypes();
        LoadEntries();
    }

    #region 内容类型

    private void LoadContentTypes()
    {
        var typesDir = Path.Combine(_sourceDir, "content-types");
        if (!Directory.Exists(typesDir)) return;

        foreach (var file in Directory.GetFiles(typesDir, "*.yaml"))
        {
            var typeName = Path.GetFileNameWithoutExtension(file);
            var definition = new ContentTypeDefinition { Name = typeName, Slug = typeName.ToLowerInvariant() };

            var content = File.ReadAllText(file);
            var parser = new YamlParser();
            var result = parser.parse(content);
            if (result.success && result.value is YamlMapping mapping)
            {
                definition.Name = DataConvert.GetYamlString(mapping, "name", definition.Name);
                definition.Slug = DataConvert.GetYamlString(mapping, "slug", definition.Slug);
                definition.Layout = DataConvert.GetYamlString(mapping, "layout", definition.Layout);
            }

            _contentTypes[typeName] = definition;
        }
    }

    #endregion

    #region 条目加载

    private void LoadEntries()
    {
        _entries.Clear();
        var contentDir = Path.Combine(_sourceDir, "content");
        if (!Directory.Exists(contentDir)) return;

        foreach (var file in Directory.GetFiles(contentDir, "*.md", SearchOption.AllDirectories))
        {
            var entry = ParseEntry(file);
            if (entry != null) _entries.Add(entry);
        }
    }

    private ContentEntry? ParseEntry(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var entry = new ContentEntry();
        var relativePath = Path.GetRelativePath(Path.Combine(_sourceDir, "content"), filePath);

        var (frontMatter, body) = FrontMatterParser.Extract(content);
        if (frontMatter != null)
        {
            var mapping = FrontMatterParser.Parse(frontMatter);
            if (mapping != null) ParseEntryFrontMatterFromMapping(mapping, entry);
        }
        else
        {
            body = content;
        }

        entry.RawContent = body;
        entry.ContentType = GetContentType(relativePath);
        entry.Slug = GenerateSlug(relativePath);

        var markdownDocument = _markdownLanguage.parse(body);
        var mdResult = _markdownRenderer.Render(markdownDocument);
        entry.Content = mdResult.Html;
        entry.TocItems = mdResult.TocItems.ToList();

        if (string.IsNullOrEmpty(entry.Summary))
        {
            var plainText = Regex.Replace(body, @"[#*\[\]\(\)`]", "");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();
            entry.Summary = plainText.Length > 200 ? plainText[..200] + "..." : plainText;
        }

        return entry;
    }

    private void ParseEntryFrontMatterFromMapping(YamlMapping mapping, ContentEntry entry)
    {
        entry.Title = DataConvert.GetYamlString(mapping, "title", entry.Title);
        entry.Slug = DataConvert.GetYamlString(mapping, "slug", entry.Slug);
        entry.Layout = DataConvert.GetYamlString(mapping, "layout", entry.Layout);
        entry.ContentType = DataConvert.GetYamlString(mapping, "type", entry.ContentType);
        entry.Author = DataConvert.GetYamlString(mapping, "author", entry.Author);
        entry.Summary = DataConvert.GetYamlString(mapping, "summary", entry.Summary);
        entry.Weight = DataConvert.GetYamlInt(mapping, "weight");

        var statusStr = DataConvert.GetYamlString(mapping, "status");
        entry.Status = statusStr.ToLowerInvariant() switch
        {
            "published" => ContentStatus.Published,
            "archived" => ContentStatus.Archived,
            "scheduled" => ContentStatus.Scheduled,
            _ => ContentStatus.Draft
        };

        entry.Tags = DataConvert.GetYamlStringList(mapping, "tags");
        entry.Categories = DataConvert.GetYamlStringList(mapping, "categories");

        var createdStr = DataConvert.GetYamlString(mapping, "createdAt");
        if (DateTime.TryParse(createdStr, out var created)) entry.CreatedAt = created;

        var updatedStr = DataConvert.GetYamlString(mapping, "updatedAt");
        if (DateTime.TryParse(updatedStr, out var updated)) entry.UpdatedAt = updated;

        foreach (var (key, value) in mapping.properties)
            if (key is not ("title" or "slug" or "layout" or "type" or "author" or "summary"
                or "weight" or "status" or "tags" or "categories" or "createdAt" or "updatedAt"))
                entry.fields[key] = DataConvert.YamlValueToObject(value) ?? "";
    }

    private static string GetContentType(string relativePath)
    {
        var parts = relativePath.Replace("\\", "/").Split('/');
        return parts.Length > 1 ? parts[0] : "page";
    }

    private static string GenerateSlug(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        return name.ToLowerInvariant().Replace(" ", "-");
    }

    #endregion

    #region 查询

    /// <summary>
    ///     查询内容条目
    /// </summary>
    public ContentQueryResult Query(ContentQuery query)
    {
        IEnumerable<ContentEntry> results = _entries;

        if (!string.IsNullOrEmpty(query.ContentType))
            results = results.Where(e => e.ContentType == query.ContentType);

        if (!string.IsNullOrEmpty(query.Tag))
            results = results.Where(e => e.Tags.Contains(query.Tag));

        if (!string.IsNullOrEmpty(query.Category))
            results = results.Where(e => e.Categories.Contains(query.Category));

        if (!string.IsNullOrEmpty(query.Author))
            results = results.Where(e => e.Author == query.Author);

        if (query.Status.HasValue)
            results = results.Where(e => e.Status == query.Status.Value);

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLowerInvariant();
            results = results.Where(e =>
                e.Title.ToLowerInvariant().Contains(term) ||
                e.RawContent.ToLowerInvariant().Contains(term));
        }

        results = query.OrderBy switch
        {
            "title" => query.Descending ? results.OrderByDescending(e => e.Title) : results.OrderBy(e => e.Title),
            "weight" => query.Descending ? results.OrderByDescending(e => e.Weight) : results.OrderBy(e => e.Weight),
            _ => query.Descending ? results.OrderByDescending(e => e.CreatedAt) : results.OrderBy(e => e.CreatedAt)
        };

        var totalCount = results.Count();
        var items = results
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new ContentQueryResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    ///     根据 Slug 获取条目
    /// </summary>
    public ContentEntry? GetBySlug(string slug)
    {
        return _entries.FirstOrDefault(e => e.Slug == slug);
    }

    /// <summary>
    ///     根据 ID 获取条目
    /// </summary>
    public ContentEntry? GetById(string id)
    {
        return _entries.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    ///     获取所有标签
    /// </summary>
    public Dictionary<string, int> GetTags()
    {
        return _entries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    ///     获取所有分类
    /// </summary>
    public Dictionary<string, int> GetCategories()
    {
        return _entries
            .SelectMany(e => e.Categories)
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    ///     获取所有作者
    /// </summary>
    public List<string> GetAuthors()
    {
        return [.. _entries.Select(e => e.Author).Where(a => !string.IsNullOrEmpty(a)).Distinct()];
    }

    #endregion

    #region 渲染

    /// <summary>
    ///     渲染内容条目为 HTML
    /// </summary>
    public string Render(ContentEntry entry)
    {
        var context = new Dictionary<string, object>
        {
            ["entry"] = new Dictionary<string, object>
            {
                ["title"] = entry.Title,
                ["content"] = entry.Content,
                ["summary"] = entry.Summary,
                ["author"] = entry.Author,
                ["date"] = entry.CreatedAt.ToString("yyyy-MM-dd"),
                ["tags"] = entry.Tags,
                ["categories"] = entry.Categories,
                ["slug"] = entry.Slug,
                ["type"] = entry.ContentType,
                ["toc"] = entry.TocItems.Select(t => new Dictionary<string, object>
                {
                    ["level"] = t.level,
                    ["text"] = t.text,
                    ["id"] = t.id
                }).ToList()
            }
        };

        var layoutName = entry.Layout;
        var pageTemplate =
            $"<% extends 'layouts/{layoutName}.dora' %>\n<% block content %>\n{entry.Content}\n<% end block %>";
        return _renderer.Render(pageTemplate, context);
    }

    /// <summary>
    ///     渲染内容条目列表为 HTML
    /// </summary>
    public string RenderList(ContentQueryResult queryResult, string listTemplate)
    {
        var context = new Dictionary<string, object>
        {
            ["items"] = queryResult.Items.Select(e => new Dictionary<string, object>
            {
                ["title"] = e.Title,
                ["summary"] = e.Summary,
                ["url"] = $"/{e.ContentType}/{e.Slug}/",
                ["date"] = e.CreatedAt.ToString("yyyy-MM-dd"),
                ["author"] = e.Author,
                ["tags"] = e.Tags
            }).ToList(),
            ["pagination"] = new Dictionary<string, object>
            {
                ["page"] = queryResult.Page,
                ["pageSize"] = queryResult.PageSize,
                ["totalCount"] = queryResult.TotalCount,
                ["totalPages"] = queryResult.TotalPages
            }
        };

        return _renderer.Render(listTemplate, context);
    }

    #endregion
}