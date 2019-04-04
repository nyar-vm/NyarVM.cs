namespace Oak.EPub;

/// <summary>
///     EPUB 章节
/// </summary>
public class EpubChapter
{
    /// <summary>
    ///     章节标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    ///     文件名（如 chapter-001.xhtml）
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    ///     章节 HTML 内容
    /// </summary>
    public string HtmlContent { get; set; } = "";

    /// <summary>
    ///     排序序号
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
///     EPUB 元数据
/// </summary>
public class EpubMetadata
{
    /// <summary>
    ///     书名
    /// </summary>
    public string Title { get; set; } = "Untitled";

    /// <summary>
    ///     作者
    /// </summary>
    public string Author { get; set; } = "Anonymous";

    /// <summary>
    ///     语言
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    ///     描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    ///     出版社
    /// </summary>
    public string Publisher { get; set; } = "";

    /// <summary>
    ///     版权信息
    /// </summary>
    public string Rights { get; set; } = "";

    /// <summary>
    ///     出版日期
    /// </summary>
    public DateTime PublishDate { get; set; } = DateTime.Now;

    /// <summary>
    ///     唯一标识符
    /// </summary>
    public string Identifier { get; set; } = Guid.NewGuid().ToString("D");

    /// <summary>
    ///     封面图片路径
    /// </summary>
    public string CoverImagePath { get; set; } = "";

    /// <summary>
    ///     额外元数据
    /// </summary>
    public Dictionary<string, string> ExtraMeta { get; set; } = new();
}

/// <summary>
///     EPUB 导出结果
/// </summary>
public class EpubExportResult
{
    /// <summary>
    ///     输出文件路径
    /// </summary>
    public string OutputPath { get; set; } = "";

    /// <summary>
    ///     格式
    /// </summary>
    public string Format { get; set; } = "";

    /// <summary>
    ///     章节数量
    /// </summary>
    public int ChapterCount { get; set; }

    /// <summary>
    ///     文件大小（字节）
    /// </summary>
    public long FileSizeBytes { get; set; }
}