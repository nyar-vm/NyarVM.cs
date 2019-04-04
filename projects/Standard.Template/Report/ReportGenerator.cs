using System.Text;

namespace Std.Template.Report;

#region 数据模型

/// <summary>
///     报表定义
/// </summary>
public class ReportDefinition
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Layout { get; set; } = "default";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string Author { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
///     报表节
/// </summary>
public class ReportSection
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string RawContent { get; set; } = "";
    public int Number { get; set; }
    public string Type { get; set; } = "text";
    public Dictionary<string, object> Data { get; set; } = new();
    public List<TocItem> TocItems { get; set; } = [];
}

/// <summary>
///     报表数据行
/// </summary>
public class ReportDataRow
{
    public Dictionary<string, object> Values { get; set; } = new();
}

/// <summary>
///     报表数据集
/// </summary>
public class ReportDataSet
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; set; } = [];
    public List<ReportDataRow> Rows { get; set; } = [];
}

/// <summary>
///     报表生成结果
/// </summary>
public class ReportResult
{
    public string Title { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public int SectionCount { get; init; }
    public int DataSetCount { get; init; }
    public List<string> GeneratedFiles { get; init; } = [];
}

#endregion

/// <summary>
///     报表生成器
/// </summary>
public sealed class ReportGenerator
{
    private readonly MarkdownLanguage _markdownLanguage;
    private readonly MarkdownRenderer _markdownRenderer;
    private readonly DejaVuRenderer _renderer;
    private readonly TemplateManager _templateManager;
    private string _sourceDir = "";

    public ReportGenerator(string sourceDir)
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
    ///     从 Markdown 文件生成报表
    /// </summary>
    public ReportResult Generate(string sourceDir, string outputDir)
    {
        _sourceDir = Path.GetFullPath(sourceDir);
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var definition = LoadDefinition();
        var sections = LoadSections();
        var dataSets = LoadDataSets();

        BuildSectionNavigation(sections);
        GenerateReportPages(definition, sections, dataSets, outputDir);
        GenerateSummaryPage(definition, sections, outputDir);
        CopyAssets(outputDir);

        return new ReportResult
        {
            Title = definition.Title,
            OutputPath = outputDir,
            SectionCount = sections.Count,
            DataSetCount = dataSets.Count,
            GeneratedFiles = [.. Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories)]
        };
    }

