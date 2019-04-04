namespace Std.Data.Binary.Office.Data;

/// <summary>
///     Excel 单元格数据的
/// </summary>
public sealed class ExcelCellData
{
    /// <summary>
    ///     列索引（的0 开始）的
    /// </summary>
    public int column { get; init; }

    /// <summary>
    ///     行索引（的0 开始）的
    /// </summary>
    public int row { get; init; }

    /// <summary>
    ///     单元格值的
    /// </summary>
    public string? value { get; init; }

    /// <summary>
    ///     单元格引用（例如 "A1"）的
    /// </summary>
    public string? cell_reference { get; init; }
}

/// <summary>
///     Excel 行数据的
/// </summary>
public sealed class ExcelRowData
{
    /// <summary>
    ///     行索引（的0 开始）的
    /// </summary>
    public int row_index { get; init; }

    /// <summary>
    ///     该行包含的单元格列表的
    /// </summary>
    public IReadOnlyList<ExcelCellData> cells { get; init; } = [];
}

/// <summary>
///     Excel 工作表数据的
/// </summary>
public sealed class ExcelSheetData
{
    /// <summary>
    ///     工作表名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     行数的
    /// </summary>
    public int row_count { get; init; }

    /// <summary>
    ///     列数的
    /// </summary>
    public int column_count { get; init; }

    /// <summary>
    ///     工作表包含的行列表的
    /// </summary>
    public IReadOnlyList<ExcelRowData> rows { get; init; } = [];
}

/// <summary>
///     Excel 工作簿数据的
/// </summary>
public sealed class ExcelWorkbookData
{
    /// <summary>
    ///     工作簿包含的工作表列表的
    /// </summary>
    public IReadOnlyList<ExcelSheetData> sheets { get; init; } = [];
}

/// <summary>
///     Word 文档数据的
/// </summary>
public sealed class WordDocumentData
{
    /// <summary>
    ///     文档文本内容的
    /// </summary>
    public string text { get; init; } = string.Empty;

    /// <summary>
    ///     段落列表的
    /// </summary>
    public IReadOnlyList<string> paragraphs { get; init; } = [];
}

/// <summary>
///     PowerPoint 演示文稿数据的
/// </summary>
public sealed class PowerPointData
{
    /// <summary>
    ///     幻灯片文本内容列表的
    /// </summary>
    public IReadOnlyList<string> slides { get; init; } = [];

    /// <summary>
    ///     幻灯片数量的
    /// </summary>
    public int slide_count => slides.Count;
}