using System.Text;
using System.Text.RegularExpressions;

namespace Std.Template.EBook;

/// <summary>
///     PDF 导出器，使用 PuppeteerSharp 将 HTML 转换为 PDF
/// </summary>
public sealed class PdfGenerator
{
    #region 默认 CSS

    private const string DefaultCss = @"
@page {
    size: A4;
    margin: 2cm 2.5cm;
    @bottom-center {
        content: counter(page);
        font-size: 0.8em;
        color: #999;
    }
}
body {
    font-family: 'Noto Serif CJK SC', 'SimSun', serif;
    font-size: 12pt;
    line-height: 1.8;
    color: #333;
}
.book-cover {
    text-align: center;
    padding: 6cm 0;
    page-break-after: always;
}
.book-title { font-size: 28pt; margin-bottom: 1cm; }
.book-author { font-size: 16pt; color: #666; }
.book-date { font-size: 12pt; color: #999; }
.book-toc {
    page-break-after: always;
}
.book-toc h2 { font-size: 18pt; }
.book-toc ol { padding-left: 1.5em; }
.book-toc li { margin: 0.4em 0; }
.chapter h1 { font-size: 20pt; margin-bottom: 0.8em; }
.chapter h2 { font-size: 16pt; margin-top: 1.5em; }
.chapter h3 { font-size: 14pt; }
.chapter p { text-indent: 2em; margin: 0.5em 0; }
.chapter pre, .chapter code {
    font-family: 'Cascadia Code', 'Consolas', monospace;
    font-size: 9pt;
}
.chapter pre {
    background: #f5f5f5;
    padding: 0.8em;
    border-radius: 3px;
    text-indent: 0;
}
.chapter code {
    background: #f5f5f5;
    padding: 0.1em 0.3em;
    text-indent: 0;
}
.chapter pre code {
    background: none;
    padding: 0;
}
.chapter blockquote {
    border-left: 3px solid #ccc;
    margin: 1em 0;
    padding: 0.5em 1em;
    color: #666;
    text-indent: 0;
}
.chapter table {
    border-collapse: collapse;
    width: 100%;
    margin: 1em 0;
    text-indent: 0;
}
.chapter th, .chapter td {
    border: 1px solid #ccc;
    padding: 0.4em 0.6em;
    text-align: left;
}
.chapter th { background: #f0f0f0; }
.chapter img { max-width: 100%; }
";

    #endregion

    /// <summary>
    ///     从 HTML 输出目录生成 PDF
    /// </summary>
    public async Task<EBookExportResult> GenerateFromDirectoryAsync(
        EBookMetadata metadata,
        string htmlOutputDir,
        string outputPath,
        PdfOptions? options = null)
    {
        options ??= new PdfOptions();

        var chapters = CollectChapterFiles(htmlOutputDir);

        var mergedHtml = MergeChaptersToHtml(metadata, chapters, options.IncludeCss);

        var outputFile = Path.ChangeExtension(outputPath, ".pdf");
        var dir = Path.GetDirectoryName(outputFile)!;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await RenderPdfAsync(mergedHtml, outputFile, options);

        return new EBookExportResult
        {
            OutputPath = outputFile,
            Format = "pdf",
            ChapterCount = chapters.Count,
            FileSizeBytes = new FileInfo(outputFile).Length
        };
    }

    /// <summary>
    ///     从章节列表生成 PDF
    /// </summary>
    public async Task<EBookExportResult> GenerateAsync(
        EBookMetadata metadata,
        List<EBookChapter> chapters,
        string outputPath,
        PdfOptions? options = null)
    {
        options ??= new PdfOptions();

        var mergedHtml = MergeChaptersToHtmlFromList(metadata, chapters, options.IncludeCss);

        var outputFile = Path.ChangeExtension(outputPath, ".pdf");
        var dir = Path.GetDirectoryName(outputFile)!;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await RenderPdfAsync(mergedHtml, outputFile, options);

        return new EBookExportResult
        {
            OutputPath = outputFile,
            Format = "pdf",
            ChapterCount = chapters.Count,
            FileSizeBytes = new FileInfo(outputFile).Length
        };
    }

    #region HTML 合并

    private static string MergeChaptersToHtml(
        EBookMetadata metadata,
        List<ChapterFileInfo> chapters,
        bool includeCss)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html lang=\"{metadata.Language}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine($"<title>{HtmlUtility.EscapeHtml(metadata.Title)}</title>");

        if (includeCss)
        {
            sb.AppendLine("<style>");
            sb.AppendLine(DefaultCss);
            sb.AppendLine("</style>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class=\"book-cover\">");
        sb.AppendLine($"<h1 class=\"book-title\">{HtmlUtility.EscapeHtml(metadata.Title)}</h1>");
        sb.AppendLine($"<p class=\"book-author\">{HtmlUtility.EscapeHtml(metadata.Author)}</p>");
        sb.AppendLine($"<p class=\"book-date\">{metadata.PublishDate:yyyy-MM-dd}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"book-toc\">");
        sb.AppendLine("<h2>目录</h2>");
        sb.AppendLine("<ol>");

        var chapterNumber = 1;
        foreach (var file in chapters)
        {
            sb.AppendLine($"<li><a href=\"#chapter-{chapterNumber}\">{HtmlUtility.EscapeHtml(file.Title)}</a></li>");
            chapterNumber++;
        }

        sb.AppendLine("</ol>");
        sb.AppendLine("</div>");

        chapterNumber = 1;
        foreach (var file in chapters)
        {
            sb.AppendLine($"<div class=\"chapter\" id=\"chapter-{chapterNumber}\">");

            var content = file.Content;

            content = Regex.Replace(
                content,
                @"<html[^>]*>.*?</html>",
                m => ExtractBody(m.Value),
                RegexOptions.Singleline);

            sb.AppendLine(content);
            sb.AppendLine("</div>");
            sb.AppendLine("<div style=\"page-break-after: always;\"></div>");

            chapterNumber++;
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string MergeChaptersToHtmlFromList(
        EBookMetadata metadata,
        List<EBookChapter> chapters,
        bool includeCss)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html lang=\"{metadata.Language}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine($"<title>{HtmlUtility.EscapeHtml(metadata.Title)}</title>");

        if (includeCss)
        {
            sb.AppendLine("<style>");
            sb.AppendLine(DefaultCss);
            sb.AppendLine("</style>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class=\"book-cover\">");
        sb.AppendLine($"<h1 class=\"book-title\">{HtmlUtility.EscapeHtml(metadata.Title)}</h1>");
        sb.AppendLine($"<p class=\"book-author\">{HtmlUtility.EscapeHtml(metadata.Author)}</p>");
        sb.AppendLine($"<p class=\"book-date\">{metadata.PublishDate:yyyy-MM-dd}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"book-toc\">");
        sb.AppendLine("<h2>目录</h2>");
        sb.AppendLine("<ol>");

        var chapterNumber = 1;
        foreach (var chapter in chapters)
        {
            sb.AppendLine($"<li><a href=\"#chapter-{chapterNumber}\">{HtmlUtility.EscapeHtml(chapter.Title)}</a></li>");
            chapterNumber++;
        }

        sb.AppendLine("</ol>");
        sb.AppendLine("</div>");

        chapterNumber = 1;
        foreach (var chapter in chapters)
        {
            sb.AppendLine($"<div class=\"chapter\" id=\"chapter-{chapterNumber}\">");
            sb.AppendLine(chapter.HtmlContent);
            sb.AppendLine("</div>");
            sb.AppendLine("<div style=\"page-break-after: always;\"></div>");
            chapterNumber++;
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string ExtractBody(string html)
    {
        var bodyStart = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (bodyStart < 0) return html;

        var bodyTagEnd = html.IndexOf('>', bodyStart);
        var bodyEnd = html.IndexOf("</body>", bodyTagEnd, StringComparison.OrdinalIgnoreCase);
        if (bodyEnd < 0) return html[(bodyTagEnd + 1)..];

        return html[(bodyTagEnd + 1)..bodyEnd];
    }

    private class ChapterFileInfo
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private static List<ChapterFileInfo> CollectChapterFiles(string htmlOutputDir)
    {
        var chapters = new List<ChapterFileInfo>();
        var indexFile = Path.Combine(htmlOutputDir, "index.html");

        if (File.Exists(indexFile))
        {
            var content = File.ReadAllText(indexFile);
            chapters.Add(new ChapterFileInfo
            {
                Title = ExtractTitleFromHtml(content),
                Content = content
            });
        }

        var dirs = Directory.GetDirectories(htmlOutputDir);
        foreach (var dir in dirs.OrderBy(d => d))
        {
            var chapterIndex = Path.Combine(dir, "index.html");
            if (File.Exists(chapterIndex))
            {
                var content = File.ReadAllText(chapterIndex);
                chapters.Add(new ChapterFileInfo
                {
                    Title = ExtractTitleFromHtml(content),
                    Content = content
                });
            }
        }

        return chapters;
    }

    private static string ExtractTitleFromHtml(string html)
    {
        var titleStart = html.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
        if (titleStart >= 0)
        {
            var titleClose = html.IndexOf('>', titleStart);
            var titleEnd = html.IndexOf("</title>", titleClose, StringComparison.OrdinalIgnoreCase);
            if (titleEnd > titleClose)
                return Regex.Replace(
                    html[(titleClose + 1)..titleEnd], @"<[^>]+>", "").Trim();
        }

        var h1Start = html.IndexOf("<h1", StringComparison.OrdinalIgnoreCase);
        if (h1Start >= 0)
        {
            var h1Close = html.IndexOf('>', h1Start);
            var h1End = html.IndexOf("</h1>", h1Close, StringComparison.OrdinalIgnoreCase);
            if (h1End > h1Close)
                return Regex.Replace(
                    html[(h1Close + 1)..h1End], @"<[^>]+>", "").Trim();
        }

        return "";
    }

    #endregion

    #region PuppeteerSharp 渲染

    private static async Task RenderPdfAsync(string html, string outputPath, PdfOptions options)
    {
        var browser = await GetBrowserAsync();

        try
        {
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            var pdfOptions = new PuppeteerSharp.PdfOptions
            {
                Format = options.PageSize switch
                {
                    PageSize.A4 => PaperFormat.A4,
                    PageSize.A5 => PaperFormat.A5,
                    PageSize.Letter => PaperFormat.Letter,
                    _ => PaperFormat.A4
                },
                DisplayHeaderFooter = options.DisplayHeaderFooter,
                HeaderTemplate = options.HeaderTemplate,
                FooterTemplate = options.FooterTemplate,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = options.MarginTop,
                    Right = options.MarginRight,
                    Bottom = options.MarginBottom,
                    Left = options.MarginLeft
                },
                Landscape = options.Landscape,
                Scale = options.Scale
            };

            var pdfData = await page.PdfDataAsync(pdfOptions);
            await File.WriteAllBytesAsync(outputPath, pdfData);
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    private static async Task<IBrowser> GetBrowserAsync()
    {
        var browserFetcher = new BrowserFetcher();
        if (!browserFetcher.GetInstalledBrowsers().Any()) await browserFetcher.DownloadAsync();

        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu"
            }
        });
    }

    #endregion
}

/// <summary>
///     PDF 导出选项
/// </summary>
public class PdfOptions
{
    /// <summary>
    ///     页面尺寸
    /// </summary>
    public PageSize PageSize { get; set; } = PageSize.A4;

    /// <summary>
    ///     是否注入默认 CSS
    /// </summary>
    public bool IncludeCss { get; set; } = true;

    /// <summary>
    ///     是否显示页眉页脚
    /// </summary>
    public bool DisplayHeaderFooter { get; set; } = true;

    /// <summary>
    ///     页眉 HTML 模板
    /// </summary>
    public string? HeaderTemplate { get; set; }

    /// <summary>
    ///     页脚 HTML 模板
    /// </summary>
    public string? FooterTemplate { get; set; }

    /// <summary>
    ///     上边距
    /// </summary>
    public string MarginTop { get; set; } = "2cm";

    /// <summary>
    ///     右边距
    /// </summary>
    public string MarginRight { get; set; } = "2.5cm";

    /// <summary>
    ///     下边距
    /// </summary>
    public string MarginBottom { get; set; } = "2cm";

    /// <summary>
    ///     左边距
    /// </summary>
    public string MarginLeft { get; set; } = "2.5cm";

    /// <summary>
    ///     横向打印
    /// </summary>
    public bool Landscape { get; set; }

    /// <summary>
    ///     缩放比例（1.0 = 100%）
    /// </summary>
    public decimal Scale { get; set; } = 1.0m;
}

/// <summary>
///     页面尺寸
/// </summary>
public enum PageSize
{
    A4,
    A5,
    Letter
}