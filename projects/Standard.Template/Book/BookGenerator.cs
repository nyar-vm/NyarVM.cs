using System.Text;
using Std.Data.Text.DejaVu;
using Std.Data.Text.DejaVu.Loader;
using Std.Data.Text.Markdown;
using Std.Data.Text.Yaml;

namespace Std.Template.Book;

#region 数据模型

/// <summary>
///     书籍配置
/// </summary>
public class BookConfig
{
    public string Title { get; set; } = "My Book";
    public string Author { get; set; } = "";
    public string Language { get; set; } = "zh-CN";
    public string Description { get; set; } = "";
    public string Isbn { get; set; } = "";
    public DateTime PublishDate { get; set; } = DateTime.Now;
    public string CoverImage { get; set; } = "";
    public List<string> Themes { get; set; } = ["default"];
    public Dictionary<string, object> Extra { get; set; } = new();
}

/// <summary>
///     章节定义
/// </summary>
public class ChapterData
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Number { get; set; }
    public string Content { get; set; } = "";
    public string RawContent { get; set; } = "";
    public List<TocItem> TocItems { get; set; } = [];
    public ChapterData? Previous { get; set; }
    public ChapterData? Next { get; set; }
    public Dictionary<string, object> Extra { get; set; } = new();
}

/// <summary>
///     书籍生成结果
/// </summary>
public class BookResult
{
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";
    public int ChapterCount { get; init; }
    public string OutputDir { get; init; } = "";
    public List<string> GeneratedFiles { get; init; } = [];
}

#endregion

/// <summary>
///     电子书/手册生成器
/// </summary>
public sealed class BookGenerator
{
    private readonly List<ChapterData> _chapters = [];
    private readonly BookConfig _config = new();
    private readonly MarkdownLanguage _markdownLanguage;
    private readonly MarkdownRenderer _markdownRenderer;
    private readonly DejaVuRenderer _renderer;
    private readonly TemplateManager _templateManager;
    private string _sourceDir = "";

