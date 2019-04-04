namespace Std.Template.EBook;

/// <summary>
///     电子书导出格式
/// </summary>
public enum EBookFormat
{
    Epub,
    Pdf
}

/// <summary>
///     电子书统一生成器，编排 PDF/EPUB 导出管线
/// </summary>
public sealed class EBookGenerator
{
    private readonly EpubGenerator _epubGenerator;
    private readonly PdfGenerator _pdfGenerator;

    public EBookGenerator()
    {
        _epubGenerator = new EpubGenerator();
        _pdfGenerator = new PdfGenerator();
    }

    /// <summary>
    ///     从 HTML 目录导出电子书
    /// </summary>
    /// <param name="metadata">元数据</param>
    /// <param name="htmlOutputDir">HTML 输出目录（BookGenerator 或 SiteGenerator 的输出）</param>
    /// <param name="outputPath">输出文件路径（不含扩展名）</param>
    /// <param name="format">导出格式</param>
    /// <param name="pdfOptions">PDF 选项（仅 PDF 格式有效）</param>
    public async Task<EBookExportResult> ExportFromDirectoryAsync(
        EBookMetadata metadata,
        string htmlOutputDir,
        string outputPath,
        EBookFormat format,
        PdfOptions? pdfOptions = null)
    {
        return format switch
        {
            EBookFormat.Epub => _epubGenerator.GenerateFromDirectory(metadata, htmlOutputDir, outputPath),
            EBookFormat.Pdf => await _pdfGenerator.GenerateFromDirectoryAsync(metadata, htmlOutputDir, outputPath,
                pdfOptions),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>
    ///     从章节列表导出电子书
    /// </summary>
    /// <param name="metadata">元数据</param>
    /// <param name="chapters">章节列表</param>
    /// <param name="outputPath">输出文件路径（不含扩展名）</param>
    /// <param name="format">导出格式</param>
    /// <param name="pdfOptions">PDF 选项（仅 PDF 格式有效）</param>
    public async Task<EBookExportResult> ExportAsync(
        EBookMetadata metadata,
        List<EBookChapter> chapters,
        string outputPath,
        EBookFormat format,
        PdfOptions? pdfOptions = null)
    {
        return format switch
        {
            EBookFormat.Epub => _epubGenerator.Generate(metadata, chapters, outputPath),
            EBookFormat.Pdf => await _pdfGenerator.GenerateAsync(metadata, chapters, outputPath, pdfOptions),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
}