    /// <summary>
    ///     从数据集生成表格 HTML
    /// </summary>
    public string GenerateTable(ReportDataSet dataSet, string? tableClass = null)
    {
        var sb = new StringBuilder();
        var classAttr = string.IsNullOrEmpty(tableClass) ? "" : $" class=\"{tableClass}\"";

        sb.AppendLine($"<table{classAttr}>");
        sb.AppendLine("<thead><tr>");

        foreach (var col in dataSet.Columns) sb.AppendLine($"<th>{EscapeHtml(col)}</th>");

        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (var row in dataSet.Rows)
        {
            sb.AppendLine("<tr>");
            foreach (var col in dataSet.Columns)
            {
                var value = row.Values.TryGetValue(col, out var v) ? v?.ToString() ?? "" : "";
                sb.AppendLine($"<td>{EscapeHtml(value)}</td>");
            }

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    /// <summary>
    ///     从数据集生成图表数据（JSON 格式）
    /// </summary>
    public string GenerateChartData(ReportDataSet dataSet, string labelColumn, string valueColumn)
    {
        var items = new List<Dictionary<string, object>>();
        foreach (var row in dataSet.Rows)
        {
            var label = row.Values.TryGetValue(labelColumn, out var l) ? l?.ToString() ?? "" : "";
            var value = row.Values.TryGetValue(valueColumn, out var v) ? v?.ToString() ?? "0" : "0";
            items.Add(new Dictionary<string, object>
            {
                ["label"] = label,
                ["value"] = double.TryParse(value, out var numValue) ? numValue : value
            });
        }

        return DataConvert.SerializeJson(items);
    }

    #region 导航

    private static void BuildSectionNavigation(List<ReportSection> sections)
    {
        for (var i = 0; i < sections.Count; i++)
        {
            if (i > 0) sections[i].Data["previousTitle"] = sections[i - 1].Title;
            if (i < sections.Count - 1) sections[i].Data["nextTitle"] = sections[i + 1].Title;
        }
    }

    #endregion

    #region 上下文

    private Dictionary<string, object> BuildReportContext(ReportDefinition definition)
    {
        return new Dictionary<string, object>
        {
            ["report"] = new Dictionary<string, object>
            {
                ["name"] = definition.Name,
                ["title"] = definition.Title,
                ["description"] = definition.Description,
                ["author"] = definition.Author,
                ["generatedAt"] = definition.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };
    }

    #endregion

    #region 工具方法

    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    #endregion

    #region 加载

    private ReportDefinition LoadDefinition()
    {
        var definition = new ReportDefinition();
        var configPath = Path.Combine(_sourceDir, "report.yaml");

        if (!File.Exists(configPath)) return definition;

        var content = File.ReadAllText(configPath);
        var parser = new YamlParser();
        var result = parser.parse(content);
        if (!result.success || result.value is not YamlMapping mapping) return definition;

        definition.Name = DataConvert.GetYamlString(mapping, "name", definition.Name);
        definition.Title = DataConvert.GetYamlString(mapping, "title", definition.Title);
        definition.Description = DataConvert.GetYamlString(mapping, "description", definition.Description);
        definition.Layout = DataConvert.GetYamlString(mapping, "layout", definition.Layout);
        definition.Author = DataConvert.GetYamlString(mapping, "author", definition.Author);

        return definition;
    }

    private List<ReportSection> LoadSections()
    {
        var sections = new List<ReportSection>();
        var sectionsDir = Path.Combine(_sourceDir, "sections");
        if (!Directory.Exists(sectionsDir)) return sections;

        var files = Directory.GetFiles(sectionsDir, "*.md").OrderBy(f => f).ToList();
        var number = 1;

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var section = new ReportSection { Number = number };

            var (frontMatter, body) = FrontMatterParser.Extract(content);
            if (frontMatter != null)
            {
                var mapping = FrontMatterParser.Parse(frontMatter);
                if (mapping != null) ParseSectionFrontMatterFromMapping(mapping, section);
            }
            else
            {
                body = content;
            }

            if (string.IsNullOrEmpty(section.Title)) section.Title = $"第 {number} 节";

            section.RawContent = body;
            var markdownDocument = _markdownLanguage.parse(body);
            var mdResult = _markdownRenderer.Render(markdownDocument);
            section.Content = mdResult.Html;
            section.TocItems = mdResult.TocItems.ToList();

            sections.Add(section);
            number++;
        }

        return sections;
    }

    private void ParseSectionFrontMatterFromMapping(YamlMapping mapping, ReportSection section)
    {
        section.Title = DataConvert.GetYamlString(mapping, "title", section.Title);
        section.type = DataConvert.GetYamlString(mapping, "type", section.type);

        foreach (var (key, value) in mapping.properties)
            if (key is not ("title" or "type"))
                section.Data[key] = DataConvert.YamlValueToObject(value) ?? "";
    }

    private List<ReportDataSet> LoadDataSets()
    {
        var dataSets = new List<ReportDataSet>();
        var dataDir = Path.Combine(_sourceDir, "data");
        if (!Directory.Exists(dataDir)) return dataSets;

        foreach (var file in Directory.GetFiles(dataDir, "*.csv"))
        {
            var dataSet = ParseCsvFile(file);
            dataSets.Add(dataSet);
        }

        return dataSets;
    }

    private ReportDataSet ParseCsvFile(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var content = File.ReadAllText(filePath);
        var rows = CsvParser.ParseRows(content);
        var dataSet = new ReportDataSet { Name = name };

        if (rows.Count == 0) return dataSet;

        dataSet.Columns = rows[0].ToList();

        for (var i = 1; i < rows.Count; i++)
        {
            if (rows[i].Count == 0) continue;

            var row = new ReportDataRow();
            for (var j = 0; j < dataSet.Columns.Count; j++)
            {
                var value = j < rows[i].Count ? rows[i][j] : "";
                if (double.TryParse(value, out var numValue))
                    row.Values[dataSet.Columns[j]] = numValue;
                else
                    row.Values[dataSet.Columns[j]] = value;
            }

            dataSet.Rows.Add(row);
        }

        return dataSet;
    }

    #endregion

    #region 生成

    private void GenerateReportPages(ReportDefinition definition, List<ReportSection> sections,
        List<ReportDataSet> dataSets, string outputDir)
    {
        foreach (var section in sections)
        {
            var context = BuildReportContext(definition);
            context["section"] = new Dictionary<string, object>
            {
                ["title"] = section.Title,
                ["number"] = section.Number,
                ["content"] = section.Content,
                ["type"] = section.type,
                ["toc"] = section.TocItems.Select(t => new Dictionary<string, object>
                {
                    ["level"] = t.level,
                    ["text"] = t.text,
                    ["id"] = t.id
                }).ToList()
            };

            var layoutName = definition.Layout;
            var pageTemplate =
                $"<% extends 'layouts/{layoutName}.dora' %>\n<% block content %>\n{section.Content}\n<% end block %>";
            var rendered = _renderer.Render(pageTemplate, context);

            var sectionDir = Path.Combine(outputDir, $"section-{section.Number}");
            Directory.CreateDirectory(sectionDir);
            File.WriteAllText(Path.Combine(sectionDir, "index.html"), rendered);
        }
    }

    private void GenerateSummaryPage(ReportDefinition definition, List<ReportSection> sections, string outputDir)
    {
        var context = BuildReportContext(definition);
        context["sections"] = sections.Select(s => new Dictionary<string, object>
        {
            ["title"] = s.Title,
            ["number"] = s.Number,
            ["type"] = s.type,
            ["url"] = $"/section-{s.Number}/"
        }).ToList();

        var summaryLayoutPath = Path.Combine(_sourceDir, "layouts/summary.dora");
        string rendered;

        if (File.Exists(summaryLayoutPath))
        {
            var layoutContent = File.ReadAllText(summaryLayoutPath);
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
            var summaryHtml = $"<h1>{definition.Title}</h1>\n<ul>\n";
            foreach (var s in sections) summaryHtml += $"<li><a href=\"/section-{s.Number}/\">{s.Title}</a></li>\n";
            summaryHtml += "</ul>\n";

            var pageTemplate =
                $"<% extends 'layouts/default.dora' %>\n<% block content %>\n{summaryHtml}\n<% end block %>";
            rendered = _renderer.Render(pageTemplate, context);
        }

        File.WriteAllText(Path.Combine(outputDir, "index.html"), rendered);
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
}