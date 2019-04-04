using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Oak.EPub;

/// <summary>
///     EPUB 3.0 电子书编码器（纯 C# 实现）
/// </summary>
public sealed class EpubEncoder
{
    private const string NsOpf = "http://www.idpf.org/2007/opf";
    private const string NsDc = "http://purl.org/dc/elements/1.1/";
    private const string NsXhtml = "http://www.w3.org/1999/xhtml";
    private const string NsEpub = "http://www.idpf.org/2007/ops";

    /// <summary>
    ///     生成 EPUB 文件
    /// </summary>
    /// <param name="metadata">元数据</param>
    /// <param name="chapters">章节列表（已扁平化，含文件名）</param>
    /// <param name="outputPath">输出文件路径（不含扩展名）</param>
    /// <returns>导出结果</returns>
    public EpubExportResult Generate(
        EpubMetadata metadata,
        List<EpubChapter> chapters,
        string outputPath)
    {
        var outputFile = Path.ChangeExtension(outputPath, ".epub");
        var dir = Path.GetDirectoryName(outputFile)!;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        using var zipStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        WriteMimetype(archive);

        var packageId = "package-" + Guid.NewGuid().ToString("N")[..8];

        WriteContainerXml(archive, packageId);
        WriteContentOpf(archive, metadata, chapters, packageId);
        WriteNavXhtml(archive, metadata, chapters);
        WriteChapterFiles(archive, chapters);
        WriteDefaultCss(archive);

        if (!string.IsNullOrEmpty(metadata.CoverImagePath) && File.Exists(metadata.CoverImagePath))
            WriteCoverImage(archive, metadata.CoverImagePath);

        return new EpubExportResult
        {
            OutputPath = outputFile,
            Format = "epub",
            ChapterCount = chapters.Count,
            FileSizeBytes = new FileInfo(outputFile).Length
        };
    }

    #region EPUB 结构写入