    public BookGenerator(string sourceDir)
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
    ///     生成电子书
    /// </summary>
    public BookResult Generate(string sourceDir, string outputDir)
    {
        _sourceDir = Path.GetFullPath(sourceDir);
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        LoadConfig();
        ScanChapters();
        BuildChapterNavigation();
        GenerateChapters(outputDir);
        GenerateTocPage(outputDir);
        GenerateCoverPage(outputDir);
        CopyAssets(outputDir);

        return new BookResult
        {
            Title = _config.Title,
            Author = _config.Author,
            ChapterCount = _chapters.Count,
            OutputDir = outputDir,
            GeneratedFiles = [.. Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories)]
        };
    }

    #region 配置

    private void LoadConfig()
    {
        var configPath = Path.Combine(_sourceDir, "book.yaml");
        if (!File.Exists(configPath)) return;

        var content = File.ReadAllText(configPath);
        var parser = new YamlParser();
        var result = parser.parse(content);
        if (!result.success || result.value is not YamlMapping mapping) return;

        _config.Title = DataConvert.GetYamlString(mapping, "title", _config.Title);
        _config.Author = DataConvert.GetYamlString(mapping, "author", _config.Author);
        _config.Language = DataConvert.GetYamlString(mapping, "language", _config.Language);
        _config.Description = DataConvert.GetYamlString(mapping, "description", _config.Description);
        _config.Isbn = DataConvert.GetYamlString(mapping, "isbn", _config.Isbn);
        _config.CoverImage = DataConvert.GetYamlString(mapping, "coverImage", _config.CoverImage);

        var dateStr = DataConvert.GetYamlString(mapping, "publishDate");
        if (DateTime.TryParse(dateStr, out var date)) _config.PublishDate = date;
    }

    #endregion

    #region 导航

    private void BuildChapterNavigation()
    {
        for (var i = 0; i < _chapters.Count; i++)
        {
            _chapters[i].Previous = i > 0 ? _chapters[i - 1] : null;
            _chapters[i].Next = i < _chapters.Count - 1 ? _chapters[i + 1] : null;
        }
    }

    #endregion

    #region 章节扫描

    private void ScanChapters()
    {
        _chapters.Clear();
        var chaptersDir = Path.Combine(_sourceDir, "chapters");
        if (!Directory.Exists(chaptersDir)) return;

        var files = Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();

        var number = 1;
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var chapter = new ChapterData { Number = number, Slug = GenerateSlug(file) };

            var (frontMatter, body) = FrontMatterParser.Extract(content);
            if (frontMatter != null)
            {
                var mapping = FrontMatterParser.Parse(frontMatter);
                if (mapping != null) ParseChapterFrontMatterFromMapping(mapping, chapter);
            }
            else
            {
                body = content;
            }

            if (string.IsNullOrEmpty(chapter.Title)) chapter.Title = $"第 {number} 章";

            chapter.RawContent = body;
            var markdownDocument = _markdownLanguage.parse(body);
            var mdResult = _markdownRenderer.Render(markdownDocument);
            chapter.Content = mdResult.Html;
            chapter.TocItems = mdResult.TocItems.ToList();

            _chapters.Add(chapter);
            number++;
        }
    }

    private void ParseChapterFrontMatterFromMapping(YamlMapping mapping, ChapterData chapter)
    {
        chapter.Title = DataConvert.GetYamlString(mapping, "title", chapter.Title);
        chapter.Slug = DataConvert.GetYamlString(mapping, "slug", chapter.Slug);
        chapter.Number = DataConvert.GetYamlInt(mapping, "number", chapter.Number);

        foreach (var (key, value) in mapping.properties)
            if (key is not ("title" or "slug" or "number"))
                chapter.Extra[key] = DataConvert.YamlValueToObject(value) ?? "";
    }

    private static string GenerateSlug(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var slug = new StringBuilder();
        var lastWasDash = false;

        foreach (var c in name)
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
                lastWasDash = false;
            }
            else if (c == ' ' || c == '-' || c == '_')
            {
                if (!lastWasDash && slug.Length > 0)
                {
                    slug.Append('-');
                    lastWasDash = true;
                }
            }

        return slug.ToString();
    }

    #endregion

    #region 页面生成

    private void GenerateChapters(string outputDir)
    {
        foreach (var chapter in _chapters)
        {
            var context = BuildChapterContext(chapter);

            var layoutPath = Path.Combine(_sourceDir, "layouts/chapter.dora");
            string rendered;

            if (File.Exists(layoutPath))
            {
                var layoutContent = File.ReadAllText(layoutPath);
                if (layoutContent.Contains("<% extends "))
                {
                    rendered = _renderer.Render(layoutContent, context);
                }
                else
                {
                    var pageTemplate =
                        $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{layoutContent}\n<% end block %>";
                    rendered = _renderer.Render(pageTemplate, context);
                }
            }
            else
            {
                var pageTemplate =
                    $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{chapter.Content}\n<% end block %>";
                rendered = _renderer.Render(pageTemplate, context);
            }

            var chapterDir = Path.Combine(outputDir, chapter.Slug);
            Directory.CreateDirectory(chapterDir);
            File.WriteAllText(Path.Combine(chapterDir, "index.html"), rendered);
        }
    }

    private void GenerateTocPage(string outputDir)
    {
        var context = BuildBookContext();
        context["chapters"] = _chapters.Select(c => new Dictionary<string, object>
        {
            ["title"] = c.Title,
            ["slug"] = c.Slug,
            ["number"] = c.Number,
            ["url"] = $"/{c.Slug}/"
        }).ToList();

        var tocLayoutPath = Path.Combine(_sourceDir, "layouts/toc.dora");
        string rendered;

        if (File.Exists(tocLayoutPath))
        {
            var layoutContent = File.ReadAllText(tocLayoutPath);
            if (layoutContent.Contains("<% extends "))
            {
                rendered = _renderer.Render(layoutContent, context);
            }
            else
            {
                var pageTemplate =
                    $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{layoutContent}\n<% end block %>";
                rendered = _renderer.Render(pageTemplate, context);
            }
        }
        else
        {
            var tocHtml = "<h1>" + _config.Title + "</h1>\n<ul>\n";
            foreach (var ch in _chapters) tocHtml += $"<li><a href=\"/{ch.Slug}/\">{ch.Title}</a></li>\n";
            tocHtml += "</ul>\n";

            var pageTemplate = $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{tocHtml}\n<% end block %>";
            rendered = _renderer.Render(pageTemplate, context);
        }

        File.WriteAllText(Path.Combine(outputDir, "index.html"), rendered);
    }

    private void GenerateCoverPage(string outputDir)
    {
        var context = BuildBookContext();
        var coverLayoutPath = Path.Combine(_sourceDir, "layouts/cover.dora");

        if (!File.Exists(coverLayoutPath)) return;

        var layoutContent = File.ReadAllText(coverLayoutPath);
        string rendered;
        if (layoutContent.Contains("<% extends "))
        {
            rendered = _renderer.Render(layoutContent, context);
        }
        else
        {
            var pageTemplate =
                $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{layoutContent}\n<% end block %>";
            rendered = _renderer.Render(pageTemplate, context);
        }

        File.WriteAllText(Path.Combine(outputDir, "cover.html"), rendered);
    }

    private void CopyAssets(string outputDir)
    {
        var assetsDir = Path.Combine(_sourceDir, "assets");
        if (!Directory.Exists(assetsDir)) return;

        foreach (var file in Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(assetsDir, file);
            var outputPath = Path.Combine(outputDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(file, outputPath, true);
        }
    }

    #endregion

    #region 上下文

    private Dictionary<string, object> BuildBookContext()
    {
        return new Dictionary<string, object>
        {
            ["book"] = new Dictionary<string, object>
            {
                ["title"] = _config.Title,
                ["author"] = _config.Author,
                ["language"] = _config.Language,
                ["description"] = _config.Description,
                ["isbn"] = _config.Isbn,
                ["publishDate"] = _config.PublishDate.ToString("yyyy-MM-dd"),
                ["coverImage"] = _config.CoverImage,
                ["chapterCount"] = _chapters.Count
            }
        };
    }

    private Dictionary<string, object> BuildChapterContext(ChapterData chapter)
    {
        var context = BuildBookContext();
        context["chapter"] = new Dictionary<string, object>
        {
            ["title"] = chapter.Title,
            ["slug"] = chapter.Slug,
            ["number"] = chapter.Number,
            ["content"] = chapter.Content,
            ["toc"] = chapter.TocItems.Select(t => new Dictionary<string, object>
            {
                ["level"] = t.level,
                ["text"] = t.text,
                ["id"] = t.id
            }).ToList()
        };

        if (chapter.Previous != null)
            ((Dictionary<string, object>)context["chapter"])["previous"] = new Dictionary<string, object>
            {
                ["title"] = chapter.Previous.Title,
                ["slug"] = chapter.Previous.Slug,
                ["url"] = $"/{chapter.Previous.Slug}/"
            };

        if (chapter.Next != null)
            ((Dictionary<string, object>)context["chapter"])["next"] = new Dictionary<string, object>
            {
                ["title"] = chapter.Next.Title,
                ["slug"] = chapter.Next.Slug,
                ["url"] = $"/{chapter.Next.Slug}/"
            };

        return context;
    }

    #endregion
}