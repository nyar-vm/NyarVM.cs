using System.Text.RegularExpressions;

namespace Std.Template.EBook;

#region 数据模型

/// <summary>
///     电子书章节
/// </summary>
public class EBookChapter
{
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string HtmlContent { get; set; } = "";
    public int Order { get; set; }
    public List<EBookChapter> Children { get; set; } = [];
}

/// <summary>
///     电子书元数据
/// </summary>
public class EBookMetadata
{
    public string Title { get; set; } = "Untitled";
    public string Author { get; set; } = "Anonymous";
    public string Language { get; set; } = "zh-CN";
    public string Description { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Rights { get; set; } = "";
    public DateTime PublishDate { get; set; } = DateTime.Now;
    public string Identifier { get; set; } = Guid.NewGuid().ToString("D");
    public string CoverImagePath { get; set; } = "";
    public Dictionary<string, string> ExtraMeta { get; set; } = new();
}

/// <summary>
///     电子书导出结果
/// </summary>
public class EBookExportResult
{
    public string OutputPath { get; set; } = "";
    public string Format { get; set; } = "";
    public int ChapterCount { get; set; }
    public long FileSizeBytes { get; set; }
}

#endregion

/// <summary>
///     EPUB 3.0 电子书生成器，委托 Oak.EPub.EpubEncoder 完成 EPUB 编码
/// </summary>
public sealed class EpubGenerator
{
    private readonly EpubEncoder _encoder = new();

    /// <summary>
    ///     从 HTML 输出目录生成 EPUB
    /// </summary>
    public EBookExportResult GenerateFromDirectory(
        EBookMetadata metadata,
        string htmlOutputDir,
        string outputPath)
    {
        var chapters = ScanHtmlDirectory(htmlOutputDir);
        return Generate(metadata, chapters, outputPath);
    }

    /// <summary>
    ///     生成 EPUB 文件
    /// </summary>
    public EBookExportResult Generate(
        EBookMetadata metadata,
        List<EBookChapter> chapters,
        string outputPath)
    {
        var epubMetadata = ConvertMetadata(metadata);
        var flatChapters = FlattenChapters(chapters);
        var epubChapters = flatChapters.ConvertAll(ConvertChapter);
        var result = _encoder.Generate(epubMetadata, epubChapters, outputPath);
        return ConvertResult(result);
    }

    #region 数据转换

    private static EpubMetadata ConvertMetadata(EBookMetadata m)
    {
        return new EpubMetadata
        {
            Title = m.Title,
            Author = m.Author,
            Language = m.Language,
            Description = m.Description,
            Publisher = m.Publisher,
            Rights = m.Rights,
            PublishDate = m.PublishDate,
            Identifier = m.Identifier,
            CoverImagePath = m.CoverImagePath,
            ExtraMeta = new Dictionary<string, string>(m.ExtraMeta)
        };
    }

    private static EpubChapter ConvertChapter(EBookChapter c)
    {
        return new EpubChapter
        {
            Title = c.Title,
            FileName = c.FileName,
            HtmlContent = c.HtmlContent,
            Order = c.Order
        };
    }

    private static EBookExportResult ConvertResult(EpubExportResult r)
    {
        return new EBookExportResult
        {
            OutputPath = r.OutputPath,
            Format = r.Format,
            ChapterCount = r.ChapterCount,
            FileSizeBytes = r.FileSizeBytes
        };
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     扁平化章节列表，含子章节展开
    /// </summary>
    private static List<EBookChapter> FlattenChapters(List<EBookChapter> chapters)
    {
        var flat = new List<EBookChapter>();
        var order = 1;

        foreach (var chapter in chapters.OrderBy(c => c.Order))
        {
            chapter.Order = order++;
            chapter.FileName = $"chapter-{chapter.Order:D3}.xhtml";
            flat.Add(chapter);

            foreach (var child in chapter.Children)
            {
                child.Order = order++;
                child.FileName = $"chapter-{child.Order:D3}.xhtml";
                flat.Add(child);
            }
        }

        return flat;
    }

    /// <summary>
    ///     扫描 HTML 目录，收集章节文件
    /// </summary>
    private List<EBookChapter> ScanHtmlDirectory(string htmlOutputDir)
    {
        var chapters = new List<EBookChapter>();
        var indexFile = Path.Combine(htmlOutputDir, "index.html");

        if (File.Exists(indexFile))
        {
            var content = File.ReadAllText(indexFile);
            var title = ExtractTitle(content);
            chapters.Add(new EBookChapter
            {
                Title = title,
                HtmlContent = content,
                Order = 0
            });
        }

        var dirs = Directory.GetDirectories(htmlOutputDir);
        var order = 1;

        foreach (var dir in dirs.OrderBy(d => d))
        {
            var chapterIndex = Path.Combine(dir, "index.html");
            if (!File.Exists(chapterIndex)) continue;

            var content = File.ReadAllText(chapterIndex);
            var title = ExtractTitle(content);
            var dirName = Path.GetFileName(dir);

            chapters.Add(new EBookChapter
            {
                Title = title,
                HtmlContent = content,
                Order = string.IsNullOrEmpty(title) ? order : order,
                FileName = dirName
            });

            order++;
        }

        return chapters;
    }

    private static string ExtractTitle(string html)
    {
        var titleStart = html.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
        if (titleStart < 0)
        {
            titleStart = html.IndexOf("<h1", StringComparison.OrdinalIgnoreCase);
            if (titleStart < 0) return "";

            var h1Close = html.IndexOf('>', titleStart);
            var h1End = html.IndexOf("</h1>", h1Close, StringComparison.OrdinalIgnoreCase);
            if (h1End < 0) return "";

            return StripHtml(html[(h1Close + 1)..h1End]);
        }

        var titleClose = html.IndexOf('>', titleStart);
        var titleEnd = html.IndexOf("</title>", titleClose, StringComparison.OrdinalIgnoreCase);
        if (titleEnd < 0) return "";

        return StripHtml(html[(titleClose + 1)..titleEnd]);
    }

    private static string StripHtml(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", "").Trim();
    }

    /// <summary>
    ///     清洗 HTML 使其符合 XHTML 标准（EPUB 要求），委托给 EpubEncoder
    /// </summary>
    public static string SanitizeHtmlForEpub(string html)
    {
        return EpubEncoder.SanitizeHtmlForEpub(html);
    }

    /// <summary>
    ///     XML 转义，委托给 EpubEncoder
    /// </summary>
    public static string EscapeXml(string text)
    {
        return EpubEncoder.EscapeXml(text);
    }

    #endregion
}