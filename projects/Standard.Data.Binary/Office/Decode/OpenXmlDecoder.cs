using System.Text;
using Std.Data.Binary.Office.Data;
using Std.Data.Binary.Zip.Decode;

namespace Std.Data.Binary.Office.Decode;

/// <summary>
///     Open XML 格式解码器，解析 Office Open XML 格式的xlsx, .docx, .pptx）的
/// </summary>
/// <remarks>
///     Open XML 格式本质上是 ZIP 压缩包，内部包含 XML 文件的
///     本解码器使用 Nyar.Binary.Zip 解析 ZIP 结构，提取其中的 XML 内容的
/// </remarks>
public sealed class OpenXmlDecoder
{
    private readonly ZipDecoder _zip_decoder;

    /// <summary>
    ///     初始的<see cref="OpenXmlDecoder" /> 类的新实例的
    /// </summary>
    public OpenXmlDecoder()
    {
        _zip_decoder = new ZipDecoder();
    }

    /// <summary>
    ///     的Open XML 二进制数据解码工作簿的xlsx）的
    /// </summary>
    /// <param name="data">
    ///     Open XML 二进制数据的/param>
    ///     <returns>解码后的工作簿数据的/returns>
    public ExcelWorkbookData decode_excel(byte[] data)
    {
        var zipFile = _zip_decoder.decode(data);

        var sheets = new List<ExcelSheetData>();

        // 查找工作簿文的
        var workbookEntry = zipFile.entries.FirstOrDefault(e => e.name == "xl/workbook.xml");
        if (workbookEntry != null)
        {
            // 提取工作表名的
            var sheetNames = extract_sheet_names(workbookEntry.data);

            foreach (var sheetName in sheetNames)
            {
                // 查找对应的工作表文件
                var sheetPath = $"xl/worksheets/{sheetName}.xml";
                var sheetEntry = zipFile.entries.FirstOrDefault(e => e.name == sheetPath);

                if (sheetEntry != null)
                {
                    var rows = extract_sheet_data(sheetEntry.data);
                    sheets.Add(new ExcelSheetData
                    {
                        name = sheetName,
                        row_count = rows.Count,
                        column_count = rows.Count > 0 ? rows.Max(r => r.cells.Count) : 0,
                        rows = rows
                    });
                }
            }
        }

        return new ExcelWorkbookData
        {
            sheets = sheets
        };
    }

    /// <summary>
    ///     的Open XML 二进制数据解码文档（.docx）的
    /// </summary>
    /// <param name="data">
    ///     Open XML 二进制数据的/param>
    ///     <returns>解码后的文档数据的/returns>
    public WordDocumentData decode_word(byte[] data)
    {
        var zipFile = _zip_decoder.decode(data);

        // 查找文档内容文件
        var documentEntry = zipFile.entries.FirstOrDefault(e => e.name == "word/document.xml");
        if (documentEntry != null)
        {
            var text = extract_text_from_xml(documentEntry.data);
            var paragraphs = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries);

            return new WordDocumentData
            {
                text = text,
                paragraphs = paragraphs
            };
        }

        return new WordDocumentData { text = string.Empty, paragraphs = [] };
    }

    /// <summary>
    ///     的Open XML 二进制数据解码演示文稿（.pptx）的
    /// </summary>
    /// <param name="data">
    ///     Open XML 二进制数据的/param>
    ///     <returns>解码后的演示文稿数据的/returns>
    public PowerPointData decode_power_point(byte[] data)
    {
        var zipFile = _zip_decoder.decode(data);

        var slides = new List<string>();

        // 查找所有幻灯片文件
        var slideEntries = zipFile.entries.Where(e => e.name.StartsWith("ppt/slides/slide") && e.name.EndsWith(".xml"));

        foreach (var slideEntry in slideEntries)
        {
            var slideText = extract_text_from_xml(slideEntry.data);
            if (!string.IsNullOrEmpty(slideText)) slides.Add(slideText);
        }

        return new PowerPointData { slides = slides };
    }

    /// <summary>
    ///     提取工作表名称的
    /// </summary>
    private List<string> extract_sheet_names(byte[] xmlData)
    {
        // 简化实现，实际应该解析 XML
        var xml = Encoding.UTF8.GetString(xmlData);
        var names = new List<string>();

        // 简单的字符串匹配提的sheet 名称
        var sheetTag = "sheet name=\"";
        var index = 0;
        while ((index = xml.IndexOf(sheetTag, index)) != -1)
        {
            index += sheetTag.Length;
            var endIndex = xml.IndexOf("\"", index);
            if (endIndex != -1) names.Add(xml.Substring(index, endIndex - index));
        }

        return names;
    }

    /// <summary>
    ///     提取工作表数据的
    /// </summary>
    private List<ExcelRowData> extract_sheet_data(byte[] xmlData)
    {
        // 简化实现，实际应该解析 XML
        var xml = Encoding.UTF8.GetString(xmlData);
        var rows = new List<ExcelRowData>();

        // 简单的字符串匹配提取行数据
        var rowTag = "<row";
        var cellTag = "<c";
        var valueTag = "<v>";

        var rowIndex = 0;
        var index = 0;
        while ((index = xml.IndexOf(rowTag, index)) != -1)
        {
            index += rowTag.Length;
            var endIndex = xml.IndexOf("</row>", index);
            if (endIndex == -1) break;

            var rowXml = xml.Substring(index, endIndex - index);
            var cells = new List<ExcelCellData>();
            var cellIndex = 0;
            var cellPos = 0;

            while ((cellPos = rowXml.IndexOf(cellTag, cellPos)) != -1)
            {
                cellPos += cellTag.Length;
                var valuePos = rowXml.IndexOf(valueTag, cellPos);
                if (valuePos != -1)
                {
                    valuePos += valueTag.Length;
                    var valueEnd = rowXml.IndexOf("</v>", valuePos);
                    if (valueEnd != -1)
                    {
                        var value = rowXml.Substring(valuePos, valueEnd - valuePos);
                        cells.Add(new ExcelCellData
                        {
                            row = rowIndex,
                            column = cellIndex,
                            value = value,
                            cell_reference = $"{get_column_name(cellIndex)}{rowIndex + 1}"
                        });
                        cellIndex++;
                    }
                }
            }

            rows.Add(new ExcelRowData
            {
                row_index = rowIndex,
                cells = cells
            });
            rowIndex++;
        }

        return rows;
    }

    /// <summary>
    ///     的XML 数据提取文本内容的
    /// </summary>
    private string extract_text_from_xml(byte[] xmlData)
    {
        // 简化实现，实际应该解析 XML
        var xml = Encoding.UTF8.GetString(xmlData);
        var text = new StringBuilder();

        // 简单的字符串匹配提取文的
        var textTag = "<w:t>";
        var index = 0;
        while ((index = xml.IndexOf(textTag, index)) != -1)
        {
            index += textTag.Length;
            var endIndex = xml.IndexOf("</w:t>", index);
            if (endIndex != -1)
            {
                text.Append(xml.Substring(index, endIndex - index));
                text.Append(" ");
            }
        }

        return text.ToString().Trim();
    }

    /// <summary>
    ///     获取列名称（的A, B, C...）的
    /// </summary>
    private string get_column_name(int index)
    {
        var name = new StringBuilder();
        var c = index;
        do
        {
            name.Insert(0, (char)('A' + c % 26));
            c = c / 26 - 1;
        } while (c >= 0);

        return name.ToString();
    }
}