    private static void WriteMimetype(ZipArchive archive)
    {
        var entry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), Encoding.ASCII);
        writer.Write("application/epub+zip");
    }

    private static void WriteContainerXml(ZipArchive archive, string packageId)
    {
        var containerXml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(XName.Get("container", "urn:oasis:names:tc:opendocument:xmlns:container"),
                new XAttribute("version", "1.0"),
                new XElement(XName.Get("rootfiles", "urn:oasis:names:tc:opendocument:xmlns:container"),
                    new XElement(XName.Get("rootfile", "urn:oasis:names:tc:opendocument:xmlns:container"),
                        new XAttribute("full-path", $"OEBPS/content_{packageId}.opf"),
                        new XAttribute("media-type", "application/oebps-package+xml")
                    )
                )
            )
        );

        var entry = archive.CreateEntry("META-INF/container.xml", CompressionLevel.Optimal);
        using var stream = entry.Open();
        containerXml.Save(stream);
    }

    private static void WriteContentOpf(
        ZipArchive archive,
        EpubMetadata metadata,
        List<EpubChapter> chapters,
        string packageId)
    {
        var nsOpf = XNamespace.Get(NsOpf);
        var nsDc = XNamespace.Get(NsDc);

        var metadataElement = new XElement(nsOpf + "metadata",
            new XAttribute(XNamespace.Xmlns + "dc", NsDc),
            new XElement(nsDc + "title", metadata.Title),
            new XElement(nsDc + "creator", metadata.Author),
            new XAttribute(nsOpf + "role", "aut"),
            new XElement(nsDc + "language", metadata.Language),
            new XElement(nsDc + "identifier",
                new XAttribute("id", "book-id"),
                metadata.Identifier),
            new XElement(nsDc + "date", metadata.PublishDate.ToString("yyyy-MM-dd")),
            new XElement(nsDc + "publisher", metadata.Publisher),
            new XElement(nsDc + "rights", metadata.Rights)
        );

        if (!string.IsNullOrEmpty(metadata.Description))
            metadataElement.Add(new XElement(nsDc + "description", metadata.Description));

        if (!string.IsNullOrEmpty(metadata.CoverImagePath))
            metadataElement.Add(new XElement(nsOpf + "meta",
                new XAttribute("name", "cover"),
                new XAttribute("content", "cover-image")));

        var manifest = new XElement(nsOpf + "manifest");
        var spine = new XElement(nsOpf + "spine");

        manifest.Add(new XElement(nsOpf + "item",
            new XAttribute("id", "nav"),
            new XAttribute("href", "nav.xhtml"),
            new XAttribute("media-type", "application/xhtml+xml"),
            new XAttribute("properties", "nav")));

        var navId = "nav";
        spine.Add(new XElement(nsOpf + "itemref",
            new XAttribute("idref", navId),
            new XAttribute("linear", "no")));

        manifest.Add(new XElement(nsOpf + "item",
            new XAttribute("id", "css-default"),
            new XAttribute("href", "styles/style.css"),
            new XAttribute("media-type", "text/css")));

        if (!string.IsNullOrEmpty(metadata.CoverImagePath))
        {
            var coverExt = Path.GetExtension(metadata.CoverImagePath).ToLowerInvariant();
            var coverMime = coverExt switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "image/jpeg"
            };

            manifest.Add(new XElement(nsOpf + "item",
                new XAttribute("id", "cover-image"),
                new XAttribute("href", "images/cover" + coverExt),
                new XAttribute("media-type", coverMime),
                new XAttribute("properties", "cover-image")));
        }

        foreach (var chapter in chapters)
        {
            var itemId = $"chapter-{chapter.Order}";
            manifest.Add(new XElement(nsOpf + "item",
                new XAttribute("id", itemId),
                new XAttribute("href", chapter.FileName),
                new XAttribute("media-type", "application/xhtml+xml")));
            spine.Add(new XElement(nsOpf + "itemref",
                new XAttribute("idref", itemId)));
        }

        var package = new XElement(nsOpf + "package",
            new XAttribute("version", "3.0"),
            new XAttribute("unique-identifier", "book-id"),
            metadataElement,
            manifest,
            spine
        );

        var opfEntry = archive.CreateEntry($"OEBPS/content_{packageId}.opf", CompressionLevel.Optimal);
        using var stream = opfEntry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        writer.Write(package.ToString());
    }

    private static void WriteNavXhtml(
        ZipArchive archive,
        EpubMetadata metadata,
        List<EpubChapter> chapters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html xmlns=\"{NsXhtml}\" xmlns:epub=\"{NsEpub}\" xml:lang=\"{metadata.Language}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine($"<title>{EscapeXml(metadata.Title)} - 目录</title>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<nav epub:type=\"toc\" id=\"toc\">");
        sb.AppendLine("<h1>目录</h1>");
        sb.AppendLine("<ol>");

        foreach (var chapter in chapters)
            sb.AppendLine($"<li><a href=\"{chapter.FileName}\">{EscapeXml(chapter.Title)}</a></li>");

        sb.AppendLine("</ol>");
        sb.AppendLine("</nav>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var entry = archive.CreateEntry("OEBPS/nav.xhtml", CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(sb.ToString());
    }

    private static void WriteChapterFiles(ZipArchive archive, List<EpubChapter> chapters)
    {
        foreach (var chapter in chapters)
        {
            var xhtml = WrapChapterAsXhtml(chapter);
            var entry = archive.CreateEntry($"OEBPS/{chapter.FileName}", CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(xhtml);
        }
    }

    private static string WrapChapterAsXhtml(EpubChapter chapter)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html xmlns=\"{NsXhtml}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine($"<title>{EscapeXml(chapter.Title)}</title>");
        sb.AppendLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"styles/style.css\" />");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{EscapeXml(chapter.Title)}</h1>");
        sb.AppendLine(SanitizeHtmlForEpub(chapter.HtmlContent));
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void WriteDefaultCss(ZipArchive archive)
    {
        var css = @"
body {
    font-family: serif;
    margin: 5%;
    line-height: 1.6;
    color: #333;
}
h1 { font-size: 1.8em; margin-bottom: 0.8em; }
h2 { font-size: 1.4em; margin-top: 1.5em; }
h3 { font-size: 1.2em; }
p { margin: 0.8em 0; }
pre, code {
    font-family: monospace;
    background: #f5f5f5;
    border-radius: 3px;
}
pre {
    padding: 0.8em;
    overflow-x: auto;
    font-size: 0.9em;
}
code { padding: 0.1em 0.3em; font-size: 0.9em; }
pre code { background: none; padding: 0; }
blockquote {
    border-left: 3px solid #ccc;
    margin-left: 0;
    padding-left: 1em;
    color: #666;
}
table {
    border-collapse: collapse;
    width: 100%;
    margin: 1em 0;
}
th, td {
    border: 1px solid #ccc;
    padding: 0.5em;
    text-align: left;
}
th { background: #f0f0f0; }
img { max-width: 100%; height: auto; }
";

        var entry = archive.CreateEntry("OEBPS/styles/style.css", CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(css);
    }

    private static void WriteCoverImage(ZipArchive archive, string coverImagePath)
    {
        var ext = Path.GetExtension(coverImagePath).ToLowerInvariant();
        var entry = archive.CreateEntry($"OEBPS/images/cover{ext}", CompressionLevel.NoCompression);
        using var source = File.OpenRead(coverImagePath);
        using var target = entry.Open();
        source.CopyTo(target);
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     清洗 HTML 使其符合 XHTML 标准（EPUB 要求）
    /// </summary>
    public static string SanitizeHtmlForEpub(string html)
    {
        return html
            .Replace("<br>", "<br/>")
            .Replace("<hr>", "<hr/>");
    }

    /// <summary>
    ///     XML 转义
    /// </summary>
    public static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    #endregion
}